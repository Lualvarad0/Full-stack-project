using System.Collections.Concurrent;
using System.Xml;
using Back.Dtos;
using Back.Helpers;
using Back.Interfaces;
using Back.Models;
using Back.Pagos;
using Back.Sri;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Back.Contabilidad;
using Back.Data;
using Back.Clasificacion;

namespace Back.Services
{
    public class ComprobanteService
    {
        public const int MaxClavesPorImportacion = 500;

        private static readonly HashSet<CategoriaGasto> Deducibles = new()
        {
            CategoriaGasto.Vivienda, CategoriaGasto.Educacion, CategoriaGasto.Salud,
            CategoriaGasto.Alimentacion, CategoriaGasto.Vestimenta, CategoriaGasto.Turismo,
        };

        private readonly IComprobanteRepository _repository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly SriAutorizacionClient _sri;
        private readonly SriOptions _sriOptions;
        private readonly ILogger<ComprobanteService> _logger;
        private readonly ControlPeriodoService _periodos;
        private readonly AppDbContext _db;

        public ComprobanteService(
            IComprobanteRepository repository,
            IUsuarioRepository usuarioRepository,
            SriAutorizacionClient sri,
            IOptions<SriOptions> sriOptions,
            ILogger<ComprobanteService> logger, ControlPeriodoService periodos, AppDbContext db)
        {
            _repository = repository;
            _usuarioRepository = usuarioRepository;
            _sri = sri;
            _sriOptions = sriOptions.Value;
            _logger = logger;
            _periodos = periodos; _db = db;
        }

        /// <summary>
        /// Busca claves de acceso en el texto (reporte .txt del SRI, XML de correo o claves pegadas),
        /// consulta cada clave nueva en el SRI y guarda los comprobantes autorizados.
        /// Los emitidos con el RUC del perfil quedan como ingresos; el resto, como gastos.
        /// </summary>
        public async Task<ImportacionResponse> ImportarAsync(int usuarioId, string texto, CancellationToken ct)
        {
            var claves = ClaveAcceso.Extraer(texto);
            if (claves.Count == 0)
                throw new BusinessException("No se encontraron claves de acceso válidas (49 dígitos) en el contenido enviado.");
            if (claves.Count > MaxClavesPorImportacion)
                throw new BusinessException($"Se encontraron {claves.Count} claves; el máximo por importación es {MaxClavesPorImportacion}.");

            var perfil = await ObtenerPerfilAsync(usuarioId);
            if (!AccesoPlan.TieneProfesional(perfil))
                throw new BusinessException(AccesoPlan.MensajeSubirFacturas, StatusCodes.Status403Forbidden);
            var resultados = new ConcurrentDictionary<string, ClaveImportada>();
            var existentes = await _repository.GetClavesExistentesAsync(usuarioId, claves);
            var porConsultar = new List<string>();
            foreach (var clave in claves)
            {
                var tipo = ClaveAcceso.TipoComprobante(clave);
                if (existentes.Contains(clave))
                    resultados[clave] = new(clave, ResultadoImportacion.YaExistia);
                else if (!TiposComprobante.Soportados.Contains(tipo))
                    resultados[clave] = new(clave, ResultadoImportacion.NoSoportado, $"{TiposComprobante.Nombre(tipo)}: todavía no se procesa.");
                else
                    porConsultar.Add(clave);
            }

            var nuevos = new ConcurrentBag<Comprobante>();
            var opciones = new ParallelOptions { MaxDegreeOfParallelism = _sriOptions.ConsultasEnParalelo, CancellationToken = ct };
            await Parallel.ForEachAsync(porConsultar, opciones, async (clave, token) =>
            {
                var (resultado, comprobante) = await ConsultarYParsearAsync(clave, token);
                if (comprobante is not null)
                {
                    comprobante.Direccion = comprobante.RucEmisor == perfil.Ruc ? Direccion.Emitido : Direccion.Recibido;
                    resultado = resultado with { Direccion = comprobante.Direccion };
                    nuevos.Add(comprobante);
                }
                resultados[clave] = resultado;
            });

            if (!nuevos.IsEmpty)
            {
                var categorias = await _repository.GetCategoriasProveedorAsync(usuarioId, nuevos.Select(c => c.RucEmisor).Distinct());
                foreach (var comprobante in nuevos)
                {
                    comprobante.UsuarioId = usuarioId;
                    comprobante.Categoria = comprobante.Direccion == Direccion.Recibido
                        ? categorias.GetValueOrDefault(comprobante.RucEmisor, CategoriaGasto.SinCategoria)
                        : CategoriaGasto.SinCategoria;
                    if (comprobante.Categoria != CategoriaGasto.SinCategoria) {
                        comprobante.Finalidad = comprobante.Categoria == CategoriaGasto.NoDeducible ? FinalidadComprobante.NoAplicable : FinalidadComprobante.Personal;
                        comprobante.RevisadoEn = DateTime.UtcNow;
                    }
                }

                try
                {
                    await using var tx = await _periodos.AbrirAsync(usuarioId, nuevos.Select(c => c.FechaEmision), ct);
                    _periodos.Auditar(usuarioId, "Importar", "Comprobantes", "Lote", $"Nuevos comprobantes: {nuevos.Count}");
                    await _repository.AddRangeAsync(nuevos);
                    await tx.ConfirmarAsync(ct);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    throw BusinessException.Conflict("Otra importación guardó algunos de estos comprobantes al mismo tiempo. Vuelve a importar el archivo.");
                }
            }

            // Respeta el orden en que aparecieron las claves en el archivo.
            var lista = claves.Select(c => resultados[c]).ToList();
            return new ImportacionResponse(
                claves.Count,
                lista.Count(r => r.Resultado == ResultadoImportacion.Importado),
                lista.Count(r => r.Resultado == ResultadoImportacion.YaExistia),
                lista.Count(r => r.Resultado is not (ResultadoImportacion.Importado or ResultadoImportacion.YaExistia)),
                lista.Count(r => r.Resultado == ResultadoImportacion.Importado && r.Direccion == Direccion.Emitido),
                PerfilCompleto: !string.IsNullOrEmpty(perfil.Ruc),
                lista);
        }

        private async Task<(ClaveImportada, Comprobante?)> ConsultarYParsearAsync(string clave, CancellationToken ct)
        {
            SriAutorizacion autorizacion;
            try
            {
                autorizacion = await _sri.ConsultarAsync(clave, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or XmlException && !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "El SRI no respondió para la clave {Clave}", clave);
                return (new(clave, ResultadoImportacion.Error, "El SRI no respondió. Intenta de nuevo más tarde."), null);
            }

            if (autorizacion.Estado == EstadoAutorizacion.NoEncontrado)
                return (new(clave, ResultadoImportacion.NoEncontrado, autorizacion.Mensaje), null);
            if (autorizacion.Estado == EstadoAutorizacion.NoAutorizado || string.IsNullOrWhiteSpace(autorizacion.ComprobanteXml))
                return (new(clave, ResultadoImportacion.NoAutorizado, autorizacion.Mensaje), null);
            if (clave.Length == 49 && clave[23] != '2')
                return (new(clave, ResultadoImportacion.NoAutorizado, "El comprobante pertenece al ambiente de pruebas; no se incorpora a los registros tributarios."), null);

            try
            {
                var comprobante = ComprobanteXmlParser.Parse(autorizacion.ComprobanteXml);
                if (comprobante.ClaveAcceso != clave)
                    return (new(clave, ResultadoImportacion.Error, "El XML devuelto por el SRI no corresponde a la clave consultada."), null);

                comprobante.NumeroAutorizacion = autorizacion.NumeroAutorizacion ?? clave;
                comprobante.FechaAutorizacion = autorizacion.FechaAutorizacion;
                comprobante.Xml = new ComprobanteXml { Contenido = autorizacion.ComprobanteXml };
                return (new(clave, ResultadoImportacion.Importado, $"{comprobante.RazonSocialEmisor} — ${comprobante.Total:0.00}"), comprobante);
            }
            catch (Exception ex) when (ex is XmlException or FormatException or NotSupportedException)
            {
                _logger.LogWarning(ex, "No se pudo leer el XML del comprobante {Clave}", clave);
                return (new(clave, ResultadoImportacion.Error, "No se pudo leer el XML del comprobante devuelto por el SRI."), null);
            }
        }

        public async Task<List<ComprobanteListaDto>> ObtenerComprobantesAsync(int usuarioId, FiltroComprobantes filtro)
        {
            var perfil = await ObtenerPerfilAsync(usuarioId);
            if (filtro.Direccion == Direccion.Emitido) AccesoPlan.ExigirProfesional(perfil, "Tus ingresos y clientes");
            if (filtro.Desde is not null || filtro.Hasta is not null || filtro.MontoMinimo is not null || filtro.MontoMaximo is not null)
                AccesoPlan.ExigirProfesional(perfil, "Los filtros por fecha y monto");
            AccesoPlan.ValidarPeriodo(perfil, filtro.Anio, filtro.Mes);

            var comprobantes = await _repository.GetAllAsync(usuarioId, filtro);
            if (!AccesoPlan.TieneProfesional(perfil))
                comprobantes = comprobantes.Where(c => c.FechaEmision >= AccesoPlan.InicioHistorial()).ToList();
            return comprobantes.Select(c => ComprobanteListaDto.From(c, perfil)).ToList();
        }

        public async Task<ComprobanteDto> ObtenerComprobanteAsync(int usuarioId, int id) =>
            ComprobanteDto.From(await ObtenerEntidadAsync(usuarioId, id, conDetalles: true), await ObtenerPerfilAsync(usuarioId));

        public async Task<string> ObtenerXmlAsync(int usuarioId, int id) =>
            await _repository.GetXmlAsync(usuarioId, id) ?? throw BusinessException.NotFound("Comprobante no encontrado.");

        public async Task<ComprobanteDto> CambiarCategoriaAsync(int usuarioId, int id, CambiarCategoriaRequest request)
        {
            var comprobante = await ObtenerEntidadAsync(usuarioId, id);
            if (comprobante.Direccion == Direccion.Emitido)
                throw new BusinessException("Las facturas que emites son ingresos y no llevan categoría de gasto.");
            await using var tx = await _periodos.AbrirAsync(usuarioId, [comprobante.FechaEmision]);
            if (request.AplicarAlProveedor) await _periodos.ExigirAbiertosAsync(usuarioId,
                await _db.Comprobantes.Where(c => c.UsuarioId == usuarioId && c.RucEmisor == comprobante.RucEmisor && c.Direccion == Direccion.Recibido).Select(c => c.FechaEmision).ToListAsync());

            // Se asigna también en la entidad cargada: ExecuteUpdate no actualiza las instancias que EF ya tiene en memoria.
            comprobante.Categoria = request.Categoria;
            comprobante.Finalidad = request.Categoria == CategoriaGasto.SinCategoria ? FinalidadComprobante.SinDefinir : request.Categoria == CategoriaGasto.NoDeducible ? FinalidadComprobante.NoAplicable : FinalidadComprobante.Personal;
            comprobante.RevisadoEn = request.Categoria == CategoriaGasto.SinCategoria ? null : DateTime.UtcNow;
            _periodos.Auditar(usuarioId, "ClasificarGasto", "Comprobante", id.ToString(), $"Categoría: {request.Categoria}; proveedor: {request.AplicarAlProveedor}");
            if (request.AplicarAlProveedor)
                await _repository.AsignarCategoriaProveedorAsync(usuarioId, comprobante.RucEmisor, request.Categoria);
            else
                await _repository.SaveChangesAsync();
            await _repository.SaveChangesAsync(); await tx.ConfirmarAsync();

            return await ObtenerComprobanteAsync(usuarioId, id);
        }

        public async Task<ComprobanteDto> CambiarCategoriaIngresoAsync(int usuarioId, int id, CambiarCategoriaIngresoRequest request)
        {
            var comprobante = await ObtenerEntidadAsync(usuarioId, id);
            AccesoPlan.ExigirProfesional(await ObtenerPerfilAsync(usuarioId), "La clasificación de ingresos");
            if (comprobante.Direccion != Direccion.Emitido)
                throw new BusinessException("Este comprobante es un gasto; elige una categoría de gasto.");
            await using var tx = await _periodos.AbrirAsync(usuarioId, [comprobante.FechaEmision]);
            comprobante.CategoriaIngreso = request.Categoria;
            comprobante.Finalidad = FinalidadComprobante.Negocio; comprobante.RevisadoEn = DateTime.UtcNow;
            _periodos.Auditar(usuarioId, "ClasificarIngreso", "Comprobante", id.ToString(), $"Categoría: {request.Categoria}");
            await _repository.SaveChangesAsync();
            await tx.ConfirmarAsync();
            return await ObtenerComprobanteAsync(usuarioId, id);
        }

        public async Task EliminarComprobanteAsync(int usuarioId, int id)
        {
            var c = await ObtenerEntidadAsync(usuarioId, id); await using var tx = await _periodos.AbrirAsync(usuarioId, [c.FechaEmision]);
            if (await _db.Set<RetencionSriDetalle>().AnyAsync(r => r.ComprobanteId == id)
                || await _db.FacturasEmitidas.AnyAsync(f => f.UsuarioId == usuarioId && f.ComprobanteSustentoId == id)
                || await _db.Set<PagoComprobante>().AnyAsync(p => p.UsuarioId == usuarioId && p.ComprobanteId == id)
                || await _db.Set<AsientoContable>().AnyAsync(a => a.UsuarioId == usuarioId && a.ComprobanteId == id))
                throw BusinessException.Conflict("El comprobante tiene retenciones vinculadas. Conserva su documento de sustento.");
            _periodos.Auditar(usuarioId, "EliminarImportado", "Comprobante", id.ToString(), $"Clave: {c.ClaveAcceso}");
            await _repository.DeleteAsync(c); await tx.ConfirmarAsync();
        }

        public async Task<List<ProveedorDto>> ObtenerProveedoresAsync(int usuarioId)
        {
            var comprobantes = await _repository.GetAllAsync(usuarioId, new FiltroComprobantes(Direccion.Recibido));
            var categorias = await _repository.GetCategoriasProveedorAsync(usuarioId);
            return comprobantes
                .GroupBy(c => c.RucEmisor)
                .Select(g => new ProveedorDto(
                    g.Key,
                    g.First().RazonSocialEmisor,
                    categorias.TryGetValue(g.Key, out var categoria) ? categoria : null,
                    g.Count(),
                    g.Sum(c => c.Signo * c.Total)))
                .OrderByDescending(p => p.Total)
                .ToList();
        }

        public async Task AsignarCategoriaProveedorAsync(int usuarioId, string ruc, CategoriaGasto categoria)
        {
            await using var tx = await _periodos.AbrirAsync(usuarioId, []);
            await _periodos.ExigirAbiertosAsync(usuarioId, await _db.Comprobantes.Where(c => c.UsuarioId == usuarioId && c.RucEmisor == ruc && c.Direccion == Direccion.Recibido).Select(c => c.FechaEmision).ToListAsync());
            _periodos.Auditar(usuarioId, "ClasificarProveedor", "Proveedor", ruc, $"Categoría: {categoria}");
            await _repository.AsignarCategoriaProveedorAsync(usuarioId, ruc, categoria); await _repository.SaveChangesAsync(); await tx.ConfirmarAsync();
        }

        /// <summary>Clientes a los que el usuario facturó (comprobantes emitidos), opcionalmente en un año/mes.</summary>
        public async Task<List<ClienteFacturadoDto>> ObtenerClientesAsync(int usuarioId, int? anio, int? mes)
        {
            AccesoPlan.ExigirProfesional(await ObtenerPerfilAsync(usuarioId), "Tus ingresos y clientes");
            var emitidos = await _repository.GetAllAsync(usuarioId, new FiltroComprobantes(Direccion.Emitido, anio, mes));
            return emitidos
                .GroupBy(c => c.IdentificacionComprador)
                .Select(g => new ClienteFacturadoDto(
                    g.Key,
                    g.OrderByDescending(c => c.FechaEmision).First().RazonSocialComprador,
                    g.Count(),
                    g.Sum(c => c.Signo * c.Total),
                    g.Sum(c => c.Signo * c.Iva),
                    g.Max(c => c.FechaEmision)))
                .OrderByDescending(c => c.Total)
                .ToList();
        }

        public async Task<ResumenDto> ObtenerResumenAsync(int usuarioId, int anio, int? mes)
        {
            var perfil = await ObtenerPerfilAsync(usuarioId);
            var todos = await _repository.GetAllAsync(usuarioId, new FiltroComprobantes(null, anio, mes));
            AccesoPlan.ValidarPeriodo(perfil, anio, mes);
            if (!AccesoPlan.TieneProfesional(perfil))
                todos = todos.Where(c => c.FechaEmision >= AccesoPlan.InicioHistorial()).ToList();
            var gastos = todos.Where(c => c.Direccion == Direccion.Recibido).ToList();
            // En el plan Personal el resumen solo trae gastos (ingresos, clientes e IVA cobrado son del plan Profesional).
            var ingresos = AccesoPlan.TieneProfesional(perfil) ? todos.Where(c => c.Direccion == Direccion.Emitido).ToList() : new List<Comprobante>();

            var porCategoria = gastos
                .GroupBy(c => c.Categoria)
                .Select(g => new TotalCategoriaDto(g.Key, g.Count(), g.Sum(c => c.Signo * c.Total)))
                .OrderByDescending(t => t.Total)
                .ToList();

            var proveedores = gastos
                .GroupBy(c => c.RucEmisor)
                .Select(g => new TotalTerceroDto(g.Key, g.First().RazonSocialEmisor, g.Count(), g.Sum(c => c.Signo * c.Total)))
                .OrderByDescending(t => t.Total)
                .Take(10)
                .ToList();

            var clientes = ingresos
                .GroupBy(c => c.IdentificacionComprador)
                .Select(g => new TotalTerceroDto(g.Key, g.First().RazonSocialComprador, g.Count(), g.Sum(c => c.Signo * c.Total)))
                .OrderByDescending(t => t.Total)
                .Take(10)
                .ToList();

            var porMes = mes.HasValue ? null : Enumerable.Range(1, 12)
                .Select(m =>
                {
                    var gastosMes = gastos.Where(c => c.FechaEmision.Month == m).ToList();
                    var ingresosMes = ingresos.Where(c => c.FechaEmision.Month == m).ToList();
                    return new TotalMesDto(
                        m,
                        gastosMes.Count,
                        gastosMes.Sum(c => c.Signo * c.Total),
                        gastosMes.Sum(c => c.Signo * c.Iva),
                        ingresosMes.Sum(c => c.Signo * c.Total),
                        ingresosMes.Sum(c => c.Signo * c.Iva));
                })
                .ToList();

            return new ResumenDto(
                anio,
                mes,
                gastos.Count,
                gastos.Sum(c => c.Signo * c.Total),
                gastos.Sum(c => c.Signo * c.Iva),
                gastos.Sum(ImportesGastosPersonales.BasePersonal),
                gastos.Count(c => c.Categoria == CategoriaGasto.SinCategoria),
                gastos.Count(c => ComprobanteListaDto.ANombreDe(c, perfil) == false),
                ingresos.Count,
                ingresos.Sum(c => c.Signo * c.Total),
                ingresos.Sum(c => c.Signo * c.Iva),
                porCategoria,
                proveedores,
                clientes,
                porMes);
        }

        private async Task<Comprobante> ObtenerEntidadAsync(int usuarioId, int id, bool conDetalles = false) =>
            await _repository.GetByIdAsync(usuarioId, id, conDetalles) ?? throw BusinessException.NotFound("Comprobante no encontrado.");

        private async Task<Usuario> ObtenerPerfilAsync(int usuarioId) =>
            await _usuarioRepository.GetByIdAsync(usuarioId) ?? throw BusinessException.NotFound("Usuario no encontrado.");
    }
}
