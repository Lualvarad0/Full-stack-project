using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Back.Data;
using Back.Helpers;
using Back.Models;
using Back.Tributacion;
using Microsoft.EntityFrameworkCore;
namespace Back.Contabilidad;

public class ExportacionesService(AppDbContext db, ControlPeriodoService control, LibroContableService libro, PreparacionTributariaService preparacion, RevisionTributariaService revision) : ServicioContable(db, control)
{
    private static readonly string[] Columnas = ["Id", "Fecha", "Tipo", "Numero", "Clave SRI", "Direccion", "RUC emisor", "Emisor", "Comprador", "Finalidad", "Rubro personal", "Ingreso comercial", "Base neta", "IVA", "Otros impuestos", "Total", "Renta", "Sustento ATS", "Forma pago SRI", "Relacionado"];
    public async Task<byte[]> ComprobantesAsync(int uid, int anio, int mes, int meses, bool excel, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); var (desde, hasta) = PreparacionTributariaService.Periodo(anio, mes, meses);
        var docs = await Documentos(uid, desde, hasta, ct); var metadata = await Db.Set<RevisionFiscalComprobante>().AsNoTracking().Where(r => r.UsuarioId == uid).ToDictionaryAsync(r => r.ComprobanteId, ct);
        var filas = docs.Select(c => { var r = metadata.GetValueOrDefault(c.Id); return new object?[] { c.Id, c.FechaEmision.ToString("yyyy-MM-dd"), c.TipoComprobante, c.NumeroComprobante, c.ClaveAcceso, c.Direccion.ToString(), c.RucEmisor, c.RazonSocialEmisor,
            c.IdentificacionComprador, c.Finalidad.ToString(), c.Categoria.ToString(), c.CategoriaIngreso?.ToString(), c.Subtotal * c.Signo, c.Iva * c.Signo, c.OtrosImpuestos * c.Signo, c.Total * c.Signo,
            (r?.Renta ?? TratamientoRentaDocumento.Pendiente).ToString(), r?.CodigoSustento, r?.FormaPago, r?.ParteRelacionada == true ? "SI" : "NO" }; });
        FormatoTabular formato = excel ? new FormatoExcel() : new FormatoCsv(); return formato.Generar(Columnas, filas);
    }
    public async Task<byte[]> BalanceAsync(int uid, int anio, int mes, CancellationToken ct)
    {
        var desde = new DateOnly(anio, 1, 1); var hasta = PeriodosService.Fecha(anio, mes).AddMonths(1).AddDays(-1);
        var estados = await libro.EstadosAsync(uid, desde, hasta, ct);
        return new FormatoExcel().Generar(["Codigo", "Nombre", "Tipo", "Debe acumulado", "Haber acumulado", "Saldo deudor (+) / acreedor (-)"], estados.Cuentas.Select(c => new object?[] { c.Codigo, c.Nombre, c.Tipo.ToString(), c.Debe, c.Haber, c.Saldo }));
    }
    public async Task<byte[]> PaqueteAsync(int uid, int anio, int mes, int meses, CancellationToken ct)
    {
        await ExigirAccesoAsync(uid, ct); var (desde, hasta) = PreparacionTributariaService.Periodo(anio, mes, meses);
        // Una vista consistente del paquete evita combinar registros de dos revisiones concurrentes.
        await using var tx = await Control.AbrirAsync(uid, [], ct);
        var docs = await Documentos(uid, desde, hasta, ct);
        var retenciones = await Db.Set<RetencionSri>().AsNoTracking().Include(r => r.Detalles).Where(r => r.UsuarioId == uid && r.PeriodoFiscal >= desde && r.PeriodoFiscal < hasta).ToListAsync(ct);
        var presupuesto = docs.Sum(c => (long)Encoding.UTF8.GetByteCount(c.Xml?.Contenido ?? "")) + retenciones.Sum(r => (long)Encoding.UTF8.GetByteCount(r.Xml));
        if (presupuesto > 50 * 1024 * 1024) throw new BusinessException("El paquete supera 50 MB de XML. Descarga un período más corto.");
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true)) {
            void Bytes(string nombre, byte[] contenido) { using var file = zip.CreateEntry(nombre).Open(); file.Write(contenido); }
            void Texto(string nombre, string contenido) => Bytes(nombre, Encoding.UTF8.GetBytes(contenido));
            var json = new JsonSerializerOptions { WriteIndented = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
            Bytes("bases-comprobantes.xlsx", await ComprobantesAsync(uid, anio, mes, meses, true, ct));
            Bytes("bases-comprobantes.csv", await ComprobantesAsync(uid, anio, mes, meses, false, ct));
            foreach (var c in docs.Where(c => c.Xml is not null)) Texto($"xml/comprobante-{c.Id}.xml", c.Xml!.Contenido);
            foreach (var r in retenciones) Texto($"xml/retencion-{r.Id}.xml", r.Xml);
            Texto("preparacion-iva.json", JsonSerializer.Serialize(await preparacion.IvaAsync(uid, anio, mes, meses, ct), json));
            Texto("preparacion-renta-anual.json", JsonSerializer.Serialize(await preparacion.RentaAsync(uid, anio, ct), json));
            Texto("bases-ats.json", JsonSerializer.Serialize(await revision.AtsAsync(uid, anio, mes, meses, ct), json));
            Texto("retenciones.json", JsonSerializer.Serialize(await revision.RetencionesAsync(uid, anio, mes, meses, ct), json));
            var gastosPersonales = await preparacion.GastosAsync(uid, anio, ct);
            Texto("gastos-personales.json", JsonSerializer.Serialize(gastosPersonales, json));
            Bytes("gastos-personales.xlsx", new FormatoExcel().Generar(["Rubro", "Base personal confirmada sin IVA ni ICE", "Proyeccion anual"], gastosPersonales.Rubros.Select(r => new object?[] { r.Categoria.ToString(), r.Real, r.Proyectado })));
            Texto("revisiones.json", JsonSerializer.Serialize(await revision.HistorialAsync(uid, anio, ct), json));
            var asientos = await Db.Set<AsientoContable>().AsNoTracking().Include(a => a.Lineas).ThenInclude(l => l.Cuenta).Where(a => a.UsuarioId == uid && a.Fecha >= desde && a.Fecha < hasta).ToListAsync(ct);
            Bytes("diario.xlsx", new FormatoExcel().Generar(["Asiento", "Fecha", "Estado", "Concepto", "Cuenta", "Nombre", "Debe", "Haber"], asientos.SelectMany(a => a.Lineas.Select(l => new object?[] { a.Id, a.Fecha.ToString("yyyy-MM-dd"), a.Estado.ToString(), a.Concepto, l.Cuenta.Codigo, l.Cuenta.Nombre, l.Debe, l.Haber }))));
            var pagos = await Db.Set<PagoComprobante>().AsNoTracking().Where(p => p.UsuarioId == uid && p.Fecha >= desde && p.Fecha < hasta).ToListAsync(ct);
            Bytes("tesoreria.xlsx", new FormatoExcel().Generar(["Pago", "Documento", "Fecha", "Tipo", "Medio", "Importe", "Referencia", "Anulado"], pagos.Select(p => new object?[] { p.Id, p.ComprobanteId, p.Fecha.ToString("yyyy-MM-dd"), p.Tipo.ToString(), p.Medio.ToString(), p.Importe, p.Referencia, p.AnuladoEn.HasValue ? "SI" : "NO" })));
            Texto("LEEME.txt", $"Contribuyente #{uid}; período {desde:yyyy-MM-dd} a {hasta.AddDays(-1):yyyy-MM-dd}; reglas {ReglasTributarias.Version}.\nLos XML son originales. Los XLSX y CSV son bases de trabajo. Los JSON son preparaciones internas, no formularios aceptados por el SRI.\nIncluye preparaciones anuales de renta y las revisiones del ejercicio completo. Completa documentos externos, feriados y ajustes antes de declarar.\nEl paquete contiene datos privados: entrégalo únicamente a tu contador autorizado. También puedes autorizarlo desde Cartera y accesos.\n");
        }
        Control.Auditar(uid, "ExportarPaqueteContable", "Entrega", $"{anio}-{mes:D2}/{meses}", $"Documentos={docs.Count}; retenciones={retenciones.Count}");
        await Db.SaveChangesAsync(ct); await tx.ConfirmarAsync(ct); return stream.ToArray();
    }
    private async Task<List<Comprobante>> Documentos(int uid, DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var docs = await Db.Comprobantes.AsNoTracking().Include(c => c.Xml).Where(c => c.UsuarioId == uid && c.FechaEmision >= desde && c.FechaEmision < hasta).OrderBy(c => c.Id).Take(5001).ToListAsync(ct);
        if (docs.Count > 5000) throw new BusinessException("El período contiene más de 5000 documentos. Descarga un intervalo más corto.");
        return docs;
    }
}
