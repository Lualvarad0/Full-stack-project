using System.Globalization;
using System.Xml.Linq;
using Back.Models;
using Back.Seguridad;
namespace Back.Sri;

public static class RetencionXmlParser
{
    private static XElement? Hijo(XElement e, string n) => e.Elements().FirstOrDefault(x => x.Name.LocalName == n);
    private static string Texto(XElement e, string n) => Hijo(e, n)?.Value.Trim() ?? "";
    private static decimal Numero(XElement e, string n) => decimal.Parse(Texto(e, n), CultureInfo.InvariantCulture);
    public static RetencionSri Parse(string xml)
    {
        var r = XmlSeguro.Documento(xml).Root ?? throw new FormatException("XML vacío.");
        if (r.Name.LocalName != "comprobanteRetencion") throw new FormatException("El documento no es una retención.");
        var t = Hijo(r, "infoTributaria") ?? throw new FormatException("Falta infoTributaria.");
        var i = Hijo(r, "infoCompRetencion") ?? throw new FormatException("Falta infoCompRetencion.");
        var ret = new RetencionSri { ClaveAcceso = Texto(t, "claveAcceso"), Numero = $"{Texto(t, "estab")}-{Texto(t, "ptoEmi")}-{Texto(t, "secuencial")}",
            FechaEmision = DateOnly.ParseExact(Texto(i, "fechaEmision"), "dd/MM/yyyy", CultureInfo.InvariantCulture),
            PeriodoFiscal = DateOnly.ParseExact("01/" + Texto(i, "periodoFiscal"), "dd/MM/yyyy", CultureInfo.InvariantCulture),
            RucEmisor = Texto(t, "ruc"), RazonSocialEmisor = Texto(t, "razonSocial"),
            IdentificacionRetenido = Texto(i, "identificacionSujetoRetenido"), RazonSocialRetenido = Texto(i, "razonSocialSujetoRetenido"), Xml = xml };
        IEnumerable<(XElement Impuesto, string? Documento)> filas = Hijo(r, "impuestos")?.Elements().Select(x => (x, (string?)Texto(x, "numDocSustento"))) ?? [];
        if (Hijo(r, "docsSustento") is { } docs)
            filas = docs.Elements().SelectMany(d => (Hijo(d, "retenciones")?.Elements() ?? []).Select(x => (x, (string?)Texto(d, "numDocSustento"))));
        foreach (var (impuesto, documento) in filas)
        {
            var d = new RetencionSriDetalle { CodigoImpuesto = Texto(impuesto, "codigo"), CodigoRetencion = Texto(impuesto, "codigoRetencion"),
                BaseImponible = Numero(impuesto, "baseImponible"), Porcentaje = Numero(impuesto, "porcentajeRetener"), Valor = Numero(impuesto, "valorRetenido"), DocumentoSustento = documento };
            if (d.CodigoImpuesto is not ("1" or "2" or "6") || d.BaseImponible < 0 || d.Valor < 0 || d.Porcentaje < 0 || d.Porcentaje > 100)
                throw new FormatException("Detalle de retención no válido.");
            ret.Detalles.Add(d);
        }
        if (ret.Detalles.Count == 0 || ret.Detalles.Count > 500) throw new FormatException("Revisa los detalles de retención.");
        return ret;
    }
}
