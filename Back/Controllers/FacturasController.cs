using Back.Dtos;
using Back.Helpers;
using Back.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class FacturasController : ControllerBase
    {
        private readonly FacturaService _facturaService;

        public FacturasController(FacturaService facturaService)
        {
            _facturaService = facturaService;
        }

        // GET api/facturas?desde=2026-10-01&hasta=2026-11-01&clienteId=3  (hasta es exclusivo)
        private int UsuarioId => JwtHelper.GetContribuyenteId(User);

        [HttpGet]
        public async Task<ActionResult<List<FacturaDto>>> GetFacturas(
            [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? clienteId) =>
            Ok(await _facturaService.ObtenerFacturasAsync(UsuarioId, ToUtc(desde), ToUtc(hasta), clienteId));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<FacturaDto>> GetFactura(int id) =>
            Ok(await _facturaService.ObtenerFacturaPorIdAsync(UsuarioId, id));

        [HttpPost]
        public async Task<ActionResult<FacturaDto>> CreateFactura(FacturaRequest request)
        {
            var factura = await _facturaService.CrearFacturaAsync(request, JwtHelper.GetUsuarioId(User));
            return CreatedAtAction(nameof(GetFactura), new { id = factura.Id }, factura);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteFactura(int id)
        {
            await _facturaService.EliminarFacturaAsync(UsuarioId, id);
            return NoContent();
        }

        // Npgsql exige DateTime UTC para columnas timestamptz.
        private static DateTime? ToUtc(DateTime? fecha) => fecha switch
        {
            null => null,
            { Kind: DateTimeKind.Utc } f => f,
            var f => DateTime.SpecifyKind(f.Value, DateTimeKind.Utc),
        };
    }
}
