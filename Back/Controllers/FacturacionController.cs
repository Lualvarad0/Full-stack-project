using System.Text;
using System.ComponentModel.DataAnnotations;
using Back.Dtos;
using Back.Facturacion;
using Back.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers
{
    /// <summary>Facturación electrónica: firma, datos de emisión y facturas emitidas.</summary>
    [ApiController]
    [Authorize]
    [Route("api/facturacion")]
    public class FacturacionController : ControllerBase
    {
        private readonly FacturacionService _service;

        public FacturacionController(FacturacionService service)
        {
            _service = service;
        }

        private int UsuarioId => JwtHelper.GetContribuyenteId(User);

        /// <summary>Facturas emitidas este mes frente al límite del plan.</summary>
        [HttpGet("cupo")]
        public Task<CupoPlanDto> GetCupo(CancellationToken ct) => _service.CupoFacturasAsync(UsuarioId, ct);

        // GET api/facturacion/firma  → 204 si aún no hay firma
        [HttpGet("firma")]
        public async Task<ActionResult<FirmaElectronicaDto>> GetFirma() =>
            await _service.ObtenerFirmaAsync(UsuarioId) is { } firma ? Ok(firma) : NoContent();

        /// <summary>Sube (o reemplaza) la firma: multipart con "archivo" (.p12/.pfx) y "clave".</summary>
        [HttpPost("firma")]
        [RequestSizeLimit(100 * 1024)]
        public async Task<ActionResult<FirmaElectronicaDto>> PostFirma([FromForm] IFormFile? archivo, [FromForm, StringLength(200)] string? clave)
        {
            if (archivo is null)
                throw new BusinessException("Adjunta el archivo .p12 de tu firma electrónica.");
            return Ok(await _service.GuardarFirmaAsync(UsuarioId, archivo, clave ?? string.Empty));
        }

        [HttpDelete("firma")]
        public async Task<IActionResult> DeleteFirma()
        {
            await _service.EliminarFirmaAsync(UsuarioId);
            return NoContent();
        }

        [HttpGet("datos")]
        public async Task<ActionResult<DatosEmisorDto>> GetDatos() => Ok(await _service.ObtenerDatosAsync(UsuarioId));

        [HttpPut("datos")]
        public async Task<ActionResult<DatosEmisorDto>> PutDatos(DatosEmisorRequest request) =>
            Ok(await _service.GuardarDatosAsync(UsuarioId, request));

        [HttpGet("facturas")]
        public async Task<ActionResult<List<FacturaEmitidaDto>>> GetFacturas() => Ok(await _service.ListarAsync(UsuarioId));

        [HttpGet("facturas/{id:int}")]
        public async Task<ActionResult<FacturaEmitidaDto>> GetFactura(int id) => Ok(await _service.ObtenerAsync(UsuarioId, id));

        [HttpGet("facturas/{id:int}/xml")]
        public async Task<IActionResult> GetXml(int id) =>
            File(Encoding.UTF8.GetBytes(await _service.ObtenerXmlAsync(UsuarioId, id)), "application/xml", $"factura-{id}.xml");

        [HttpGet("facturas/{id:int}/pdf")]
        public async Task<IActionResult> GetPdf(int id) =>
            File(await _service.ObtenerPdfAsync(UsuarioId, id), "application/pdf", $"factura-{id}.pdf");

        /// <summary>Firma la factura, la envía al SRI y espera su autorización unos segundos.</summary>
        [HttpPost("facturas")]
        public async Task<ActionResult<FacturaEmitidaDto>> PostFactura(EmitirFacturaRequest request, CancellationToken ct) =>
            Ok(await _service.EmitirAsync(UsuarioId, request, ct));

        /// <summary>Borra una factura que nunca se autorizó (sin enviar, devuelta o no autorizada).</summary>
        [HttpDelete("facturas/{id:int}")]
        public async Task<IActionResult> DeleteFactura(int id)
        {
            await _service.EliminarAsync(UsuarioId, id);
            return NoContent();
        }

        /// <summary>Valida la factura propia para gestionar su anulación en SRI en Línea; no la anula.</summary>
        [HttpPost("facturas/{id:int}/anular")]
        public async Task<ActionResult<FacturaEmitidaDto>> Anular(int id) => Ok(await _service.AnularAsync(UsuarioId, id));

        [HttpGet("servicios")]
        public async Task<ActionResult<List<ServicioFacturableDto>>> GetServicios() => Ok(await _service.ServiciosAsync(UsuarioId));

        [HttpPost("servicios")]
        public async Task<ActionResult<ServicioFacturableDto>> PostServicio(ServicioFacturableRequest request) =>
            Ok(await _service.GuardarServicioAsync(UsuarioId, null, request));

        [HttpPut("servicios/{id:int}")]
        public async Task<ActionResult<ServicioFacturableDto>> PutServicio(int id, ServicioFacturableRequest request) =>
            Ok(await _service.GuardarServicioAsync(UsuarioId, id, request));

        [HttpDelete("servicios/{id:int}")]
        public async Task<IActionResult> DeleteServicio(int id)
        {
            await _service.EliminarServicioAsync(UsuarioId, id);
            return NoContent();
        }

        [HttpPost("facturas/{id:int}/reintentar")]
        public async Task<ActionResult<FacturaEmitidaDto>> Reintentar(int id, CancellationToken ct) =>
            Ok(await _service.ReintentarAsync(UsuarioId, id, ct));
    }
}
