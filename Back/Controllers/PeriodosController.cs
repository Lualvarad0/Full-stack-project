using System.ComponentModel.DataAnnotations;
using Back.Contabilidad;
using Back.Data;
using Back.Helpers;
using Back.Models;
using Back.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Back.Controllers;

public record ReabrirPeriodoRequest([Required, TextoPlano, StringLength(1000, MinimumLength = 10)] string Motivo);
[ApiController, Authorize, Route("api/contabilidad")]
public class PeriodosController(PeriodosService servicio, AppDbContext db) : ControllerBase
{
    private int UsuarioId => JwtHelper.GetContribuyenteId(User);
    [HttpGet("periodos/{anio:int}/{mes:int}")] public Task<RevisionPeriodoDto> Revisar(int anio, int mes, CancellationToken ct) => servicio.RevisarAsync(UsuarioId, anio, mes, ct);
    [HttpPost("periodos/{anio:int}/{mes:int}/cerrar")] public async Task<IActionResult> Cerrar(int anio, int mes, CancellationToken ct)
    { await servicio.CerrarAsync(UsuarioId, anio, mes, ct); return NoContent(); }
    [HttpPost("periodos/{anio:int}/{mes:int}/reabrir")] public async Task<IActionResult> Reabrir(int anio, int mes, ReabrirPeriodoRequest request, CancellationToken ct)
    { await servicio.ReabrirAsync(UsuarioId, anio, mes, request.Motivo, ct); return NoContent(); }
    [HttpGet("auditoria")] public Task<List<AuditoriaContable>> Auditoria(CancellationToken ct) => db.Set<AuditoriaContable>().AsNoTracking().Where(x => x.UsuarioId == UsuarioId).OrderByDescending(x => x.Fecha).Take(200).ToListAsync(ct);
}
