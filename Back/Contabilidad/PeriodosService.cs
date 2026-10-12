using Back.Data;
using Back.Helpers;
using Back.Models;
using Back.Pagos;
using Microsoft.EntityFrameworkCore;

namespace Back.Contabilidad;

public record RevisionPeriodoDto(int Anio, int Mes, EstadoPeriodo Estado, int Comprobantes, int Pendientes, int FacturasSriPendientes, DateTime? CerradoEn);
public class PeriodosService(AppDbContext db, ControlPeriodoService control)
{
    public async Task<RevisionPeriodoDto> RevisarAsync(int usuarioId, int anio, int mes, CancellationToken ct)
    {
        var desde = Fecha(anio, mes); var hasta = desde.AddMonths(1);
        var c = await db.Comprobantes.Include(x => x.Detalles).Where(x => x.UsuarioId == usuarioId && x.FechaEmision >= desde && x.FechaEmision < hasta).ToListAsync(ct);
        var p = await db.Set<PeriodoContable>().AsNoTracking().SingleOrDefaultAsync(p => p.UsuarioId == usuarioId && p.Anio == anio && p.Mes == mes, ct);
        var retenciones = await db.Set<RetencionSri>().Where(r => r.UsuarioId == usuarioId && r.PeriodoFiscal >= desde && r.PeriodoFiscal < hasta).ToListAsync(ct);
        var pendientes = c.Count(x => x.RevisadoEn == null || (x.Finalidad == FinalidadComprobante.Mixto && x.Detalles.Any(d => d.RevisadoEn == null))
            || (x.Direccion == Direccion.Recibido && (x.Finalidad == FinalidadComprobante.Negocio && x.TratamientoIva == TratamientoIva.Pendiente
                || x.Finalidad == FinalidadComprobante.Mixto && x.Detalles.Any(d => d.Finalidad == FinalidadComprobante.Negocio && d.TratamientoIva == TratamientoIva.Pendiente))));
        pendientes += retenciones.Count(r => r.RevisadoEn == null);
        pendientes += await db.Set<AsientoContable>().CountAsync(a => a.UsuarioId == usuarioId && a.Fecha >= desde && a.Fecha < hasta && a.Estado == EstadoAsiento.Borrador, ct);
        if (await db.DatosEmisores.AnyAsync(e => e.UsuarioId == usuarioId && e.ObligadoContabilidad, ct))
        {
            var contabilizados = (await db.Set<AsientoContable>().Where(a => a.UsuarioId == usuarioId && a.ComprobanteId.HasValue && a.Estado == EstadoAsiento.Contabilizado).Select(a => a.ComprobanteId!.Value).ToListAsync(ct)).ToHashSet();
            pendientes += c.Count(x => x.Finalidad is FinalidadComprobante.Negocio or FinalidadComprobante.Mixto && !contabilizados.Contains(x.Id));
        }
        pendientes += await db.Set<MovimientoBancario>().CountAsync(m => m.UsuarioId == usuarioId && m.Fecha >= desde && m.Fecha < hasta && m.RevisadoSinFacturaEn == null
            && (db.Set<ConciliacionBancaria>().Where(c => c.MovimientoBancarioId == m.Id).Sum(c => (decimal?)c.Importe).GetValueOrDefault() != Math.Abs(m.Importe)), ct);
        var sri = await db.FacturasEmitidas.CountAsync(f => f.UsuarioId == usuarioId && f.FechaEmision >= desde && f.FechaEmision < hasta
            && (f.Estado == EstadoFactura.Firmada || f.Estado == EstadoFactura.EnProceso || f.Estado == EstadoFactura.Recibida
                || (f.Estado == EstadoFactura.Autorizada && f.Ambiente == AmbienteSri.Produccion
                    && (f.TipoDocumento == "07" ? !db.Set<RetencionSri>().Any(x => x.UsuarioId == usuarioId && x.ClaveAcceso == f.ClaveAcceso) : !db.Comprobantes.Any(x => x.UsuarioId == usuarioId && x.ClaveAcceso == f.ClaveAcceso)))), ct);
        return new(anio, mes, p?.Estado ?? EstadoPeriodo.Abierto, c.Count + retenciones.Count, pendientes, sri, p?.CerradoEn);
    }
    public async Task CerrarAsync(int usuarioId, int anio, int mes, CancellationToken ct)
    {
        AccesoPlan.ExigirProfesional(await db.Usuarios.SingleAsync(u => u.Id == usuarioId, ct), "El cierre contable");
        var fecha = Fecha(anio, mes); await using var tx = await control.AbrirAsync(usuarioId, [fecha], ct);
        var r = await RevisarAsync(usuarioId, anio, mes, ct);
        if (r.Pendientes > 0 || r.FacturasSriPendientes > 0) throw BusinessException.Conflict("Revisa todos los comprobantes y termina las autorizaciones del SRI antes de cerrar el período.");
        var p = await db.Set<PeriodoContable>().SingleOrDefaultAsync(p => p.UsuarioId == usuarioId && p.Anio == anio && p.Mes == mes, ct);
        if (p is null) { p = new() { UsuarioId = usuarioId, Anio = anio, Mes = mes }; db.Add(p); }
        p.Estado = EstadoPeriodo.Cerrado; p.CerradoEn = DateTime.UtcNow; p.CerradoPorId = control.ActorId(usuarioId);
        control.Auditar(usuarioId, "CerrarPeriodo", "Periodo", $"{anio}-{mes:D2}", $"Comprobantes revisados: {r.Comprobantes}");
        await db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task ReabrirAsync(int usuarioId, int anio, int mes, string motivo, CancellationToken ct)
    {
        Fecha(anio, mes); AccesoPlan.ExigirProfesional(await db.Usuarios.SingleAsync(u => u.Id == usuarioId, ct), "El cierre contable");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Usuarios.FromSqlInterpolated($"SELECT * FROM usuarios WHERE id = {usuarioId} FOR UPDATE").ToListAsync(ct);
        var p = await db.Set<PeriodoContable>().SingleOrDefaultAsync(p => p.UsuarioId == usuarioId && p.Anio == anio && p.Mes == mes, ct)
            ?? throw BusinessException.NotFound("Período no encontrado.");
        if (p.Estado != EstadoPeriodo.Cerrado) throw BusinessException.Conflict("El período ya está abierto.");
        p.Estado = EstadoPeriodo.Abierto;
        control.Auditar(usuarioId, "ReabrirPeriodo", "Periodo", $"{anio}-{mes:D2}", motivo.Trim());
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public static DateOnly Fecha(int anio, int mes) => anio is >= 2000 and <= 2100 && mes is >= 1 and <= 12
        ? new(anio, mes, 1) : throw new BusinessException("Indica un año y mes válidos.");
}
