using Back.Data;
using Back.Helpers;
using Back.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Back.Contabilidad;

/// <summary>Serializa las modificaciones y los cierres por titular; evita cerrar mientras se guarda información.</summary>
public class ControlPeriodoService(AppDbContext db, IHttpContextAccessor http)
{
    public int ActorId(int usuarioId) => http.HttpContext?.User.Identity?.IsAuthenticated == true ? JwtHelper.GetUsuarioId(http.HttpContext.User) : usuarioId;
    public async Task<MutacionPeriodo> AbrirAsync(int usuarioId, IEnumerable<DateOnly> fechas, CancellationToken ct = default)
    {
        var propia = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            await db.Usuarios.FromSqlInterpolated($"SELECT * FROM usuarios WHERE id = {usuarioId} FOR UPDATE").ToListAsync(ct);
            await ExigirAbiertosAsync(usuarioId, fechas, ct);
            return new MutacionPeriodo(propia);
        }
        catch { if (propia is not null) await propia.DisposeAsync(); throw; }
    }
    public async Task ExigirAbiertosAsync(int usuarioId, IEnumerable<DateOnly> fechas, CancellationToken ct = default)
    {
        var claves = fechas.Select(f => f.Year * 100 + f.Month).Distinct().ToArray();
        if (await db.Set<PeriodoContable>().AnyAsync(p => p.UsuarioId == usuarioId && p.Estado == EstadoPeriodo.Cerrado && claves.Contains(p.Anio * 100 + p.Mes), ct))
            throw BusinessException.Conflict("El período está cerrado. Reábrelo con una justificación antes de modificar sus registros.");
    }
    public void Auditar(int usuarioId, string accion, string entidad, string referencia, string detalle)
    {
        var actor = ActorId(usuarioId);
        db.Set<AuditoriaContable>().Add(new() { UsuarioId = usuarioId, ActorId = actor, Accion = accion, Entidad = entidad, Referencia = referencia, Detalle = detalle });
    }
}
public sealed class MutacionPeriodo(IDbContextTransaction? transaccion) : IAsyncDisposable
{
    public Task ConfirmarAsync(CancellationToken ct = default) => transaccion?.CommitAsync(ct) ?? Task.CompletedTask;
    public ValueTask DisposeAsync() => transaccion?.DisposeAsync() ?? ValueTask.CompletedTask;
}
