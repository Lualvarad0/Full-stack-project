using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Microsoft.EntityFrameworkCore;
namespace Back.Contabilidad;

public class LibroContableService(AppDbContext db, ControlPeriodoService control) : ServicioContable(db, control)
{
    public async Task<List<CuentaContableDto>> CuentasAsync(int uid, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct);
        return await Db.Set<CuentaContable>().Where(c => c.UsuarioId == uid).OrderBy(c => c.Codigo)
            .Select(c => new CuentaContableDto(c.Id, c.Codigo, c.Nombre, c.Tipo, c.Activa)).ToListAsync(ct);
    }
    public async Task InicializarAsync(int uid, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct);
        await using var tx = await Control.AbrirAsync(uid, [], ct);
        var existentes = await Db.Set<CuentaContable>().Where(c => c.UsuarioId == uid).Select(c => c.Codigo).ToListAsync(ct);
        foreach (var c in PlanCuentasBase.Cuentas.Where(c => !existentes.Contains(c.Codigo)))
            Db.Add(new CuentaContable { UsuarioId = uid, Codigo = c.Codigo, Nombre = c.Nombre, Tipo = c.Tipo });
        Control.Auditar(uid, "InicializarPlanCuentas", "PlanCuentas", uid.ToString(), "Copia individual del plan de cuentas básico.");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task<int> CrearCuentaAsync(int uid, CuentaContableRequest r, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); await using var tx = await Control.AbrirAsync(uid, [], ct);
        if (await Db.Set<CuentaContable>().AnyAsync(c => c.UsuarioId == uid && c.Codigo == r.Codigo, ct)) throw BusinessException.Conflict("El código ya existe en tu plan.");
        var c = new CuentaContable { UsuarioId = uid, Codigo = r.Codigo, Nombre = r.Nombre.Trim(), Tipo = r.Tipo };
        Db.Add(c); Control.Auditar(uid, "CrearCuentaContable", "Cuenta", r.Codigo, r.Nombre.Trim());
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct); return c.Id;
    }
    public async Task<List<AsientoDto>> AsientosAsync(int uid, int anio, int mes, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); var desde = PeriodosService.Fecha(anio, mes);
        var a = await Db.Set<AsientoContable>().AsNoTracking().Include(a => a.Lineas).ThenInclude(l => l.Cuenta)
            .Where(a => a.UsuarioId == uid && a.Fecha >= desde && a.Fecha < desde.AddMonths(1)).OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id).ToListAsync(ct);
        return a.Select(Dto).ToList();
    }
    public async Task<int> GuardarAsync(int uid, int? id, AsientoRequest r, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); FechaRegistrable(r.Fecha);
        await using var tx = await Control.AbrirAsync(uid, [r.Fecha], ct);
        AsientoContable a;
        if (id.HasValue) { a = await PropioAsync(uid, id.Value, ct); await Control.ExigirAbiertosAsync(uid, [a.Fecha], ct); ExigirBorrador(a); Db.RemoveRange(a.Lineas); a.Lineas = []; }
        else { a = new() { UsuarioId = uid }; Db.Add(a); }
        await ValidarLineasAsync(uid, r.Lineas, ct);
        a.Fecha = r.Fecha; a.Concepto = r.Concepto.Trim();
        a.Lineas = r.Lineas.Select(l => new LineaAsiento { CuentaContableId = l.CuentaId, Debe = l.Debe, Haber = l.Haber }).ToList();
        Control.Auditar(uid, "GuardarAsientoBorrador", "Asiento", id?.ToString() ?? "Nuevo", $"Fecha={r.Fecha}; debe={r.Lineas.Sum(l => l.Debe)}; haber={r.Lineas.Sum(l => l.Haber)}");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct); return a.Id;
    }
    public async Task ContabilizarAsync(int uid, int id, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); await using var tx = await Control.AbrirAsync(uid, [], ct);
        var a = await PropioAsync(uid, id, ct); ExigirBorrador(a); await Control.ExigirAbiertosAsync(uid, [a.Fecha], ct);
        await ValidarLineasAsync(uid, a.Lineas.Select(l => new LineaAsientoRequest(l.CuentaContableId, l.Debe, l.Haber)).ToList(), ct);
        if (a.Lineas.Sum(l => l.Debe) != a.Lineas.Sum(l => l.Haber)) throw BusinessException.Conflict("El debe y el haber deben coincidir antes de contabilizar.");
        if (a.PagoComprobanteId.HasValue && await Db.Set<PagoComprobante>().AnyAsync(p => p.Id == a.PagoComprobanteId && p.AnuladoEn != null, ct)) throw BusinessException.Conflict("El pago de origen fue anulado.");
        a.Estado = EstadoAsiento.Contabilizado; a.ContabilizadoEn = DateTime.UtcNow;
        Control.Auditar(uid, "ContabilizarAsiento", "Asiento", id.ToString(), $"Debe y haber: {a.Lineas.Sum(l => l.Debe)}");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task AnularAsync(int uid, int id, string motivo, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); await using var tx = await Control.AbrirAsync(uid, [], ct);
        var a = await PropioAsync(uid, id, ct); await Control.ExigirAbiertosAsync(uid, [a.Fecha], ct);
        if (a.Estado == EstadoAsiento.Anulado) throw BusinessException.Conflict("El asiento ya está anulado.");
        a.Estado = EstadoAsiento.Anulado; a.MotivoAnulacion = motivo.Trim();
        Control.Auditar(uid, "AnularAsiento", "Asiento", id.ToString(), motivo.Trim()); await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task<int> DesdeComprobanteAsync(int uid, int id, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); await using var tx = await Control.AbrirAsync(uid, [], ct);
        var c = await Db.Comprobantes.Include(c => c.Detalles).SingleOrDefaultAsync(c => c.UsuarioId == uid && c.Id == id, ct) ?? throw BusinessException.NotFound("Comprobante no encontrado.");
        await Control.ExigirAbiertosAsync(uid, [c.FechaEmision], ct);
        if (c.RevisadoEn == null || c.Finalidad is FinalidadComprobante.SinDefinir or FinalidadComprobante.NoAplicable) throw new BusinessException("Revisa la finalidad del comprobante antes de preparar su asiento.");
        if (await Db.Set<AsientoContable>().AnyAsync(a => a.UsuarioId == uid && a.ComprobanteId == id && a.Estado != EstadoAsiento.Anulado, ct)) throw BusinessException.Conflict("Ya existe un asiento activo para el comprobante.");
        var cuentas = await Db.Set<CuentaContable>().Where(c => c.UsuarioId == uid && c.Activa).ToDictionaryAsync(c => c.Codigo, ct);
        var lineas = new Dictionary<string, decimal>();
        void Importe(string codigo, decimal debe) { if (debe != 0) lineas[codigo] = lineas.GetValueOrDefault(codigo) + debe; }
        if (c.Direccion == Direccion.Emitido) {
            Importe("1.1.02", c.Total); Importe(c.CategoriaIngreso == CategoriaIngreso.Servicios ? "4.1.02" : "4.1.01", -c.Subtotal);
            Importe("2.1.02", -c.Iva); Importe("4.1.01", -(c.Total - c.Subtotal - c.Iva));
        } else {
            Importe("2.1.01", -c.Total);
            var personal = c.Finalidad == FinalidadComprobante.Personal ? c.Subtotal : c.Finalidad == FinalidadComprobante.Mixto ? c.Detalles.Where(d => d.Finalidad == FinalidadComprobante.Personal).Sum(d => d.Subtotal) : 0;
            if (c.Finalidad == FinalidadComprobante.Mixto && c.Detalles.Any(d => d.RevisadoEn == null || d.Finalidad is not (FinalidadComprobante.Personal or FinalidadComprobante.Negocio))) throw new BusinessException("Revisa todas las partidas personales o del negocio antes de generar el borrador.");
            var credito = c.Finalidad == FinalidadComprobante.Negocio && c.TratamientoIva == TratamientoIva.ConCredito ? c.Iva
                : c.Finalidad == FinalidadComprobante.Mixto ? c.Detalles.Where(d => d.Finalidad == FinalidadComprobante.Negocio && d.TratamientoIva == TratamientoIva.ConCredito).Sum(d => d.Iva ?? 0) : 0;
            Importe("3.1.02", personal); Importe("5.1.01", c.Subtotal - personal); Importe("1.1.03", credito);
            Importe(c.Finalidad == FinalidadComprobante.Personal ? "3.1.02" : "5.1.02", c.Total - c.Subtotal - credito);
        }
        var a = new AsientoContable { UsuarioId = uid, Fecha = c.FechaEmision, Concepto = $"Borrador: {TiposComprobante.Nombre(c.TipoComprobante)} {c.NumeroComprobante}", ComprobanteId = id };
        foreach (var (codigo, importe) in lineas) {
            if (!cuentas.TryGetValue(codigo, out var cuenta)) throw new BusinessException($"Inicializa o completa el plan de cuentas: falta {codigo}.");
            var valor = importe * c.Signo; a.Lineas.Add(new() { CuentaContableId = cuenta.Id, Debe = Math.Max(0, valor), Haber = Math.Max(0, -valor) });
        }
        Db.Add(a); Control.Auditar(uid, "PrepararAsientoComprobante", "Comprobante", id.ToString(), "Borrador editable; requiere revisión y contabilización.");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct); return a.Id;
    }
    public async Task<int> DesdePagoAsync(int uid, int id, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); await using var tx = await Control.AbrirAsync(uid, [], ct);
        var p = await Db.Set<PagoComprobante>().Include(p => p.Comprobante).SingleOrDefaultAsync(p => p.UsuarioId == uid && p.Id == id && p.AnuladoEn == null, ct) ?? throw BusinessException.NotFound("Pago no encontrado.");
        await Control.ExigirAbiertosAsync(uid, [p.Fecha], ct);
        if (await Db.Set<AsientoContable>().AnyAsync(a => a.UsuarioId == uid && a.PagoComprobanteId == id && a.Estado != EstadoAsiento.Anulado, ct)) throw BusinessException.Conflict("El pago ya tiene un asiento activo.");
        var cobro = p.Comprobante.Direccion == Direccion.Emitido; var signo = p.Tipo == TipoAplicacionPago.Reembolso ? -1 : 1;
        var cuentas = await Db.Set<CuentaContable>().Where(c => c.UsuarioId == uid && c.Activa).ToDictionaryAsync(c => c.Codigo, ct);
        var codigoContrapartida = cobro ? "1.1.02" : "2.1.01";
        if (!cuentas.ContainsKey("1.1.01") || !cuentas.ContainsKey(codigoContrapartida)) throw new BusinessException("Inicializa el plan de cuentas.");
        var bancoDebe = (cobro ? p.Importe : -p.Importe) * signo;
        var a = new AsientoContable { UsuarioId = uid, Fecha = p.Fecha, Concepto = $"{p.Tipo}: {p.Comprobante.NumeroComprobante}", PagoComprobanteId = id,
            Lineas = [new() { CuentaContableId = cuentas["1.1.01"].Id, Debe = Math.Max(0, bancoDebe), Haber = Math.Max(0, -bancoDebe) }, new() { CuentaContableId = cuentas[codigoContrapartida].Id, Debe = Math.Max(0, -bancoDebe), Haber = Math.Max(0, bancoDebe) }] };
        Db.Add(a); Control.Auditar(uid, "PrepararAsientoPago", "Pago", id.ToString(), "Borrador de tesorería pendiente de revisión.");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct); return a.Id;
    }
    public async Task<EstadosContablesDto> EstadosAsync(int uid, DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); Rango(desde, hasta);
        var cuentas = await CuentasAsync(uid, ct);
        var lineas = await (from l in Db.Set<LineaAsiento>() join a in Db.Set<AsientoContable>() on l.AsientoContableId equals a.Id
            where a.UsuarioId == uid && a.Estado == EstadoAsiento.Contabilizado && a.Fecha <= hasta select new { l.CuentaContableId, a.Fecha, l.Debe, l.Haber }).ToListAsync(ct);
        var saldos = cuentas.Select(c => {
            var grupo = lineas.Where(l => l.CuentaContableId == c.Id);
            var debe = grupo.Sum(l => l.Debe); var haber = grupo.Sum(l => l.Haber);
            return new SaldoCuentaDto(c.Id, c.Codigo, c.Nombre, c.Tipo, debe, haber, debe - haber);
        }).ToList();
        decimal Total(TipoCuentaContable tipo) => saldos.Where(c => c.Tipo == tipo).Sum(c => c.Saldo) * (tipo is TipoCuentaContable.Pasivo or TipoCuentaContable.Patrimonio or TipoCuentaContable.Ingreso ? -1 : 1);
        var activos = Total(TipoCuentaContable.Activo); var pasivos = Total(TipoCuentaContable.Pasivo); var patrimonio = Total(TipoCuentaContable.Patrimonio);
        var tipos = cuentas.ToDictionary(c => c.Id, c => c.Tipo);
        var ingresos = lineas.Where(l => l.Fecha >= desde && tipos[l.CuentaContableId] == TipoCuentaContable.Ingreso).Sum(l => l.Haber - l.Debe);
        var gastos = lineas.Where(l => l.Fecha >= desde && tipos[l.CuentaContableId] == TipoCuentaContable.Gasto).Sum(l => l.Debe - l.Haber);
        var resultadoAcumulado = lineas.Where(l => cuentas.Any(c => c.Id == l.CuentaContableId && c.Tipo is TipoCuentaContable.Ingreso or TipoCuentaContable.Gasto)).Sum(l => l.Haber - l.Debe);
        return new(activos, pasivos, patrimonio, ingresos, gastos, ingresos - gastos, activos - pasivos - patrimonio - resultadoAcumulado, saldos);
    }
    public async Task<List<MayorDto>> MayorAsync(int uid, int cuentaId, DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); Rango(desde, hasta);
        if (!await Db.Set<CuentaContable>().AnyAsync(c => c.Id == cuentaId && c.UsuarioId == uid, ct)) throw BusinessException.NotFound("Cuenta no encontrada.");
        var filas = await (from l in Db.Set<LineaAsiento>() join a in Db.Set<AsientoContable>() on l.AsientoContableId equals a.Id
            where a.UsuarioId == uid && a.Estado == EstadoAsiento.Contabilizado && l.CuentaContableId == cuentaId && a.Fecha <= hasta
            orderby a.Fecha, a.Id, l.Id select new { a.Id, a.Fecha, a.Concepto, l.Debe, l.Haber }).ToListAsync(ct);
        decimal saldo = filas.Where(l => l.Fecha < desde).Sum(l => l.Debe - l.Haber);
        var resultado = new List<MayorDto> { new(0, desde, "Saldo anterior", 0, 0, saldo) };
        foreach (var f in filas.Where(l => l.Fecha >= desde)) { saldo += f.Debe - f.Haber; resultado.Add(new(f.Id, f.Fecha, f.Concepto, f.Debe, f.Haber, saldo)); }
        return resultado;
    }
    private async Task ValidarLineasAsync(int uid, List<LineaAsientoRequest> lineas, CancellationToken ct)
    {
        if (lineas.Count is < 2 or > 200) throw new BusinessException("El asiento requiere entre dos y doscientas líneas.");
        foreach (var l in lineas) { ImporteExacto(l.Debe); ImporteExacto(l.Haber); if (l.Debe < 0 || l.Haber < 0 || (l.Debe > 0) == (l.Haber > 0)) throw new BusinessException("Cada línea debe tener un importe positivo únicamente en debe o haber."); }
        var ids = lineas.Select(l => l.CuentaId).Distinct().ToArray();
        if (await Db.Set<CuentaContable>().CountAsync(c => c.UsuarioId == uid && c.Activa && ids.Contains(c.Id), ct) != ids.Length) throw BusinessException.NotFound("Una cuenta no pertenece a tu plan o está inactiva.");
    }
    private async Task<AsientoContable> PropioAsync(int uid, int id, CancellationToken ct) => await Db.Set<AsientoContable>().Include(a => a.Lineas).SingleOrDefaultAsync(a => a.UsuarioId == uid && a.Id == id, ct) ?? throw BusinessException.NotFound("Asiento no encontrado.");
    private static void ExigirBorrador(AsientoContable a) { if (a.Estado != EstadoAsiento.Borrador) throw BusinessException.Conflict("Solo puedes editar o contabilizar un borrador. Anula el asiento con una justificación para reemplazarlo."); }
    private static void Rango(DateOnly desde, DateOnly hasta) { if (desde.Year < 2000 || hasta.Year > 2100 || desde > hasta) throw new BusinessException("Indica un intervalo válido."); }
    private static AsientoDto Dto(AsientoContable a) => new(a.Id, a.Fecha, a.Concepto, a.Estado, a.ComprobanteId, a.PagoComprobanteId, a.Lineas.Select(l => new LineaAsientoDto(l.CuentaContableId, l.Cuenta.Codigo, l.Cuenta.Nombre, l.Debe, l.Haber)).ToList());
}
