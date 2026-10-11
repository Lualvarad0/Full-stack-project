using System.Security.Cryptography;
using System.Text;
using Back.Correo;
using Back.Dtos;
using Back.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers
{
    /// <summary>Buzón de reenvío: dirección propia del usuario y webhook de correo entrante.</summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CorreoController : ControllerBase
    {
        public const string EncabezadoSecreto = "X-Nudo-Webhook-Secret";
        private const long MaxBytesCorreo = 25 * 1024 * 1024;

        private readonly BuzonService _buzonService;
        private readonly IConfiguration _configuration;

        public CorreoController(BuzonService buzonService, IConfiguration configuration)
        {
            _buzonService = buzonService;
            _configuration = configuration;
        }

        private int UsuarioId => JwtHelper.GetUsuarioId(User);

        [HttpGet]
        public async Task<ActionResult<BuzonDto>> GetBuzon() =>
            Ok(await _buzonService.ObtenerAsync(UsuarioId));

        [HttpPost("regenerar")]
        public async Task<ActionResult<BuzonDto>> Regenerar() =>
            Ok(await _buzonService.RegenerarAsync(UsuarioId));

        /// <summary>
        /// Webhook del proveedor de correo entrante (Cloudflare Email Workers, SendGrid Inbound Parse, Mailgun).
        /// Acepta el correo MIME crudo (Content-Type message/rfc822) o un formulario multipart con adjuntos.
        /// Se autentica con el encabezado X-Nudo-Webhook-Secret (Correo:SecretoWebhook).
        /// El destinatario puede venir en ?para= (recomendado: la dirección real de entrega).
        /// </summary>
        [HttpPost("entrante")]
        [AllowAnonymous]
        [RequestSizeLimit(MaxBytesCorreo)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxBytesCorreo)]
        public async Task<IActionResult> Entrante([FromQuery] string? para, CancellationToken ct)
        {
            var secreto = _configuration["Correo:SecretoWebhook"];
            if (string.IsNullOrEmpty(secreto))
                return Problem("El correo entrante no está configurado (Correo:SecretoWebhook).", statusCode: StatusCodes.Status503ServiceUnavailable);

            var recibido = Request.Headers[EncabezadoSecreto].ToString();
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recibido), Encoding.UTF8.GetBytes(secreto)))
                return Unauthorized();

            var extras = string.IsNullOrWhiteSpace(para) ? Array.Empty<string>() : new[] { para };
            var correo = Request.HasFormContentType
                ? await CorreoEntranteParser.DesdeFormularioAsync(await Request.ReadFormAsync(ct), extras, ct)
                : await CorreoEntranteParser.DesdeMimeAsync(Request.Body, extras, ct);

            // 202 aunque no haya buzón: así el proveedor no reintenta correos que nunca se podrán entregar.
            await _buzonService.RecibirAsync(correo);
            return Accepted();
        }
    }
}
