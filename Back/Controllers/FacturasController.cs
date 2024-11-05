using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class FacturasController : ControllerBase
{
    private readonly FacturaService _facturaService;

    public FacturasController(FacturaService facturaService)
    {
        _facturaService = facturaService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFacturas()
    {
        var facturas = await _facturaService.ObtenerFacturasAsync();
        return Ok(facturas);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFactura(int id)
    {
        var factura = await _facturaService.ObtenerFacturaPorIdAsync(id);
        return factura != null ? Ok(factura) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateFactura(Factura factura)
    {
        await _facturaService.CrearFacturaAsync(factura);
        return CreatedAtAction(nameof(GetFactura), new { id = factura.FacturaId }, factura);
    }
}
