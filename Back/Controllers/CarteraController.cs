using Back.Contabilidad;
using Back.Dtos;
using Back.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Back.Controllers;

[ApiController, Authorize, Route("api/cartera")]
public class CarteraController(CarteraService servicio) : ControllerBase
{
    private int UsuarioId => JwtHelper.GetUsuarioId(User);
    [HttpGet] public Task<List<ContribuyenteAccesibleDto>> Lista(CancellationToken ct) => servicio.ContribuyentesAsync(UsuarioId, ct);
    [HttpGet("cupo")] public Task<CupoPlanDto> Cupo(CancellationToken ct) => servicio.CupoAsync(UsuarioId, ct);
    [HttpGet("solicitudes")] public Task<List<VinculoContadorDto>> Solicitudes(CancellationToken ct) => servicio.SolicitudesAsync(UsuarioId, ct);
    [HttpPost("solicitudes")] public async Task<IActionResult> Solicitar(SolicitarVinculoRequest request, CancellationToken ct)
    { await servicio.SolicitarAsync(UsuarioId, request, ct); return Ok(new { mensaje = "Si la cuenta está registrada y activa, recibirá tu solicitud. No tendrás acceso hasta que su titular la autorice." }); }
    [HttpPost("solicitudes/{id:int}/aceptar")] public async Task<IActionResult> Aceptar(int id, CancellationToken ct)
    { await servicio.ResponderAsync(UsuarioId, id, true, ct); return NoContent(); }
    [HttpPost("solicitudes/{id:int}/rechazar")] public async Task<IActionResult> Rechazar(int id, CancellationToken ct)
    { await servicio.ResponderAsync(UsuarioId, id, false, ct); return NoContent(); }
    [HttpDelete("accesos/{id:int}")] public async Task<IActionResult> Revocar(int id, CancellationToken ct)
    { await servicio.RevocarAsync(UsuarioId, id, ct); return NoContent(); }
}
