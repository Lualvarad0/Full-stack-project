using System.Text;
using Back.Avisos;
using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Back.Pagos;
using Back.Services;
using Back.Sri;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Back.Facturacion
{
    /// <summary>Firma electrónica, datos de emisión y emisión de facturas electrónicas ante el SRI.</summary>
    public class FacturacionService
    {
        private const long MaxBytesFirma = 50 * 1024;
        private static readonly TimeSpan[] EsperasAutorizacion = { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5) };

        private readonly AppDbContext _context;
        private readonly IDataProtector _protector;
        private readonly SriAutorizacionClient _sri;
        private readonly AvisoService _avisos;
        private readonly ComprobanteService _comprobantes;
        private readonly ILogger<FacturacionService> _logger;

        public FacturacionService(AppDbContext context, IDataProtectionProvider proteccion, SriAutorizacionClient sri,
            AvisoService avisos, ComprobanteService comprobantes, ILogger<FacturacionService> logger)
        {
            _context = context;
            _protector = proteccion.CreateProtector("Nudo.FirmaElectronica.v1");
            _sri = sri;
            _avisos = avisos;
            _comprobantes = comprobantes;
            _logger = logger;
        }

        // ---------- Firma electrónica ----------

        public async Task<FirmaElectronicaDto?> ObtenerFirmaAsync(int usuarioId)
        {
            var firma = await _context.FirmasElectronicas.AsNoTracking().FirstOrDefaultAsync(f => f.UsuarioId == usuarioId);
            return firma is null ? null : FirmaElectronicaDto.From(firma, await UsuarioAsync(usuarioId));
        }

        public async Task<FirmaElectronicaDto> GuardarFirmaAsync(int usuarioId, IFormFile archivo, string clave)
        {
            if (archivo.Length == 0 || archivo.Length > MaxBytesFirma)
                throw new BusinessException("Sube el archivo .p12 de tu firma electrónica (pesa unos pocos KB).");
            if (string.IsNullOrEmpty(clave))
                throw new BusinessException("Escribe la contraseña de tu firma electrónica.");

            using var memoria = new MemoryStream();
            await archivo.CopyToAsync(memoria);
            var bytes = memoria.ToArray();

            using var certificado = CertificadoFirma.Abrir(bytes, clave);
            var datos = CertificadoFirma.Describir(certificado);
            if (datos.ValidaHasta < DateTime.UtcNow)
                throw new BusinessException($"Esta firma venció el {datos.ValidaHasta:dd/MM/yyyy}. Renuévala con tu entidad certificadora.");

            var firma = await _context.FirmasElectronicas.FirstOrDefaultAsync(f => f.UsuarioId == usuarioId);
            var reemplaza = firma is not null;
            firma ??= _context.FirmasElectronicas.Add(new FirmaElectronica { UsuarioId = usuarioId }).Entity;
            firma.ArchivoCifrado = _protector.Protect(bytes);
            firma.ClaveCifrada = _protector.Protect(Encoding.UTF8.GetBytes(clave));
            firma.Titular = datos.Titular;
            firma.Identificacion = datos.Identificacion;
            firma.Emisor = datos.Emisor;
            firma.NumeroSerie = datos.NumeroSerie;
            firma.ValidaDesde = datos.ValidaDesde;
            firma.ValidaHasta = datos.ValidaHasta;
            firma.SubidaEn = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _avisos.NotificarAsync(usuarioId, TipoAviso.FirmaElectronica,
                reemplaza ? "Se reemplazó tu firma electrónica" : "Se agregó tu firma electrónica",
                $"Certificado de {datos.Titular}, emitido por {datos.Emisor}, válido hasta el {datos.ValidaHasta:dd/MM/yyyy}.");

            return FirmaElectronicaDto.From(firma, await UsuarioAsync(usuarioId));
        }

        public async Task EliminarFirmaAsync(int usuarioId)
        {
            var eliminadas = await _context.FirmasElectronicas.Where(f => f.UsuarioId == usuarioId).ExecuteDeleteAsync();
            if (eliminadas > 0)
                await _avisos.NotificarAsync(usuarioId, TipoAviso.FirmaElectronica, "Se eliminó tu firma electrónica",
                    "Ya no podrás emitir facturas hasta que subas una firma nuevamente.");
        }

        // ---------- Datos de emisión ----------

        public async Task<DatosEmisorDto> ObtenerDatosAsync(int usuarioId)
        {
            var datos = await _context.DatosEmisores.AsNoTracking().FirstOrDefaultAsync(d => d.UsuarioId == usuarioId) ?? new DatosEmisor();
            return DatosEmisorDto.From(datos, await PendientesAsync(usuarioId, datos));
        }

        public async Task<DatosEmisorDto> GuardarDatosAsync(int usuarioId, DatosEmisorRequest request)
        {
            var datos = await _context.DatosEmisores.FirstOrDefaultAsync(d => d.UsuarioId == usuarioId)
                ?? _context.DatosEmisores.Add(new DatosEmisor { UsuarioId = usuarioId }).Entity;

            var cambios = new List<string>();
            if (datos.Ambiente != request.Ambiente) cambios.Add($"ambiente ({(request.Ambiente == AmbienteSri.Produccion ? "producción" : "pruebas")})");
            if (datos.Establecimiento != request.Establecimiento || datos.PuntoEmision != request.PuntoEmision) cambios.Add("punto de emisión");
            if (datos.SiguienteSecuencial != request.SiguienteSecuencial) cambios.Add("secuencial");
            if (datos.DireccionMatriz != request.DireccionMatriz.Trim()) cambios.Add("dirección");

            datos.Ambiente = request.Ambiente;
            datos.Establecimiento = request.Establecimiento;
            datos.PuntoEmision = request.PuntoEmision;
            datos.SiguienteSecuencial = request.SiguienteSecuencial;
            datos.NombreComercial = Limpiar(request.NombreComercial);
            datos.DireccionMatriz = request.DireccionMatriz.Trim();
            datos.DireccionEstablecimiento = Limpiar(request.DireccionEstablecimiento);
            datos.ObligadoContabilidad = request.ObligadoContabilidad;
            datos.ContribuyenteEspecial = Limpiar(request.ContribuyenteEspecial);
            await _context.SaveChangesAsync();

            await _avisos.NotificarAsync(usuarioId, TipoAviso.DatosFacturacion, "Se actualizaron tus datos de facturación",
                cambios.Count > 0 ? $"Cambios: {string.Join(", ", cambios)}." : "Se guardaron los datos de facturación.");

            return DatosEmisorDto.From(datos, await PendientesAsync(usuarioId, datos));
        }

        // ---------- Facturas ----------

        public async Task<List<FacturaEmitidaDto>> ListarAsync(int usuarioId) =>
            (await _context.FacturasEmitidas.AsNoTracking()
                .Where(f => f.UsuarioId == usuarioId)
                .OrderByDescending(f => f.CreadaEn)
                .Take(200)
                .ToListAsync())
            .Select(f => FacturaEmitidaDto.From(f))
            .ToList();

        public async Task<FacturaEmitidaDto> ObtenerAsync(int usuarioId, int id) =>
            FacturaEmitidaDto.From(await FacturaAsync(usuarioId, id), conDetalles: true);

        public async Task<string> ObtenerXmlAsync(int usuarioId, int id) => (await FacturaAsync(usuarioId, id)).XmlFirmado;

        /// <summary>Arma, firma y envía la factura al SRI; espera unos segundos la autorización.</summary>
        public async Task<FacturaEmitidaDto> EmitirAsync(int usuarioId, EmitirFacturaRequest request, CancellationToken ct)
        {
            var usuario = await UsuarioAsync(usuarioId);
            var firma = await _context.FirmasElectronicas.AsNoTracking().FirstOrDefaultAsync(f => f.UsuarioId == usuarioId, ct);
            var datosActuales = await _context.DatosEmisores.AsNoTracking().FirstOrDefaultAsync(d => d.UsuarioId == usuarioId, ct);
            var pendientes = await PendientesAsync(usuarioId, datosActuales ?? new DatosEmisor());
            if (pendientes.Count > 0)
                throw new BusinessException($"Antes de facturar completa: {string.Join(", ", pendientes)}.");

            var factura = ArmarFactura(request);
            factura.UsuarioId = usuarioId;
            factura.FechaEmision = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));

            // Reserva el secuencial con bloqueo de fila: dos emisiones simultáneas no pueden tomar el mismo número.
            await using (var transaccion = await _context.Database.BeginTransactionAsync(ct))
            {
                var datos = (await _context.DatosEmisores
                    .FromSqlInterpolated($"SELECT * FROM datos_emisor WHERE usuario_id = {usuarioId} FOR UPDATE")
                    .ToListAsync(ct)).Single();
                factura.Ambiente = datos.Ambiente;
                factura.Establecimiento = datos.Establecimiento;
                factura.PuntoEmision = datos.PuntoEmision;
                factura.Secuencial = datos.SiguienteSecuencial;
                factura.ClaveAcceso = FacturaXml.GenerarClave(factura.FechaEmision, usuario.Ruc!, datos.Ambiente, datos.Establecimiento, datos.PuntoEmision, datos.SiguienteSecuencial);

                using var certificado = AbrirCertificado(firma!);
                factura.XmlFirmado = FirmadorXades.Firmar(FacturaXml.Construir(factura, usuario, datos), certificado);

                datos.SiguienteSecuencial++;
                _context.FacturasEmitidas.Add(factura);
                await _context.SaveChangesAsync(ct);
                await transaccion.CommitAsync(ct);
            }

            await EnviarYAutorizarAsync(factura, ct);
            return FacturaEmitidaDto.From(factura, conDetalles: true);
        }

        /// <summary>Reenvía una factura que no llegó al SRI o vuelve a consultar su autorización.</summary>
        public async Task<FacturaEmitidaDto> ReintentarAsync(int usuarioId, int id, CancellationToken ct)
        {
            var factura = await FacturaAsync(usuarioId, id);
            if (factura.Estado is EstadoFactura.Autorizada or EstadoFactura.Devuelta or EstadoFactura.NoAutorizada or EstadoFactura.Anulada)
                throw new BusinessException("Esta factura ya tiene una respuesta definitiva del SRI.");
            await EnviarYAutorizarAsync(factura, ct);
            return FacturaEmitidaDto.From(factura, conDetalles: true);
        }

        /// <summary>
        /// Borra una factura que nunca quedó autorizada (sin enviar, devuelta o no autorizada): no existe para el SRI.
        /// Las autorizadas no se borran: se anulan con <see cref="AnularAsync"/>.
        /// </summary>
        public async Task EliminarAsync(int usuarioId, int id)
        {
            var factura = await FacturaAsync(usuarioId, id);
            if (factura.Estado is not (EstadoFactura.Firmada or EstadoFactura.Devuelta or EstadoFactura.NoAutorizada))
                throw BusinessException.Conflict(factura.Estado is EstadoFactura.Autorizada or EstadoFactura.Anulada
                    ? "Una factura autorizada por el SRI no se puede borrar: anúlala."
                    : "El SRI todavía está procesando esta factura. Consulta su autorización antes de borrarla.");
            _context.FacturasEmitidas.Remove(factura);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Marca como anulada una factura autorizada y la quita de los ingresos. La anulación tributaria se hace en
        /// SRI en Línea (Facturación electrónica → Anulación de comprobantes) y el cliente debe aceptarla.
        /// </summary>
        public async Task<FacturaEmitidaDto> AnularAsync(int usuarioId, int id)
        {
            var factura = await FacturaAsync(usuarioId, id);
            if (factura.Estado != EstadoFactura.Autorizada)
                throw BusinessException.Conflict("Solo se anulan facturas autorizadas. Las demás se pueden borrar.");
            factura.Estado = EstadoFactura.Anulada;
            factura.Mensajes = "Anulada en Nudo. Completa la anulación en SRI en Línea → Facturación electrónica → Anulación de comprobantes; tu cliente debe aceptarla.";
            await _context.Comprobantes.Where(c => c.UsuarioId == usuarioId && c.ClaveAcceso == factura.ClaveAcceso).ExecuteDeleteAsync();
            await _context.SaveChangesAsync();
            await _avisos.NotificarAsync(usuarioId, TipoAviso.DatosFacturacion, $"Anulaste la factura {factura.Numero}",
                $"Cliente: {factura.RazonSocialComprador}, total ${factura.Total:0.00}. Recuerda completar la anulación en SRI en Línea.");
            return FacturaEmitidaDto.From(factura, conDetalles: true);
        }

        // ---------- Servicios que se facturan a menudo ----------

        public async Task<List<ServicioFacturableDto>> ServiciosAsync(int usuarioId)
        {
            AccesoPlan.ExigirProfesional(await UsuarioAsync(usuarioId), "Tus servicios para facturar");
            return (await _context.ServiciosFacturables.AsNoTracking().Where(s => s.UsuarioId == usuarioId).OrderBy(s => s.Descripcion).ToListAsync())
                .Select(ServicioFacturableDto.From).ToList();
        }

        public async Task<ServicioFacturableDto> GuardarServicioAsync(int usuarioId, int? id, ServicioFacturableRequest request)
        {
            AccesoPlan.ExigirProfesional(await UsuarioAsync(usuarioId), "Tus servicios para facturar");
            var servicio = id is null
                ? _context.ServiciosFacturables.Add(new ServicioFacturable { UsuarioId = usuarioId }).Entity
                : await _context.ServiciosFacturables.FirstOrDefaultAsync(s => s.UsuarioId == usuarioId && s.Id == id)
                  ?? throw BusinessException.NotFound("Servicio no encontrado.");
            servicio.Codigo = Limpiar(request.Codigo);
            servicio.Descripcion = request.Descripcion.Trim();
            servicio.PrecioUnitario = request.PrecioUnitario;
            servicio.Tarifa = request.Tarifa;
            await _context.SaveChangesAsync();
            return ServicioFacturableDto.From(servicio);
        }

        public async Task EliminarServicioAsync(int usuarioId, int id)
        {
            if (await _context.ServiciosFacturables.Where(s => s.UsuarioId == usuarioId && s.Id == id).ExecuteDeleteAsync() == 0)
                throw BusinessException.NotFound("Servicio no encontrado.");
        }

        private async Task EnviarYAutorizarAsync(FacturaEmitida factura, CancellationToken ct)
        {
            try
            {
                if (factura.Estado == EstadoFactura.Firmada)
                {
                    var recepcion = await _sri.EnviarAsync(factura.XmlFirmado, factura.Ambiente == AmbienteSri.Produccion, ct);
                    // Error 43 "clave de acceso registrada": el SRI ya la tenía (por ejemplo, un reintento): se consulta igual.
                    var yaRegistrada = recepcion.Mensajes?.StartsWith("43:") == true || recepcion.Mensajes?.Contains("| 43:") == true;
                    if (!recepcion.Recibida && !yaRegistrada)
                    {
                        factura.Estado = EstadoFactura.Devuelta;
                        factura.Mensajes = recepcion.Mensajes ?? "El SRI devolvió el comprobante sin indicar el motivo.";
                        await _context.SaveChangesAsync(ct);
                        return;
                    }
                    factura.Estado = EstadoFactura.Recibida;
                    factura.Mensajes = null;
                    await _context.SaveChangesAsync(ct);
                }

                foreach (var espera in EsperasAutorizacion)
                {
                    await Task.Delay(espera, ct);
                    var autorizacion = await _sri.ConsultarAsync(factura.ClaveAcceso, ct);
                    switch (autorizacion.Estado)
                    {
                        case EstadoAutorizacion.Autorizado:
                            factura.Estado = EstadoFactura.Autorizada;
                            factura.NumeroAutorizacion = autorizacion.NumeroAutorizacion;
                            factura.FechaAutorizacion = autorizacion.FechaAutorizacion;
                            factura.Mensajes = null;
                            await _context.SaveChangesAsync(ct);
                            await RegistrarComoIngresoAsync(factura, ct);
                            return;
                        case EstadoAutorizacion.NoAutorizado:
                            factura.Estado = EstadoFactura.NoAutorizada;
                            factura.Mensajes = autorizacion.Mensaje;
                            await _context.SaveChangesAsync(ct);
                            return;
                    }
                }

                factura.Estado = EstadoFactura.EnProceso;
                factura.Mensajes = "El SRI aún no termina de autorizarla. Consulta de nuevo en unos minutos.";
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "No se pudo contactar al SRI para la factura {Clave}", factura.ClaveAcceso);
                factura.Mensajes = "No se pudo contactar al SRI. La factura quedó firmada: reintenta el envío en unos minutos.";
                await _context.SaveChangesAsync(CancellationToken.None);
            }
        }

        /// <summary>En producción, la factura autorizada se suma a los ingresos del usuario (como cualquier comprobante del SRI).</summary>
        private async Task RegistrarComoIngresoAsync(FacturaEmitida factura, CancellationToken ct)
        {
            if (factura.Ambiente != AmbienteSri.Produccion) return;
            try
            {
                await _comprobantes.ImportarAsync(factura.UsuarioId, factura.ClaveAcceso, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "La factura {Clave} se autorizó pero no se pudo registrar como ingreso", factura.ClaveAcceso);
            }
        }

        private static FacturaEmitida ArmarFactura(EmitirFacturaRequest request)
        {
            var tipo = request.TipoIdentificacion;
            if (!FacturaXml.TiposIdentificacion.Todos.Contains(tipo))
                throw new BusinessException("Tipo de identificación no válido.");
            if (!FacturaXml.FormasPago.ContainsKey(request.FormaPago))
                throw new BusinessException("Forma de pago no válida.");

            var identificacion = request.Identificacion.Trim();
            var razonSocial = request.RazonSocial.Trim().ToUpperInvariant();
            switch (tipo)
            {
                case FacturaXml.TiposIdentificacion.Cedula when !Identificacion.EsCedulaValida(identificacion):
                    throw new BusinessException("La cédula del cliente no es válida.");
                case FacturaXml.TiposIdentificacion.Ruc when !Identificacion.EsRucValido(identificacion):
                    throw new BusinessException("El RUC del cliente no es válido.");
                case FacturaXml.TiposIdentificacion.ConsumidorFinal:
                    identificacion = FacturaXml.ConsumidorFinal;
                    razonSocial = "CONSUMIDOR FINAL";
                    break;
            }

            var detalles = request.Detalles.Select(d =>
            {
                var bruto = Math.Round(d.Cantidad * d.PrecioUnitario, 2, MidpointRounding.AwayFromZero);
                var descuento = Math.Round(d.Descuento, 2, MidpointRounding.AwayFromZero);
                if (descuento > bruto)
                    throw new BusinessException($"El descuento de \"{d.Descripcion}\" es mayor que su valor.");
                var subtotal = bruto - descuento;
                return new FacturaEmitidaDetalle
                {
                    Codigo = Limpiar(d.Codigo),
                    Descripcion = d.Descripcion.Trim(),
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Descuento = descuento,
                    Subtotal = subtotal,
                    Tarifa = d.Tarifa,
                    Iva = Math.Round(subtotal * FacturaXml.Porcentaje(d.Tarifa) / 100m, 2, MidpointRounding.AwayFromZero),
                };
            }).ToList();

            var factura = new FacturaEmitida
            {
                TipoIdentificacionComprador = tipo,
                IdentificacionComprador = identificacion,
                RazonSocialComprador = razonSocial,
                CorreoComprador = Limpiar(request.Correo),
                DireccionComprador = Limpiar(request.Direccion),
                FormaPago = request.FormaPago,
                Detalles = detalles,
                Subtotal = detalles.Sum(d => d.Subtotal),
                Descuento = detalles.Sum(d => d.Descuento),
                Iva = detalles.Sum(d => d.Iva),
            };
            factura.Total = factura.Subtotal + factura.Iva;

            if (factura.Total <= 0)
                throw new BusinessException("El total de la factura debe ser mayor que cero.");
            // Resolución del SRI: a consumidor final solo se factura hasta USD 50.
            if (tipo == FacturaXml.TiposIdentificacion.ConsumidorFinal && factura.Total > 50m)
                throw new BusinessException("A consumidor final solo puedes facturar hasta $50. Pide la cédula o RUC del cliente.");
            return factura;
        }

        private async Task<List<string>> PendientesAsync(int usuarioId, DatosEmisor datos)
        {
            var usuario = await UsuarioAsync(usuarioId);
            var firma = await _context.FirmasElectronicas.AsNoTracking().FirstOrDefaultAsync(f => f.UsuarioId == usuarioId);
            var pendientes = new List<string>();
            if (!AccesoPlan.TieneProfesional(usuario)) pendientes.Add("el plan Profesional");
            if (string.IsNullOrEmpty(usuario.Ruc)) pendientes.Add("tu RUC en el perfil");
            if (string.IsNullOrEmpty(usuario.RazonSocial)) pendientes.Add("tu razón social en el perfil");
            if (firma is null) pendientes.Add("tu firma electrónica");
            else if (firma.ValidaHasta < DateTime.UtcNow) pendientes.Add("renovar tu firma electrónica (venció)");
            if (datos.Id == 0 || string.IsNullOrWhiteSpace(datos.DireccionMatriz)) pendientes.Add("los datos de facturación (dirección y punto de emisión)");
            return pendientes;
        }

        private System.Security.Cryptography.X509Certificates.X509Certificate2 AbrirCertificado(FirmaElectronica firma) =>
            CertificadoFirma.Abrir(_protector.Unprotect(firma.ArchivoCifrado), Encoding.UTF8.GetString(_protector.Unprotect(firma.ClaveCifrada)));

        private async Task<FacturaEmitida> FacturaAsync(int usuarioId, int id) =>
            await _context.FacturasEmitidas.Include(f => f.Detalles).FirstOrDefaultAsync(f => f.UsuarioId == usuarioId && f.Id == id)
            ?? throw BusinessException.NotFound("Factura no encontrada.");

        private async Task<Usuario> UsuarioAsync(int usuarioId) =>
            await _context.Usuarios.FindAsync(usuarioId) ?? throw BusinessException.NotFound("Usuario no encontrado.");

        private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
