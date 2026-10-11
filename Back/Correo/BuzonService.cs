using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Back.Avisos;
using Back.Dtos;
using Back.Helpers;
using Back.Interfaces;
using Back.Models;
using Back.Services;
using Back.Sri;

namespace Back.Correo
{
    /// <summary>
    /// Buzón de reenvío: asigna a cada usuario una dirección propia, recibe los correos que llegan a ella
    /// (webhook del proveedor de correo entrante) y los deja en cola para importar sus XML con el SRI.
    /// </summary>
    public partial class BuzonService
    {
        private const string Alfabeto = "abcdefghjkmnpqrstuvwxyz23456789"; // sin caracteres ambiguos (l, 1, o, 0, i)
        private const int LargoCodigo = 10;
        private const int MaxIntentos = 3;

        private readonly IBuzonRepository _repository;
        private readonly ComprobanteService _comprobanteService;
        private readonly SenalCorreos _senal;
        private readonly AvisoService _avisos;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BuzonService> _logger;

        public BuzonService(
            IBuzonRepository repository,
            ComprobanteService comprobanteService,
            SenalCorreos senal,
            AvisoService avisos,
            IConfiguration configuration,
            ILogger<BuzonService> logger)
        {
            _repository = repository;
            _comprobanteService = comprobanteService;
            _senal = senal;
            _avisos = avisos;
            _configuration = configuration;
            _logger = logger;
        }

        private string? Dominio => _configuration["Correo:DominioEntrante"] is { Length: > 0 } d ? d.Trim().ToLowerInvariant() : null;

        public async Task<BuzonDto> ObtenerAsync(int usuarioId)
        {
            var buzon = await _repository.GetByUsuarioAsync(usuarioId) ?? await CrearAsync(usuarioId);
            var recientes = await _repository.GetCorreosRecientesAsync(usuarioId, 10);
            return BuzonDto.From(buzon, Dominio, recientes);
        }

        /// <summary>Cambia la dirección (por ejemplo si se filtró): la anterior deja de recibir.</summary>
        public async Task<BuzonDto> RegenerarAsync(int usuarioId)
        {
            var buzon = await _repository.GetByUsuarioAsync(usuarioId);
            if (buzon is null) return await ObtenerAsync(usuarioId);

            buzon.Codigo = await GenerarCodigoUnicoAsync();
            buzon.ConfirmacionGmailCodigo = null;
            buzon.ConfirmacionGmailEnlace = null;
            buzon.ConfirmacionGmailEn = null;
            await _repository.SaveChangesAsync();
            await _avisos.NotificarAsync(usuarioId, TipoAviso.BuzonRegenerado, "Cambiaste tu dirección de reenvío",
                "La dirección anterior dejó de recibir facturas. Actualiza el reenvío en tu correo con la nueva dirección.");
            return await ObtenerAsync(usuarioId);
        }

        /// <summary>
        /// Registra un correo entrante. Devuelve false si no corresponde a ningún buzón (se ignora).
        /// El procesamiento con el SRI ocurre en segundo plano para responder rápido al proveedor.
        /// </summary>
        public async Task<bool> RecibirAsync(CorreoEntrante correo)
        {
            var buzon = await BuscarBuzonAsync(correo.Destinatarios);
            if (buzon is null)
            {
                _logger.LogInformation("Correo entrante sin buzón conocido ({Destinatarios})", string.Join(", ", correo.Destinatarios));
                return false;
            }

            buzon.UltimoCorreoEn = DateTime.UtcNow;
            var registro = new CorreoRecibido
            {
                UsuarioId = buzon.UsuarioId,
                Remitente = Recortar(correo.Remitente, 254),
                Asunto = Recortar(correo.Asunto, 300),
                ArchivosXml = correo.Xmls.Count,
            };

            if (EsConfirmacionGmail(correo, out var codigo, out var enlace))
            {
                buzon.ConfirmacionGmailCodigo = codigo;
                buzon.ConfirmacionGmailEnlace = enlace;
                buzon.ConfirmacionGmailEn = DateTime.UtcNow;
                registro.Estado = EstadoCorreo.ConfirmacionGmail;
                registro.Detalle = "Gmail pide confirmar el reenvío: usa el código que aparece en Nudo.";
                registro.ProcesadoEn = DateTime.UtcNow;
            }
            else
            {
                var contenido = string.Join("\n", correo.Xmls.Append(correo.Texto));
                registro.ClavesEncontradas = ClaveAcceso.Extraer(contenido).Count;
                if (registro.ClavesEncontradas == 0)
                {
                    registro.Estado = EstadoCorreo.SinComprobantes;
                    registro.Detalle = "No trae XML ni claves de acceso de comprobantes electrónicos.";
                    registro.ProcesadoEn = DateTime.UtcNow;
                }
                else
                {
                    registro.Contenido = contenido;
                }
            }

            await _repository.AddCorreoAsync(registro); // guarda también el buzón (mismo DbContext)
            if (registro.Estado == EstadoCorreo.Pendiente) _senal.Avisar();
            return true;
        }

        /// <summary>Importa los correos pendientes (lo llama el procesador en segundo plano).</summary>
        public async Task<int> ProcesarPendientesAsync(CancellationToken ct)
        {
            var pendientes = await _repository.GetPendientesAsync(20);
            foreach (var correo in pendientes)
            {
                correo.Intentos++;
                try
                {
                    var claves = ClaveAcceso.Extraer(correo.Contenido ?? string.Empty);
                    var importados = 0;
                    var ingresos = 0;
                    var problemas = 0;
                    foreach (var lote in claves.Chunk(ComprobanteService.MaxClavesPorImportacion))
                    {
                        var r = await _comprobanteService.ImportarAsync(correo.UsuarioId, string.Join(" ", lote), ct);
                        importados += r.Importados;
                        ingresos += r.Ingresos;
                        problemas += r.ConProblemas;
                    }

                    correo.Estado = EstadoCorreo.Procesado;
                    correo.Importados = importados;
                    correo.Ingresos = ingresos;
                    correo.Detalle = importados switch
                    {
                        0 when problemas == 0 => "Los comprobantes ya estaban registrados.",
                        0 => "El SRI no autorizó los comprobantes de este correo.",
                        _ => problemas > 0 ? $"{problemas} comprobantes no se pudieron verificar con el SRI." : null,
                    };
                    correo.Contenido = null;
                    correo.ProcesadoEn = DateTime.UtcNow;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "No se pudo procesar el correo {CorreoId}", correo.Id);
                    if (correo.Intentos >= MaxIntentos)
                    {
                        correo.Estado = EstadoCorreo.Error;
                        correo.Detalle = "No se pudo procesar este correo. Reenvíalo o sube sus XML en Traer facturas.";
                        correo.Contenido = null;
                        correo.ProcesadoEn = DateTime.UtcNow;
                    }
                }
                await _repository.SaveChangesAsync();
            }
            return pendientes.Count;
        }

        private async Task<BuzonCorreo?> BuscarBuzonAsync(IEnumerable<string> destinatarios)
        {
            foreach (var direccion in destinatarios.SelectMany(Direcciones))
            {
                var codigo = CodigoDe(direccion);
                if (codigo is null) continue;
                var buzon = await _repository.GetByCodigoAsync(codigo);
                if (buzon is not null) return buzon;
            }
            return null;
        }

        /// <summary>Local-part de la dirección (o lo que va tras "+", para alias tipo facturas+codigo@dominio).</summary>
        private string? CodigoDe(string direccion)
        {
            var arroba = direccion.LastIndexOf('@');
            if (arroba <= 0) return null;
            if (Dominio is not null && !direccion[(arroba + 1)..].Equals(Dominio, StringComparison.OrdinalIgnoreCase)) return null;

            var local = direccion[..arroba].ToLowerInvariant();
            var mas = local.LastIndexOf('+');
            var codigo = mas >= 0 ? local[(mas + 1)..] : local;
            return codigo.Length == LargoCodigo && codigo.All(Alfabeto.Contains) ? codigo : null;
        }

        [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}")]
        private static partial Regex Email();

        private static IEnumerable<string> Direcciones(string texto) => Email().Matches(texto ?? string.Empty).Select(m => m.Value);

        [GeneratedRegex(@"\(#(\d{6,12})\)")]
        private static partial Regex CodigoAsunto();

        [GeneratedRegex(@"\b(\d{9})\b")]
        private static partial Regex CodigoCuerpo();

        [GeneratedRegex(@"https://mail-settings\.google\.com/\S+")]
        private static partial Regex EnlaceConfirmacion();

        /// <summary>Correo de Gmail "Gmail Forwarding Confirmation": trae el código para autorizar el reenvío.</summary>
        private static bool EsConfirmacionGmail(CorreoEntrante correo, out string? codigo, out string? enlace)
        {
            codigo = null;
            enlace = null;
            if (!correo.Remitente.Equals("forwarding-noreply@google.com", StringComparison.OrdinalIgnoreCase)) return false;

            codigo = CodigoAsunto().Match(correo.Asunto) is { Success: true } a ? a.Groups[1].Value
                : CodigoCuerpo().Match(correo.Texto) is { Success: true } c ? c.Groups[1].Value : null;
            enlace = EnlaceConfirmacion().Match(correo.Texto) is { Success: true } e ? e.Value.TrimEnd('.', ')', '>') : null;
            return true;
        }

        private async Task<BuzonCorreo> CrearAsync(int usuarioId)
        {
            var buzon = new BuzonCorreo { UsuarioId = usuarioId, Codigo = await GenerarCodigoUnicoAsync() };
            await _repository.AddAsync(buzon);
            return buzon;
        }

        private async Task<string> GenerarCodigoUnicoAsync()
        {
            while (true)
            {
                var codigo = new string(Enumerable.Range(0, LargoCodigo).Select(_ => Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)]).ToArray());
                if (!await _repository.CodigoExisteAsync(codigo)) return codigo;
            }
        }

        private static string Recortar(string texto, int maximo) => texto.Length <= maximo ? texto : texto[..maximo];
    }

    /// <summary>Despierta al procesador apenas llega un correo con comprobantes.</summary>
    public class SenalCorreos
    {
        private readonly SemaphoreSlim _senal = new(0);

        public void Avisar()
        {
            if (_senal.CurrentCount == 0) _senal.Release();
        }

        public Task<bool> EsperarAsync(TimeSpan maximo, CancellationToken ct) => _senal.WaitAsync(maximo, ct);
    }

    /// <summary>Procesa en segundo plano los correos pendientes (al recibir uno, o cada minuto).</summary>
    public class ProcesadorCorreosWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly SenalCorreos _senal;
        private readonly ILogger<ProcesadorCorreosWorker> _logger;

        public ProcesadorCorreosWorker(IServiceScopeFactory scopes, SenalCorreos senal, ILogger<ProcesadorCorreosWorker> logger)
        {
            _scopes = scopes;
            _senal = senal;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    int procesados;
                    do
                    {
                        using var scope = _scopes.CreateScope();
                        procesados = await scope.ServiceProvider.GetRequiredService<BuzonService>().ProcesarPendientesAsync(stoppingToken);
                    } while (procesados > 0 && !stoppingToken.IsCancellationRequested);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error procesando correos entrantes");
                }

                await _senal.EsperarAsync(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
