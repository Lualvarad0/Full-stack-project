using System.Net;
using Back.Data;
using Back.Dtos;
using Back.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Back.Avisos
{
    /// <summary>
    /// Avisos de seguridad al cliente: cada ingreso al sistema y cada cambio de configuración se registra
    /// (visible en Configuración → Actividad de la cuenta) y se envía a su correo.
    /// </summary>
    public class AvisoService
    {
        private readonly AppDbContext _context;
        private readonly ColaCorreoSaliente _cola;
        private readonly SmtpOptions _smtp;
        private readonly IHttpContextAccessor _http;
        private readonly IConfiguration _configuration;

        public AvisoService(AppDbContext context, ColaCorreoSaliente cola, IOptions<SmtpOptions> smtp, IHttpContextAccessor http, IConfiguration configuration)
        {
            _context = context;
            _cola = cola;
            _smtp = smtp.Value;
            _http = http;
            _configuration = configuration;
        }

        public async Task NotificarAsync(int usuarioId, TipoAviso tipo, string titulo, string detalle)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario is null) return;

            var peticion = _http.HttpContext;
            var aviso = new AvisoCuenta
            {
                UsuarioId = usuario.Id,
                Tipo = tipo,
                Titulo = titulo,
                Detalle = detalle,
                Ip = peticion?.Connection.RemoteIpAddress?.ToString(),
                Dispositivo = DescribirDispositivo(peticion?.Request.Headers.UserAgent.ToString()),
            };

            // El usuario puede apagar el correo de cada ingreso (Configuración → Sistema); el aviso queda en su actividad.
            var enviar = tipo != TipoAviso.Ingreso || usuario.AvisarIngresosPorCorreo;
            aviso.EnviadoPorCorreo = enviar && _smtp.Configurado && _cola.Encolar(Componer(usuario, aviso));
            _context.AvisosCuenta.Add(aviso);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AvisoCuentaDto>> ObtenerRecientesAsync(int usuarioId) =>
            (await _context.AvisosCuenta.AsNoTracking()
                .Where(a => a.UsuarioId == usuarioId)
                .OrderByDescending(a => a.Fecha)
                .Take(30)
                .ToListAsync())
            .Select(AvisoCuentaDto.From)
            .ToList();

        private CorreoSaliente Componer(Usuario usuario, AvisoCuenta aviso)
        {
            var app = _configuration["Frontend:Url"] ?? "http://localhost:4200";
            var hora = TimeZoneInfo.ConvertTimeFromUtc(aviso.Fecha, ZonaEcuador()).ToString("dd/MM/yyyy HH:mm");
            var origen = string.Join(" · ", new[] { aviso.Dispositivo, aviso.Ip is null ? null : $"IP {aviso.Ip}" }.Where(x => x is not null));
            static string e(string? texto) => WebUtility.HtmlEncode(texto ?? string.Empty);

            var html = $"""
                <div style="font-family:Inter,Arial,sans-serif;max-width:520px;margin:auto;color:#252b31">
                  <p style="font-size:18px;font-weight:600;margin:0 0 16px">Nudo</p>
                  <p>Hola, {e(usuario.Nombre)}:</p>
                  <p style="font-size:16px;font-weight:600">{e(aviso.Titulo)}</p>
                  <p>{e(aviso.Detalle)}</p>
                  <p style="color:#606b6c;font-size:13px">{e(hora)} (hora de Ecuador){(origen.Length > 0 ? " · " + e(origen) : "")}</p>
                  <p style="background:#fffbeb;border:1px solid #fde68a;border-radius:10px;padding:12px;font-size:13px">
                    ¿No fuiste tú? Ingresa a <a href="{e(app)}/configuracion?seccion=sistema">Nudo</a> y revisa la actividad de tu cuenta.
                  </p>
                  <p style="color:#606b6c;font-size:12px;margin-top:24px">Nudo · Diseño y desarrollo: <a href="https://www.alvarado-dev.com/" style="color:#606b6c">Cornerstone Develop</a></p>
                </div>
                """;
            var texto = $"Hola, {usuario.Nombre}:\n\n{aviso.Titulo}\n{aviso.Detalle}\n{hora} (hora de Ecuador) {origen}\n\n¿No fuiste tú? Revisa tu cuenta en {app}/configuracion\n\nNudo · Diseño y desarrollo: Cornerstone Develop";
            return new CorreoSaliente(usuario.Correo, $"Nudo: {aviso.Titulo}", html, texto);
        }

        /// <summary>"Chrome en Windows", "Safari en iPhone"… a partir del User-Agent.</summary>
        public static string? DescribirDispositivo(string? agente)
        {
            if (string.IsNullOrWhiteSpace(agente)) return null;
            var navegador = agente switch
            {
                _ when agente.Contains("Edg/") => "Edge",
                _ when agente.Contains("OPR/") => "Opera",
                _ when agente.Contains("Firefox/") => "Firefox",
                _ when agente.Contains("Chrome/") => "Chrome",
                _ when agente.Contains("Safari/") => "Safari",
                _ => "Navegador",
            };
            var sistema = agente switch
            {
                _ when agente.Contains("Windows") => "Windows",
                _ when agente.Contains("iPhone") => "iPhone",
                _ when agente.Contains("iPad") => "iPad",
                _ when agente.Contains("Android") => "Android",
                _ when agente.Contains("Mac OS X") => "Mac",
                _ when agente.Contains("Linux") => "Linux",
                _ => null,
            };
            return sistema is null ? navegador : $"{navegador} en {sistema}";
        }

        private static TimeZoneInfo ZonaEcuador()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil"); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("ECT", TimeSpan.FromHours(-5), "Ecuador", "Ecuador"); }
        }
    }
}
