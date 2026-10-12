using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using Back.Seguridad;
namespace Back.Tributacion;

public record ValidacionAtsDto(bool Valido, string Esquema, List<string> Errores, string Alcance);
public static class ValidadorAts
{
    private static readonly Lazy<XmlSchemaSet> Esquemas = new(() => {
        using var recurso = Assembly.GetExecutingAssembly().GetManifestResourceStream("Back.Tributacion.Esquemas.ats.xsd") ?? throw new InvalidOperationException("No está disponible el esquema ATS.");
        using var reader = XmlReader.Create(recurso, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var set = new XmlSchemaSet { XmlResolver = null }; set.Add(null, reader); set.Compile(); return set;
    });
    public static ValidacionAtsDto Validar(string xml, string ruc)
    {
        var errores = new List<string>();
        try {
            var doc = XmlSeguro.Documento(xml);
            if (doc.Root?.Name != "iva" || (string?)doc.Root.Element("IdInformante") != ruc) return Resultado(["El ATS debe pertenecer al RUC del contribuyente seleccionado."]);
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, ValidationType = ValidationType.Schema, Schemas = Esquemas.Value, MaxCharactersInDocument = 5 * 1024 * 1024 };
            settings.ValidationEventHandler += (_, e) => { if (errores.Count < 50) errores.Add(e.Message); };
            using var reader = XmlReader.Create(new StringReader(xml), settings); while (reader.Read()) { }
        } catch (XmlException e) { errores.Add($"El XML no se puede interpretar: {e.Message}"); }
        return Resultado(errores);
    }
    private static ValidacionAtsDto Resultado(List<string> errores) => new(errores.Count == 0, "ATS / ats.xsd publicado por SRI; consultado 11-10-2026", errores,
        "Comprueba estructura XSD y RUC del informante. Completa catálogos, reglas de negocio y aceptación con el programa ATS vigente del SRI.");
}
