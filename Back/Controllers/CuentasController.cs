using Back.Dtos;
using Back.Helpers;
using Back.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers;

/// <summary>Mis cuentas: registro manual de cuentas del mes, ingreso declarado y día de pago (todos los planes).</summary>
[ApiController, Authorize, Route("api/cuentas")]
public class CuentasController(CuentasPersonalesService cuentas) : ControllerBase
{
    // Finanzas personales del propio usuario: no se comparten con un contador vinculado.
    private int UsuarioId => JwtHelper.GetUsuarioId(User);

    [HttpGet] public Task<MesCuentasDto> Mes(int anio, int mes, CancellationToken ct) => cuentas.MesAsync(UsuarioId, anio, mes, ct);
    [HttpPost] public Task<CuentaPersonalDto> Crear(CuentaPersonalRequest r, CancellationToken ct) => cuentas.CrearAsync(UsuarioId, r, ct);
    [HttpPut("{id:int}")] public Task<CuentaPersonalDto> Editar(int id, CuentaPersonalRequest r, CancellationToken ct) => cuentas.EditarAsync(UsuarioId, id, r, ct);
    [HttpDelete("{id:int}")] public async Task<IActionResult> Eliminar(int id, CancellationToken ct) { await cuentas.EliminarAsync(UsuarioId, id, ct); return NoContent(); }
    [HttpPut("{id:int}/pago")] public Task<CuentaPersonalDto> Pagar(int id, PagoCuentaRequest r, CancellationToken ct) => cuentas.PagarAsync(UsuarioId, id, r.Fecha, ct);
    [HttpDelete("{id:int}/pago")] public Task<CuentaPersonalDto> QuitarPago(int id, CancellationToken ct) => cuentas.QuitarPagoAsync(UsuarioId, id, ct);
    [HttpPut("ingreso")] public async Task<IActionResult> Ingreso(IngresoMensualRequest r, CancellationToken ct) { await cuentas.GuardarIngresoAsync(UsuarioId, r, ct); return NoContent(); }
    /// <summary>Cuentas pendientes del mes que terminó (planes de pago); 204 si no hay nada que cerrar.</summary>
    [HttpGet("cierre")]
    public async Task<ActionResult<CierreMesDto>> Cierre(CancellationToken ct) =>
        await cuentas.CierrePendienteAsync(UsuarioId, ct) is { Pendientes.Count: > 0 } cierre ? cierre : NoContent();
    [HttpPost("cierre")] public Task<CierreMesResultadoDto> Cerrar(CierreMesRequest r, CancellationToken ct) => cuentas.CerrarMesAsync(UsuarioId, r, ct);
    [HttpPost("copiar-mes-anterior")] public async Task<IActionResult> Copiar(CopiarMesRequest r, CancellationToken ct) => Ok(new { copiadas = await cuentas.CopiarMesAnteriorAsync(UsuarioId, r.Anio, r.Mes, ct) });
}
