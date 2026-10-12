using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
namespace Back.Contabilidad;

public abstract class FormatoTabular
{
    public abstract byte[] Generar(string[] columnas, IEnumerable<object?[]> filas);
    protected static string Texto(object? valor) => valor is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : valor?.ToString() ?? "";
}
public sealed class FormatoCsv : FormatoTabular
{
    public override byte[] Generar(string[] columnas, IEnumerable<object?[]> filas)
    {
        string Celda(object? valor) {
            var texto = Texto(valor);
            // Los importes conservan su tipo; los textos no pueden activar fórmulas en Excel.
            if (valor is string && texto.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@') texto = "'" + texto;
            return "\"" + texto.Replace("\"", "\"\"") + "\"";
        }
        var sb = new StringBuilder(); sb.AppendLine(string.Join(';', columnas.Select(Celda)));
        foreach (var fila in filas) sb.AppendLine(string.Join(';', fila.Select(Celda)));
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
}
public sealed class FormatoExcel : FormatoTabular
{
    public override byte[] Generar(string[] columnas, IEnumerable<object?[]> filas)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XElement Celda(object? v) => v is decimal or int or long or double ? new XElement(ns + "c", new XElement(ns + "v", Texto(v)))
            : new XElement(ns + "c", new XAttribute("t", "inlineStr"), new XElement(ns + "is", new XElement(ns + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), Texto(v))));
        var rows = new[] { columnas.Select(c => (object?)c).ToArray() }.Concat(filas).Select(f => new XElement(ns + "row", f.Select(Celda)));
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true)) {
            void Archivo(string nombre, string contenido) { using var writer = new StreamWriter(zip.CreateEntry(nombre).Open(), new UTF8Encoding(false)); writer.Write(contenido); }
            Archivo("[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            Archivo("_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Archivo("xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Datos\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Archivo("xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            Archivo("xl/worksheets/sheet1.xml", new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(ns + "worksheet", new XElement(ns + "sheetData", rows))).ToString());
        }
        return stream.ToArray();
    }
}
