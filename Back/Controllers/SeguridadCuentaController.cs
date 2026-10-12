using System.Security.Claims;
using Back.Dtos;
using Back.Helpers;
using Back.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers;

[ApiController, Authorize, Route("api/cuenta/seguridad")]
public class SeguridadCuentaController(SeguridadCuentaService servicio) : ControllerBase
{
    private int UsuarioId => JwtHelper.GetUsuarioId(User);
    private string Sesion => User.FindFirstValue("jti")!;
    [HttpGet] public Task<SeguridadCuentaDto> Estado(CancellationToken ct) => servicio.EstadoAsync(UsuarioId, ct);
    [HttpPut("clave")] public async Task<IActionResult> Cambiar(CambiarClaveRequest request, CancellationToken ct)
    { await servicio.CambiarClaveAsync(UsuarioId, request, ct); return NoContent(); }
    [HttpPost("verificacion")] public async Task<IActionResult> Solicitar(CancellationToken ct)
    { await servicio.SolicitarVerificacionAsync(UsuarioId, ct); return NoContent(); }
    [HttpGet("sesiones")] public Task<List<SesionCuentaDto>> Sesiones(CancellationToken ct) => servicio.SesionesAsync(UsuarioId, Sesion, ct);
    [HttpDelete("sesiones/{id:guid}")] public async Task<IActionResult> Revocar(Guid id, CancellationToken ct)
    { await servicio.RevocarSesionAsync(UsuarioId, id.ToString(), ct); return NoContent(); }
    [HttpDelete("sesiones/otras")] public async Task<IActionResult> Otras(CancellationToken ct)
    { await servicio.RevocarOtrasAsync(UsuarioId, Sesion, ct); return NoContent(); }
    [HttpDelete("sesiones/actual")] public async Task<IActionResult> Salir(CancellationToken ct)
    { await servicio.RevocarSesionAsync(UsuarioId, Sesion, ct); return NoContent(); }
}
