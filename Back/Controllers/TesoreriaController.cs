using Back.Contabilidad;
using Back.Dtos;
using Back.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Back.Controllers;

public record VencimientoRequest(DateOnly Fecha);
[ApiController, Authorize, Route("api/contabilidad/tesoreria")]
public class TesoreriaController(CobrosPagosService pagos, BancosService bancos, ControlPeriodoService periodos) : ControllerBase
{
    private int UsuarioId => JwtHelper.GetContribuyenteId(User);
    [HttpGet("saldos")] public Task<List<SaldoComprobanteDto>> Saldos(CancellationToken ct) => pagos.SaldosAsync(UsuarioId, ct);
    [HttpGet("pagos")] public Task<List<PagoDto>> Pagos(CancellationToken ct) => pagos.PagosAsync(UsuarioId, ct);
    [HttpPost("pagos")] public async Task<IActionResult> Registrar(RegistrarPagoRequest request, CancellationToken ct) => Ok(new { id = await pagos.RegistrarAsync(UsuarioId, request, ct) });
    [HttpPost("pagos/{id:int}/anular")] public async Task<IActionResult> Anular(int id, AnularPagoRequest request, CancellationToken ct) { await pagos.AnularAsync(UsuarioId, id, request.Motivo, ct); return NoContent(); }
    [HttpPut("saldos/{id:int}/vencimiento")] public async Task<IActionResult> Vencimiento(int id, VencimientoRequest request, CancellationToken ct) { await pagos.VencimientoAsync(UsuarioId, id, request.Fecha, ct); return NoContent(); }
    [HttpGet("cuentas")] public Task<List<CuentaBancariaDto>> Cuentas(CancellationToken ct) => bancos.CuentasAsync(UsuarioId, ct);
    [HttpPost("cuentas")] public async Task<IActionResult> Cuenta(CuentaBancariaRequest request, CancellationToken ct) => Ok(new { id = await bancos.CrearCuentaAsync(UsuarioId, request, ct) });
    [HttpGet("cuentas/{id:int}/movimientos")] public Task<List<MovimientoBancarioDto>> Movimientos(int id, int anio, int mes, CancellationToken ct) => bancos.MovimientosAsync(UsuarioId, id, anio, mes, ct);
    [HttpPost("cuentas/{id:int}/movimientos")] public async Task<IActionResult> Movimiento(int id, MovimientoBancarioRequest request, CancellationToken ct) => Ok(new { id = await bancos.MovimientoAsync(UsuarioId, id, request, ct) });
    [HttpPost("movimientos/{id:int}/conciliar")] public async Task<IActionResult> Conciliar(int id, ConciliarRequest request, CancellationToken ct) { await bancos.ConciliarAsync(UsuarioId, id, request, ct); return NoContent(); }
    [HttpPost("movimientos/{id:int}/desconciliar")] public async Task<IActionResult> Desconciliar(int id, AnularPagoRequest request, CancellationToken ct) { await bancos.DesconciliarAsync(UsuarioId, id, request.Motivo, ct); return NoContent(); }
    [HttpPost("movimientos/{id:int}/revisar-otro")] public async Task<IActionResult> RevisarOtro(int id, AnularPagoRequest request, CancellationToken ct) { await bancos.RevisarOtroAsync(UsuarioId, id, request.Motivo, ct); return NoContent(); }
    [HttpPost("cuentas/{id:int}/importar"), RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Csv(int id, [FromForm] IFormFile archivo, CancellationToken ct)
    {
        if (archivo.Length > 5 * 1024 * 1024 || Path.GetExtension(archivo.FileName).ToLowerInvariant() != ".csv") throw new BusinessException("Adjunta un CSV de hasta 5 MB.");
        using var lector = new StreamReader(archivo.OpenReadStream()); var filas = CsvBancario.Leer(await lector.ReadToEndAsync(ct));
        await using var tx = await periodos.AbrirAsync(UsuarioId, filas.Select(f => f.Fecha), ct);
        var importadas = 0; var duplicadas = 0;
        foreach (var fila in filas) {
            try { await bancos.MovimientoAsync(UsuarioId, id, fila, ct); importadas++; }
            catch (BusinessException e) when (e.StatusCode == 409 && e.Message.Contains("ya está registrado")) { duplicadas++; }
        }
        await tx.ConfirmarAsync(ct);
        return Ok(new { importadas, duplicadas });
    }
}
