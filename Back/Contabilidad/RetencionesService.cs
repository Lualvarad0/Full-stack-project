using Back.Data;
using Back.Helpers;
using Back.Models;
using Back.Pagos;
using Back.Sri;
using Back.Facturacion;
using Microsoft.EntityFrameworkCore;
namespace Back.Contabilidad;

public record RetencionResumenDto(int Id, string Numero, DateOnly FechaEmision, DateOnly PeriodoFiscal, Direccion Direccion, string Emisor, string Retenido, decimal Renta, decimal Iva, bool Revisada);
public record ResultadoRetencionesDto(int Importadas, int Existentes, List<string> Problemas);
public class RetencionesService(AppDbContext db, SriAutorizacionClient sri, ControlPeriodoService control)
{
    public async Task<ResultadoRetencionesDto> ImportarAsync(int usuarioId, string texto, CancellationToken ct)
    {
        var u = await db.Usuarios.SingleAsync(u => u.Id == usuarioId, ct); AccesoPlan.ExigirProfesional(u, "Las retenciones");
        if (u.Ruc is null) throw new BusinessException("Configura el RUC del contribuyente antes de importar retenciones.");
        var claves = ClaveAcceso.Extraer(texto).Where(c => ClaveAcceso.TipoComprobante(c) == "07").ToList();
        if (claves.Count == 0 || claves.Count > 100) throw new BusinessException("Incluye entre 1 y 100 claves de retención válidas.");
        var importadas = 0; var existentes = 0; var problemas = new List<string>();
        foreach (var clave in claves)
        {
            if (await db.Set<RetencionSri>().AnyAsync(r => r.UsuarioId == usuarioId && r.ClaveAcceso == clave, ct)) { existentes++; continue; }
            var respuesta = await sri.ConsultarAsync(clave, ct);
            if (respuesta.Estado != EstadoAutorizacion.Autorizado || string.IsNullOrEmpty(respuesta.ComprobanteXml)) { problemas.Add($"{clave}: sin autorización disponible."); continue; }
            RetencionSri r;
            try { r = RetencionXmlParser.Parse(respuesta.ComprobanteXml); }
            catch (Exception ex) when (ex is FormatException or System.Xml.XmlException) { problemas.Add($"{clave}: XML de retención no válido."); continue; }
            if (r.ClaveAcceso != clave || (r.RucEmisor != u.Ruc && !u.Identificaciones().Contains(r.IdentificacionRetenido))) { problemas.Add($"{clave}: no corresponde al contribuyente seleccionado."); continue; }
            // Los comprobantes de pruebas nunca participan de preparación tributaria.
            if (ClaveAcceso.Ambiente(clave) != "2") { problemas.Add($"{clave}: ambiente de pruebas, sin valor tributario."); continue; }
            await using var tx = await control.AbrirAsync(usuarioId, [r.FechaEmision, r.PeriodoFiscal], ct);
            if (await db.Set<RetencionSri>().AnyAsync(r => r.UsuarioId == usuarioId && r.ClaveAcceso == clave, ct)) { existentes++; continue; }
            r.UsuarioId = usuarioId; r.Direccion = r.RucEmisor == u.Ruc ? Direccion.Emitido : Direccion.Recibido;
            r.NumeroAutorizacion = respuesta.NumeroAutorizacion ?? clave; r.FechaAutorizacion = respuesta.FechaAutorizacion;
            var candidatas = await db.Comprobantes.Where(c => c.UsuarioId == usuarioId && c.TipoComprobante == "01"
                && (r.Direccion == Direccion.Recibido ? c.Direccion == Direccion.Emitido && c.IdentificacionComprador == r.RucEmisor
                    : c.Direccion == Direccion.Recibido && c.RucEmisor == r.IdentificacionRetenido)).Select(c => new { c.Id, c.NumeroComprobante }).ToListAsync(ct);
            foreach (var d in r.Detalles) {
                var coinciden = candidatas.Where(c => c.NumeroComprobante.Replace("-", "") == d.DocumentoSustento?.Replace("-", "")).ToList();
                if (coinciden.Count == 1) d.ComprobanteId = coinciden[0].Id;
            }
            db.Add(r); control.Auditar(usuarioId, "ImportarRetencion", "Retencion", clave, $"Dirección: {r.Direccion}; partidas: {r.Detalles.Count}");
            await db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct); importadas++;
        }
        return new(importadas, existentes, problemas);
    }
    public async Task<List<RetencionResumenDto>> ListarAsync(int usuarioId, int anio, int? mes, CancellationToken ct)
    {
        AccesoPlan.ExigirProfesional(await db.Usuarios.SingleAsync(u => u.Id == usuarioId, ct), "Las retenciones");
        var desde = PeriodosService.Fecha(anio, mes ?? 1); var hasta = mes.HasValue ? desde.AddMonths(1) : desde.AddYears(1);
        return await db.Set<RetencionSri>().AsNoTracking().Where(r => r.UsuarioId == usuarioId && r.PeriodoFiscal >= desde && r.PeriodoFiscal < hasta)
            .OrderByDescending(r => r.FechaEmision).Select(r => new RetencionResumenDto(r.Id, r.Numero, r.FechaEmision, r.PeriodoFiscal, r.Direccion,
                r.RazonSocialEmisor, r.RazonSocialRetenido, r.Detalles.Where(d => d.CodigoImpuesto == "1").Sum(d => d.Valor), r.Detalles.Where(d => d.CodigoImpuesto == "2").Sum(d => d.Valor), r.RevisadoEn.HasValue)).ToListAsync(ct);
    }
    public Task<RetencionSri?> ObtenerAsync(int usuarioId, int id, CancellationToken ct) => db.Set<RetencionSri>().AsNoTracking().Include(r => r.Detalles).SingleOrDefaultAsync(r => r.Id == id && r.UsuarioId == usuarioId, ct);
    public async Task RevisarAsync(int usuarioId, int id, CancellationToken ct)
    {
        var r = await db.Set<RetencionSri>().SingleOrDefaultAsync(r => r.UsuarioId == usuarioId && r.Id == id, ct) ?? throw BusinessException.NotFound("Retención no encontrada.");
        await using var tx = await control.AbrirAsync(usuarioId, [r.FechaEmision, r.PeriodoFiscal], ct); r.RevisadoEn = DateTime.UtcNow;
        control.Auditar(usuarioId, "RevisarRetencion", "Retencion", id.ToString(), "El usuario confirmó período, valores y sustento de la retención.");
        await db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct);
    }
    public async Task<byte[]> PdfAsync(int usuarioId, int id, CancellationToken ct)
    {
        var r = await ObtenerAsync(usuarioId, id, ct) ?? throw BusinessException.NotFound("Retención no encontrada.");
        AccesoPlan.ExigirProfesional(await db.Usuarios.SingleAsync(u => u.Id == usuarioId, ct), "Los PDF de retenciones");
        var numero = r.Numero.Split('-');
        return FacturaPdf.Crear(new FacturaEmitida { TipoDocumento = "07", Estado = EstadoFactura.Autorizada, Ambiente = AmbienteSri.Produccion,
            Establecimiento = numero[0], PuntoEmision = numero[1], Secuencial = int.Parse(numero[2]), ClaveAcceso = r.ClaveAcceso,
            NumeroAutorizacion = r.NumeroAutorizacion, FechaAutorizacion = r.FechaAutorizacion, XmlFirmado = r.Xml });
    }
}
