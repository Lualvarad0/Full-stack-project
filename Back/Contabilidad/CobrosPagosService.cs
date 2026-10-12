using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Microsoft.EntityFrameworkCore;
namespace Back.Contabilidad;

public class CobrosPagosService(AppDbContext db, ControlPeriodoService control) : ServicioContable(db, control)
{
    public async Task<List<SaldoComprobanteDto>> SaldosAsync(int usuarioId, CancellationToken ct)
    {
        await ExigirAccesoAsync(usuarioId, ct);
        var docs = await Db.Comprobantes.AsNoTracking().Where(c => c.UsuarioId == usuarioId).ToListAsync(ct);
        var pagos = await Db.Set<PagoComprobante>().AsNoTracking().Where(p => p.UsuarioId == usuarioId && p.AnuladoEn == null).ToListAsync(ct);
        var porSustento = docs.Where(n => n.TipoComprobante is "04" or "05").ToLookup(n => (n.DocumentoModificado, n.Direccion, n.RucEmisor, n.IdentificacionComprador));
        var porFactura = pagos.ToLookup(p => p.ComprobanteId);
        var retenciones = await Db.Set<RetencionSri>().AsNoTracking().Include(r => r.Detalles).Where(r => r.UsuarioId == usuarioId && r.RevisadoEn != null).ToListAsync(ct);
        var retPorFactura = retenciones.SelectMany(r => r.Detalles.Where(d => d.ComprobanteId.HasValue && d.CodigoImpuesto is "1" or "2").Select(d => new { Id = d.ComprobanteId!.Value, r.Direccion, d.Valor })).ToLookup(d => d.Id);
        return docs.Where(c => c.TipoComprobante == "01").Select(c => {
            var notas = porSustento[(c.NumeroComprobante, c.Direccion, c.RucEmisor, c.IdentificacionComprador)].Sum(n => n.Total * n.Signo);
            var aplicado = porFactura[c.Id].Sum(p => p.Importe * (p.Tipo == TipoAplicacionPago.Pago ? 1 : -1));
            var retenido = retPorFactura[c.Id].Where(r => r.Direccion != c.Direccion).Sum(r => r.Valor);
            return new SaldoComprobanteDto(c.Id, c.NumeroComprobante, c.Direccion == Direccion.Emitido ? c.RazonSocialComprador : c.RazonSocialEmisor, c.Direccion, c.FechaEmision, c.FechaVencimiento, c.Total, notas, aplicado, c.Total + notas - aplicado - retenido) { Retenciones = retenido };
        }).OrderByDescending(c => c.Fecha).ToList();
    }
    public async Task<int> RegistrarAsync(int usuarioId, RegistrarPagoRequest request, CancellationToken ct)
    {
        await ExigirAccesoAsync(usuarioId, ct); ImporteExacto(request.Importe); FechaRegistrable(request.Fecha);
        var c = await Db.Comprobantes.SingleOrDefaultAsync(c => c.Id == request.ComprobanteId && c.UsuarioId == usuarioId && c.TipoComprobante == "01", ct) ?? throw BusinessException.NotFound("Factura no encontrada.");
        if (request.Fecha < c.FechaEmision) throw new BusinessException("La fecha del pago no puede ser anterior a la factura.");
        await using var tx = await Control.AbrirAsync(usuarioId, [request.Fecha], ct);
        if (request.CuentaBancariaId.HasValue && !await Db.Set<CuentaBancaria>().AnyAsync(b => b.Id == request.CuentaBancariaId && b.UsuarioId == usuarioId && b.Activa, ct)) throw BusinessException.NotFound("Cuenta bancaria no encontrada.");
        var saldo = (await SaldosAsync(usuarioId, ct)).Single(s => s.Id == c.Id).Saldo;
        if (request.Tipo == TipoAplicacionPago.Pago && request.Importe > saldo || request.Tipo == TipoAplicacionPago.Reembolso && request.Importe > -saldo)
            throw BusinessException.Conflict("El importe supera el saldo pendiente de pago o de devolución.");
        var p = new PagoComprobante { UsuarioId = usuarioId, ComprobanteId = c.Id, Fecha = request.Fecha, Importe = request.Importe, Tipo = request.Tipo, Medio = request.Medio, Referencia = request.Referencia, CuentaBancariaId = request.CuentaBancariaId };
        Db.Add(p); Control.Auditar(usuarioId, "RegistrarPago", "Comprobante", c.Id.ToString(), $"Tipo={p.Tipo}; fecha={p.Fecha}; importe={p.Importe}; medio={p.Medio}");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct); return p.Id;
    }
    public async Task<List<PagoDto>> PagosAsync(int usuarioId, CancellationToken ct)
    {
        await ExigirAccesoAsync(usuarioId, ct);
        return await Db.Set<PagoComprobante>().AsNoTracking().Where(p => p.UsuarioId == usuarioId).OrderByDescending(p => p.Fecha)
            .Select(p => new PagoDto(p.Id, p.ComprobanteId, p.Comprobante.NumeroComprobante, p.Fecha, p.Importe, p.Tipo, p.Medio, p.Referencia, p.AnuladoEn.HasValue,
                p.Importe - Db.Set<ConciliacionBancaria>().Where(x => x.PagoComprobanteId == p.Id).Sum(x => (decimal?)x.Importe).GetValueOrDefault(),
                p.Comprobante.Direccion == Direccion.Emitido && p.Tipo == TipoAplicacionPago.Pago || p.Comprobante.Direccion == Direccion.Recibido && p.Tipo == TipoAplicacionPago.Reembolso)).ToListAsync(ct);
    }
    public async Task AnularAsync(int usuarioId, int id, string motivo, CancellationToken ct)
    {
        await ExigirAccesoAsync(usuarioId, ct);
        var p = await Db.Set<PagoComprobante>().SingleOrDefaultAsync(p => p.UsuarioId == usuarioId && p.Id == id, ct) ?? throw BusinessException.NotFound("Pago no encontrado.");
        await using var tx = await Control.AbrirAsync(usuarioId, [p.Fecha], ct);
        if (p.AnuladoEn.HasValue) throw BusinessException.Conflict("El pago ya está anulado.");
        if (await Db.Set<ConciliacionBancaria>().AnyAsync(x => x.PagoComprobanteId == id, ct)) throw BusinessException.Conflict("Deshaz primero la conciliación bancaria de este pago.");
        if (await Db.Set<AsientoContable>().AnyAsync(a => a.UsuarioId == usuarioId && a.PagoComprobanteId == id && a.Estado != EstadoAsiento.Anulado, ct)) throw BusinessException.Conflict("Anula primero el asiento asociado al pago.");
        p.AnuladoEn = DateTime.UtcNow; p.MotivoAnulacion = motivo.Trim();
        Control.Auditar(usuarioId, "AnularPago", "Pago", id.ToString(), motivo.Trim()); await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task VencimientoAsync(int usuarioId, int id, DateOnly fecha, CancellationToken ct)
    {
        await ExigirAccesoAsync(usuarioId, ct);
        var c = await Db.Comprobantes.SingleOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Id == id && c.TipoComprobante == "01", ct) ?? throw BusinessException.NotFound("Factura no encontrada.");
        if (fecha < c.FechaEmision || fecha.Year > 2100) throw new BusinessException("Revisa la fecha de vencimiento.");
        await using var tx = await Control.AbrirAsync(usuarioId, [c.FechaEmision], ct); c.FechaVencimiento = fecha;
        Control.Auditar(usuarioId, "Vencimiento", "Comprobante", id.ToString(), fecha.ToString()); await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
}
