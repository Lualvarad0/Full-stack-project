using Back.Contabilidad;
using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Back.Pagos;
using Microsoft.EntityFrameworkCore;

namespace Back.Clasificacion;

public class ClasificacionPartidasService(AppDbContext db, ControlPeriodoService periodos)
{
    public async Task ClasificarAsync(int usuarioId, int id, ClasificarPartidasRequest request, CancellationToken ct)
    {
        var c = await db.Comprobantes.Include(c => c.Detalles).SingleOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Id == id, ct) ?? throw BusinessException.NotFound("Comprobante no encontrado.");
        if (c.Direccion == Direccion.Emitido) AccesoPlan.ExigirProfesional(await db.Usuarios.SingleAsync(u => u.Id == usuarioId, ct), "La clasificación de ingresos");
        await using var tx = await periodos.AbrirAsync(usuarioId, [c.FechaEmision], ct);
        if (request.Partidas.Select(p => p.Id).Distinct().Count() != request.Partidas.Count) throw new BusinessException("No repitas una partida en la solicitud.");
        foreach (var p in request.Partidas)
        {
            var d = c.Detalles.SingleOrDefault(d => d.Id == p.Id) ?? throw BusinessException.NotFound("Partida no encontrada en este comprobante.");
            var anterior = $"Finalidad={d.Finalidad}; categoría={d.Categoria}; ingreso={d.CategoriaIngreso}; IVA={d.TratamientoIva}";
            if (p.Finalidad is FinalidadComprobante.Mixto or FinalidadComprobante.SinDefinir) throw new BusinessException("Cada partida debe ser personal, del negocio o no aplicable.");
            if (c.Direccion == Direccion.Emitido && (p.Finalidad != FinalidadComprobante.Negocio || !p.CategoriaIngreso.HasValue || p.Categoria.HasValue))
                throw new BusinessException("Los ingresos requieren categoría comercial y finalidad de negocio.");
            if (c.Direccion == Direccion.Recibido && p.CategoriaIngreso.HasValue) throw new BusinessException("Un gasto no lleva categoría comercial de ingreso.");
            if (p.Finalidad == FinalidadComprobante.Personal && !ImportesGastosPersonales.EsRubro(p.Categoria)) throw new BusinessException("Elige un rubro personal para esta partida.");
            if (p.Finalidad != FinalidadComprobante.Personal && p.Categoria.HasValue && p.Categoria != CategoriaGasto.NoDeducible)
                throw new BusinessException("Los rubros personales solo corresponden a partidas personales.");
            if (p.TratamientoIva == TratamientoIva.ConCredito && (p.Finalidad != FinalidadComprobante.Negocio || !d.Iva.HasValue))
                throw new BusinessException("El crédito de IVA requiere finalidad de negocio e IVA desglosado en el XML. Revisa el documento.");
            d.Finalidad = p.Finalidad; d.Categoria = p.Categoria; d.CategoriaIngreso = p.CategoriaIngreso;
            d.TratamientoIva = p.Finalidad == FinalidadComprobante.Negocio ? p.TratamientoIva : TratamientoIva.SinCredito;
            d.RevisadoEn = DateTime.UtcNow;
            periodos.Auditar(usuarioId, "ClasificarPartida", "ComprobanteDetalle", d.Id.ToString(), $"Antes: {anterior}. Después: Finalidad={d.Finalidad}; categoría={d.Categoria}; ingreso={d.CategoriaIngreso}; IVA={d.TratamientoIva}. Comprobante={id}");
        }
        c.Finalidad = FinalidadComprobante.Mixto; c.Categoria = CategoriaGasto.SinCategoria;
        c.RevisadoEn = c.Detalles.All(d => d.RevisadoEn.HasValue) ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task FinalidadAsync(int usuarioId, int id, FinalidadComprobanteRequest request, CancellationToken ct)
    {
        var c = await db.Comprobantes.SingleOrDefaultAsync(c => c.UsuarioId == usuarioId && c.Id == id, ct) ?? throw BusinessException.NotFound("Comprobante no encontrado.");
        await using var tx = await periodos.AbrirAsync(usuarioId, [c.FechaEmision], ct);
        if (request.Finalidad is FinalidadComprobante.SinDefinir or FinalidadComprobante.Mixto) throw new BusinessException("Clasifica las partidas para una factura mixta.");
        if (c.Direccion == Direccion.Emitido && request.Finalidad != FinalidadComprobante.Negocio) throw new BusinessException("Una factura emitida corresponde a una operación de ingreso.");
        if (request.Finalidad == FinalidadComprobante.Personal && !ImportesGastosPersonales.EsRubro(c.Categoria)) throw new BusinessException("Primero elige el rubro de gasto personal.");
        if (request.TratamientoIva == TratamientoIva.ConCredito && request.Finalidad != FinalidadComprobante.Negocio) throw new BusinessException("El gasto personal no genera crédito de IVA del negocio.");
        c.Finalidad = request.Finalidad; c.TratamientoIva = request.Finalidad == FinalidadComprobante.Negocio ? request.TratamientoIva : TratamientoIva.SinCredito;
        if (c.Finalidad != FinalidadComprobante.Personal) c.Categoria = CategoriaGasto.NoDeducible;
        c.RevisadoEn = DateTime.UtcNow;
        periodos.Auditar(usuarioId, "Finalidad", "Comprobante", id.ToString(), $"Finalidad: {c.Finalidad}; IVA: {c.TratamientoIva}");
        await db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
}
