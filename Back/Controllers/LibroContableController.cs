using Back.Contabilidad;
using Back.Dtos;
using Back.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Back.Controllers;

[ApiController, Authorize, Route("api/contabilidad/libros")]
public class LibroContableController(LibroContableService libro) : ControllerBase
{
    private int Uid => JwtHelper.GetContribuyenteId(User);
    [HttpGet("cuentas")] public Task<List<CuentaContableDto>> Cuentas(CancellationToken ct) => libro.CuentasAsync(Uid, ct);
    [HttpPost("cuentas/base")] public async Task<IActionResult> Base(CancellationToken ct) { await libro.InicializarAsync(Uid, ct); return NoContent(); }
    [HttpPost("cuentas")] public async Task<IActionResult> Cuenta(CuentaContableRequest r, CancellationToken ct) => Ok(new { id = await libro.CrearCuentaAsync(Uid, r, ct) });
    [HttpGet("asientos")] public Task<List<AsientoDto>> Asientos(int anio, int mes, CancellationToken ct) => libro.AsientosAsync(Uid, anio, mes, ct);
    [HttpPost("asientos")] public async Task<IActionResult> Crear(AsientoRequest r, CancellationToken ct) => Ok(new { id = await libro.GuardarAsync(Uid, null, r, ct) });
    [HttpPut("asientos/{id:int}")] public async Task<IActionResult> Editar(int id, AsientoRequest r, CancellationToken ct) => Ok(new { id = await libro.GuardarAsync(Uid, id, r, ct) });
    [HttpPost("asientos/{id:int}/contabilizar")] public async Task<IActionResult> Contabilizar(int id, CancellationToken ct) { await libro.ContabilizarAsync(Uid, id, ct); return NoContent(); }
    [HttpPost("asientos/{id:int}/anular")] public async Task<IActionResult> Anular(int id, AnularPagoRequest r, CancellationToken ct) { await libro.AnularAsync(Uid, id, r.Motivo, ct); return NoContent(); }
    [HttpPost("comprobantes/{id:int}")] public async Task<IActionResult> Comprobante(int id, CancellationToken ct) => Ok(new { id = await libro.DesdeComprobanteAsync(Uid, id, ct) });
    [HttpPost("pagos/{id:int}")] public async Task<IActionResult> Pago(int id, CancellationToken ct) => Ok(new { id = await libro.DesdePagoAsync(Uid, id, ct) });
    [HttpGet("estados")] public Task<EstadosContablesDto> Estados(DateOnly desde, DateOnly hasta, CancellationToken ct) => libro.EstadosAsync(Uid, desde, hasta, ct);
    [HttpGet("cuentas/{id:int}/mayor")] public Task<List<MayorDto>> Mayor(int id, DateOnly desde, DateOnly hasta, CancellationToken ct) => libro.MayorAsync(Uid, id, desde, hasta, ct);
}
