using System.Text;
using Back.Dtos;
using Back.Helpers;
using Back.Pagos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Back.Controllers
{
    /// <summary>Planes de pago: precios, suscripción con tarjeta (Kushki), cancelación y webhooks.</summary>
    [ApiController]
    [Route("api/suscripcion")]
    public class SuscripcionController : ControllerBase
    {
        private readonly SuscripcionService _service;

        public SuscripcionController(SuscripcionService service)
        {
            _service = service;
        }

        /// <summary>Precios (con IVA), días de prueba y clave pública de Kushki. Público: lo usa también la landing.</summary>
        [HttpGet("planes")]
        [AllowAnonymous]
        public ActionResult<CatalogoPagosDto> GetPlanes() => Ok(_service.Catalogo());

        [HttpGet("uso")]
        [Authorize]
        public async Task<ActionResult<UsoPlanDto>> GetUso() => Ok(await _service.UsoAsync(JwtHelper.GetUsuarioId(User)));

        // GET api/suscripcion  → 204 si está en el plan Personal (sin suscripción)
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<SuscripcionDto>> Get() =>
            await _service.ObtenerAsync(JwtHelper.GetUsuarioId(User)) is { } s ? Ok(s) : NoContent();

        /// <summary>Suscribe al usuario con el token de tarjeta que generó Kushki en el navegador.</summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<SuscripcionResultadoDto>> Post(SuscribirRequest request, CancellationToken ct) =>
            Ok(await _service.SuscribirAsync(JwtHelper.GetUsuarioId(User), request, ct));

        [HttpDelete]
        [Authorize]
        public async Task<ActionResult<SuscripcionResultadoDto>> Delete(CancellationToken ct) =>
            Ok(await _service.CancelarAsync(JwtHelper.GetUsuarioId(User), ct));

        /// <summary>
        /// Webhook de Kushki (cobros aprobados, rechazados, reintentos y cancelaciones). Se verifica la firma
        /// HMAC-SHA256 de X-Kushki-Signature sobre el cuerpo exacto recibido.
        /// </summary>
        [HttpPost("/api/pagos/kushki/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook([FromServices] IOptions<KushkiOptions> opciones)
        {
            using var lector = new StreamReader(Request.Body, Encoding.UTF8);
            var cuerpo = await lector.ReadToEndAsync();
            if (!KushkiClient.FirmaValida(cuerpo, Request.Headers["X-Kushki-Id"], Request.Headers["X-Kushki-Signature"], opciones.Value.FirmaWebhook))
                return Unauthorized();

            await _service.ProcesarWebhookAsync(cuerpo);
            // Se responde 200 aunque la suscripción no exista, para que Kushki no reintente sin fin.
            return Ok();
        }
    }
}
