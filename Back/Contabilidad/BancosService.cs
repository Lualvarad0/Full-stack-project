using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
namespace Back.Contabilidad;

public class BancosService(AppDbContext db, ControlPeriodoService control, IDataProtectionProvider proteccion) : ServicioContable(db, control)
{
    public async Task<List<CuentaBancariaDto>> CuentasAsync(int usuarioId, CancellationToken ct)
    {
        await ExigirAccesoAsync(usuarioId, ct);
        return await Db.Set<CuentaBancaria>().AsNoTracking().Where(c => c.UsuarioId == usuarioId).OrderBy(c => c.Nombre)
            .Select(c => new CuentaBancariaDto(c.Id, c.Nombre, c.Entidad, c.UltimosDigitos, c.SaldoInicial, c.FechaInicial,
                c.SaldoInicial + Db.Set<MovimientoBancario>().Where(m => m.UsuarioId == usuarioId && m.CuentaBancariaId == c.Id).Sum(m => (decimal?)m.Importe).GetValueOrDefault(), c.Activa)).ToListAsync(ct);
    }
    public async Task<int> CrearCuentaAsync(int usuarioId, CuentaBancariaRequest request, CancellationToken ct)
    {
        await ExigirAccesoAsync(usuarioId, ct); ImporteExacto(request.SaldoInicial); FechaRegistrable(request.FechaInicial);
        await using var tx = await Control.AbrirAsync(usuarioId, [request.FechaInicial], ct);
        var numero = request.Numero.Replace(" ", "").Replace("-", "");
        if (numero.Length < 4) throw new BusinessException("Revisa el número de cuenta.");
        var cuenta = new CuentaBancaria { UsuarioId = usuarioId, Nombre = request.Nombre.Trim(), Entidad = request.Entidad.Trim(),
            NumeroCifrado = proteccion.CreateProtector("Nudo.CuentaBancaria.v1").Protect(numero), UltimosDigitos = numero.Length <= 4 ? numero : numero[^4..], SaldoInicial = request.SaldoInicial, FechaInicial = request.FechaInicial };
        Db.Add(cuenta); Control.Auditar(usuarioId, "CrearCuentaBancaria", "Banco", cuenta.Nombre, "Cuenta registrada en USD; número completo cifrado.");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct); return cuenta.Id;
    }
    public async Task<int> MovimientoAsync(int usuarioId, int cuentaId, MovimientoBancarioRequest request, CancellationToken ct)
    {
        await ExigirAccesoAsync(usuarioId, ct); ImporteExacto(request.Importe); FechaRegistrable(request.Fecha);
        if (request.Importe == 0) throw new BusinessException("El movimiento debe tener un importe distinto de cero.");
        var cuenta = await Db.Set<CuentaBancaria>().SingleOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Id == cuentaId && c.Activa, ct) ?? throw BusinessException.NotFound("Cuenta no encontrada.");
        if (request.Fecha < cuenta.FechaInicial) throw new BusinessException("El movimiento es anterior al saldo inicial de la cuenta.");
        await using var tx = await Control.AbrirAsync(usuarioId, [request.Fecha], ct);
        if (await Db.Set<MovimientoBancario>().AnyAsync(m => m.CuentaBancariaId == cuentaId && m.IdExterno == request.IdExterno.Trim(), ct)) throw BusinessException.Conflict("Este movimiento ya está registrado en la cuenta.");
        var movimiento = new MovimientoBancario { UsuarioId = usuarioId, CuentaBancariaId = cuentaId, IdExterno = request.IdExterno.Trim(), Fecha = request.Fecha, Concepto = request.Concepto.Trim(), Referencia = request.Referencia, Importe = request.Importe };
        Db.Add(movimiento); Control.Auditar(usuarioId, "RegistrarMovimiento", "Banco", cuentaId.ToString(), $"Identificador={movimiento.IdExterno}; fecha={movimiento.Fecha}; importe={movimiento.Importe}");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct); return movimiento.Id;
    }
    public async Task<List<MovimientoBancarioDto>> MovimientosAsync(int usuarioId, int cuentaId, int anio, int mes, CancellationToken ct)
    {
        await ExigirAccesoAsync(usuarioId, ct); if (!await Db.Set<CuentaBancaria>().AnyAsync(c => c.UsuarioId == usuarioId && c.Id == cuentaId, ct)) throw BusinessException.NotFound("Cuenta no encontrada.");
        var desde = PeriodosService.Fecha(anio, mes); var hasta = desde.AddMonths(1);
        return await Db.Set<MovimientoBancario>().AsNoTracking().Where(m => m.UsuarioId == usuarioId && m.CuentaBancariaId == cuentaId && m.Fecha >= desde && m.Fecha < hasta)
            .OrderByDescending(m => m.Fecha).Select(m => new MovimientoBancarioDto(m.Id, m.Fecha, m.Concepto, m.Referencia, m.Importe,
                Db.Set<ConciliacionBancaria>().Where(c => c.MovimientoBancarioId == m.Id).Sum(c => (decimal?)c.Importe).GetValueOrDefault()) { RevisadoSinFactura = m.RevisadoSinFacturaEn.HasValue }).ToListAsync(ct);
    }
    public async Task ConciliarAsync(int usuarioId, int id, ConciliarRequest request, CancellationToken ct)
    {
        await ExigirAccesoAsync(usuarioId, ct);
        var movimiento = await Db.Set<MovimientoBancario>().SingleOrDefaultAsync(m => m.UsuarioId == usuarioId && m.Id == id, ct) ?? throw BusinessException.NotFound("Movimiento no encontrado.");
        await using var tx = await Control.AbrirAsync(usuarioId, [movimiento.Fecha], ct);
        movimiento.RevisadoSinFacturaEn = null; movimiento.MotivoRevision = null;
        if (request.Pagos.Select(p => p.PagoId).Distinct().Count() != request.Pagos.Count) throw new BusinessException("No repitas un pago en la conciliación.");
        if (await Db.Set<ConciliacionBancaria>().AnyAsync(c => c.MovimientoBancarioId == id, ct)) throw BusinessException.Conflict("El movimiento ya fue conciliado.");
        if (request.Pagos.Sum(p => p.Importe) != Math.Abs(movimiento.Importe)) throw new BusinessException("Los pagos aplicados deben igualar el importe del movimiento.");
        foreach (var linea in request.Pagos)
        {
            ImporteExacto(linea.Importe);
            var p = await Db.Set<PagoComprobante>().Include(p => p.Comprobante).SingleOrDefaultAsync(p => p.Id == linea.PagoId && p.UsuarioId == usuarioId && p.AnuladoEn == null, ct) ?? throw BusinessException.NotFound("Pago no encontrado.");
            await Control.ExigirAbiertosAsync(usuarioId, [p.Fecha], ct);
            var entrada = p.Comprobante.Direccion == Direccion.Emitido && p.Tipo == TipoAplicacionPago.Pago || p.Comprobante.Direccion == Direccion.Recibido && p.Tipo == TipoAplicacionPago.Reembolso;
            if (entrada != (movimiento.Importe > 0) || p.CuentaBancariaId.HasValue && p.CuentaBancariaId != movimiento.CuentaBancariaId) throw new BusinessException("El sentido o cuenta del pago no coincide con el movimiento.");
            var usado = await Db.Set<ConciliacionBancaria>().Where(c => c.PagoComprobanteId == p.Id).SumAsync(c => (decimal?)c.Importe, ct) ?? 0;
            if (usado + linea.Importe > p.Importe) throw BusinessException.Conflict("El pago ya está aplicado a otros movimientos.");
            p.CuentaBancariaId = movimiento.CuentaBancariaId;
            Db.Add(new ConciliacionBancaria { MovimientoBancarioId = id, PagoComprobanteId = p.Id, Importe = linea.Importe });
        }
        Control.Auditar(usuarioId, "ConciliarBanco", "Movimiento", id.ToString(), $"Pagos: {string.Join(',', request.Pagos.Select(p => p.PagoId))}; importe: {movimiento.Importe}");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task DesconciliarAsync(int usuarioId, int id, string motivo, CancellationToken ct)
    {
        await ExigirAccesoAsync(usuarioId, ct);
        var m = await Db.Set<MovimientoBancario>().SingleOrDefaultAsync(m => m.UsuarioId == usuarioId && m.Id == id, ct) ?? throw BusinessException.NotFound("Movimiento no encontrado.");
        await using var tx = await Control.AbrirAsync(usuarioId, [m.Fecha], ct);
        var pagos = await (from c in Db.Set<ConciliacionBancaria>() join p in Db.Set<PagoComprobante>() on c.PagoComprobanteId equals p.Id where c.MovimientoBancarioId == id select p.Fecha).ToListAsync(ct);
        await Control.ExigirAbiertosAsync(usuarioId, pagos, ct);
        if (await Db.Set<ConciliacionBancaria>().Where(c => c.MovimientoBancarioId == id).ExecuteDeleteAsync(ct) == 0) throw BusinessException.Conflict("El movimiento no está conciliado.");
        Control.Auditar(usuarioId, "DesconciliarBanco", "Movimiento", id.ToString(), motivo.Trim()); await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task RevisarOtroAsync(int usuarioId, int id, string motivo, CancellationToken ct)
    {
        await ExigirAccesoAsync(usuarioId, ct);
        var m = await Db.Set<MovimientoBancario>().SingleOrDefaultAsync(m => m.UsuarioId == usuarioId && m.Id == id, ct) ?? throw BusinessException.NotFound("Movimiento no encontrado.");
        await using var tx = await Control.AbrirAsync(usuarioId, [m.Fecha], ct);
        if (await Db.Set<ConciliacionBancaria>().AnyAsync(c => c.MovimientoBancarioId == id, ct)) throw BusinessException.Conflict("El movimiento ya está vinculado a pagos.");
        m.RevisadoSinFacturaEn = DateTime.UtcNow; m.MotivoRevision = motivo.Trim();
        Control.Auditar(usuarioId, "RevisarMovimientoSinFactura", "Movimiento", id.ToString(), motivo.Trim()); await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
}
