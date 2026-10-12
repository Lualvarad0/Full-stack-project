using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Microsoft.EntityFrameworkCore;

namespace Back.Services;

/// <summary>
/// Registro manual de cuentas del mes (todos los planes): el usuario dice cuánto gana, anota sus cuentas
/// y marca cada una con el día en que la pagó. No toca comprobantes del SRI.
/// </summary>
public class CuentasPersonalesService(AppDbContext db)
{
    public const int MaxCuentasPorMes = 100;

    public async Task<MesCuentasDto> MesAsync(int usuarioId, int anio, int mes, CancellationToken ct)
    {
        ValidarMes(anio, mes);
        var cuentas = await db.CuentasPersonales.AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId && c.Anio == anio && c.Mes == mes)
            .OrderBy(c => c.PagadaEl != null).ThenBy(c => c.DiaVencimiento ?? 32).ThenBy(c => c.Nombre)
            .ToListAsync(ct);
        var ingreso = await IngresoDelMesAsync(usuarioId, anio, mes, ct);
        var total = cuentas.Sum(c => c.Monto);
        var pagado = cuentas.Where(c => c.PagadaEl != null).Sum(c => c.Monto);
        return new MesCuentasDto(anio, mes, ingreso, total, pagado, total - pagado, ingreso - total,
            cuentas.Select(CuentaPersonalDto.From).ToList());
    }

    public async Task<CuentaPersonalDto> CrearAsync(int usuarioId, CuentaPersonalRequest r, CancellationToken ct)
    {
        ValidarMes(r.Anio, r.Mes);
        if (await db.CuentasPersonales.CountAsync(c => c.UsuarioId == usuarioId && c.Anio == r.Anio && c.Mes == r.Mes, ct) >= MaxCuentasPorMes)
            throw new BusinessException($"Puedes registrar hasta {MaxCuentasPorMes} cuentas por mes.");
        var cuenta = new CuentaPersonal { UsuarioId = usuarioId, Anio = r.Anio, Mes = r.Mes };
        Aplicar(cuenta, r);
        db.CuentasPersonales.Add(cuenta);
        await db.SaveChangesAsync(ct);
        return CuentaPersonalDto.From(cuenta);
    }

    public async Task<CuentaPersonalDto> EditarAsync(int usuarioId, int id, CuentaPersonalRequest r, CancellationToken ct)
    {
        var cuenta = await BuscarAsync(usuarioId, id, ct);
        if (cuenta.Anio != r.Anio || cuenta.Mes != r.Mes) throw new BusinessException("La cuenta pertenece a otro mes.");
        Aplicar(cuenta, r);
        await db.SaveChangesAsync(ct);
        return CuentaPersonalDto.From(cuenta);
    }

    public async Task EliminarAsync(int usuarioId, int id, CancellationToken ct)
    {
        db.CuentasPersonales.Remove(await BuscarAsync(usuarioId, id, ct));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Marca la cuenta como pagada el día indicado (no puede ser futuro).</summary>
    public async Task<CuentaPersonalDto> PagarAsync(int usuarioId, int id, DateOnly fecha, CancellationToken ct)
    {
        var cuenta = await BuscarAsync(usuarioId, id, ct);
        if (fecha > HoyEcuador()) throw new BusinessException("La fecha de pago no puede ser posterior a hoy.");
        if (fecha < new DateOnly(cuenta.Anio, cuenta.Mes, 1).AddMonths(-12)) throw new BusinessException("Revisa la fecha de pago.");
        cuenta.PagadaEl = fecha;
        await db.SaveChangesAsync(ct);
        return CuentaPersonalDto.From(cuenta);
    }

    public async Task<CuentaPersonalDto> QuitarPagoAsync(int usuarioId, int id, CancellationToken ct)
    {
        var cuenta = await BuscarAsync(usuarioId, id, ct);
        cuenta.PagadaEl = null;
        await db.SaveChangesAsync(ct);
        return CuentaPersonalDto.From(cuenta);
    }

    public async Task GuardarIngresoAsync(int usuarioId, IngresoMensualRequest r, CancellationToken ct)
    {
        ValidarMes(r.Anio, r.Mes);
        var ingreso = await db.IngresosMensuales.SingleOrDefaultAsync(i => i.UsuarioId == usuarioId && i.Anio == r.Anio && i.Mes == r.Mes, ct);
        if (ingreso is null) db.IngresosMensuales.Add(ingreso = new IngresoMensual { UsuarioId = usuarioId, Anio = r.Anio, Mes = r.Mes });
        ingreso.Monto = Math.Round(r.Monto, 2, MidpointRounding.AwayFromZero);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Trae al mes las cuentas del mes anterior que todavía no estén (por nombre), sin marcar como pagadas.</summary>
    public async Task<int> CopiarMesAnteriorAsync(int usuarioId, int anio, int mes, CancellationToken ct)
    {
        ValidarMes(anio, mes);
        var anterior = new DateOnly(anio, mes, 1).AddMonths(-1);
        var origen = await db.CuentasPersonales.AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId && c.Anio == anterior.Year && c.Mes == anterior.Month).ToListAsync(ct);
        var actuales = await db.CuentasPersonales
            .Where(c => c.UsuarioId == usuarioId && c.Anio == anio && c.Mes == mes).Select(c => c.Nombre.ToLower()).ToListAsync(ct);
        var nuevas = origen.Where(c => !actuales.Contains(c.Nombre.ToLower()))
            .Take(MaxCuentasPorMes - actuales.Count)
            .Select(c => new CuentaPersonal
            {
                UsuarioId = usuarioId, Anio = anio, Mes = mes, Nombre = c.Nombre, Categoria = c.Categoria, Monto = c.Monto,
                DiaVencimiento = c.DiaVencimiento,
            }).ToList();
        db.CuentasPersonales.AddRange(nuevas);
        await db.SaveChangesAsync(ct);
        return nuevas.Count;
    }

    /// <summary>Ingreso del mes; si no lo registró, el último que declaró antes (el sueldo suele repetirse).</summary>
    private async Task<decimal?> IngresoDelMesAsync(int usuarioId, int anio, int mes, CancellationToken ct)
    {
        var clave = anio * 12 + mes;
        return await db.IngresosMensuales.AsNoTracking()
            .Where(i => i.UsuarioId == usuarioId && i.Anio * 12 + i.Mes <= clave)
            .OrderByDescending(i => i.Anio).ThenByDescending(i => i.Mes)
            .Select(i => (decimal?)i.Monto).FirstOrDefaultAsync(ct);
    }

    private async Task<CuentaPersonal> BuscarAsync(int usuarioId, int id, CancellationToken ct) =>
        await db.CuentasPersonales.SingleOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId, ct)
        ?? throw BusinessException.NotFound("Cuenta no encontrada.");

    private static void Aplicar(CuentaPersonal cuenta, CuentaPersonalRequest r)
    {
        cuenta.Nombre = r.Nombre.Trim();
        cuenta.Categoria = r.Categoria;
        cuenta.Monto = Math.Round(r.Monto, 2, MidpointRounding.AwayFromZero);
        cuenta.DiaVencimiento = r.DiaVencimiento;
    }

    private static void ValidarMes(int anio, int mes)
    {
        if (anio is < 2000 or > 2100 || mes is < 1 or > 12) throw new BusinessException("Mes no válido.");
    }

    private static DateOnly HoyEcuador() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
}
