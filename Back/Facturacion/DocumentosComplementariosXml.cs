using System.Globalization;
using System.Xml.Linq;
using Back.Models;
using Back.Seguridad;
namespace Back.Facturacion;

/// <summary>Constructores especializados que comparten los datos tributarios y la firma del emisor.</summary>
public abstract class ConstructorDocumentoSri
{
    protected static XElement Tributaria(FacturaEmitida f, Usuario u, DatosEmisor d) =>
        new(XmlSeguro.Documento(FacturaXml.Construir(f, u, d)).Root!.Element("infoTributaria")!);
    protected static XElement? Opcional(string nombre, string? valor) => string.IsNullOrWhiteSpace(valor) ? null : new(nombre, valor);
    protected static string Serializar(XElement raiz) => new XDeclaration("1.0", "UTF-8", null) + raiz.ToString(SaveOptions.DisableFormatting);
    public abstract string Construir(FacturaEmitida f, Usuario u, DatosEmisor datos, Comprobante? sustento = null);
}
public sealed class ConstructorNotaCredito : ConstructorDocumentoSri
{
    public override string Construir(FacturaEmitida f, Usuario u, DatosEmisor d, Comprobante? sustento = null)
    {
        var baseXml = XmlSeguro.Documento(FacturaXml.Construir(f, u, d)).Root!;
        var info = new XElement("infoNotaCredito", new XElement("fechaEmision", f.FechaEmision.ToString("dd/MM/yyyy")),
            Opcional("dirEstablecimiento", d.DireccionEstablecimiento ?? d.DireccionMatriz),
            new XElement("tipoIdentificacionComprador", f.TipoIdentificacionComprador), new XElement("razonSocialComprador", f.RazonSocialComprador),
            new XElement("identificacionComprador", f.IdentificacionComprador), Opcional("contribuyenteEspecial", d.ContribuyenteEspecial),
            new XElement("obligadoContabilidad", d.ObligadoContabilidad ? "SI" : "NO"), new XElement("codDocModificado", "01"),
            new XElement("numDocModificado", f.NumeroDocumentoSustento), new XElement("fechaEmisionDocSustento", f.FechaDocumentoSustento!.Value.ToString("dd/MM/yyyy")),
            new XElement("totalSinImpuestos", FacturaXml.Monto(f.Subtotal)), new XElement("valorModificacion", FacturaXml.Monto(f.Total)), new XElement("moneda", "DOLAR"),
            new XElement(baseXml.Element("infoFactura")!.Element("totalConImpuestos")!), new XElement("motivo", f.Motivo));
        var detalles = new XElement(baseXml.Element("detalles")!);
        foreach (var codigo in detalles.Elements().Select(x => x.Element("codigoPrincipal"))) if (codigo is not null) codigo.Name = "codigoInterno";
        return Serializar(new XElement("notaCredito", new XAttribute("id", "comprobante"), new XAttribute("version", "1.1.0"), Tributaria(f, u, d), info, detalles, baseXml.Element("infoAdicional")));
    }
}
public sealed class ConstructorRetencion : ConstructorDocumentoSri
{
    public override string Construir(FacturaEmitida f, Usuario u, DatosEmisor d, Comprobante? sustento = null)
    {
        if (sustento?.Xml is null) throw new InvalidOperationException("La retención requiere el XML original del sustento.");
        var original = XmlSeguro.Documento(sustento.Xml.Contenido).Root!.Element("infoFactura")!;
        var impuestos = original.Element("totalConImpuestos")?.Elements() ?? [];
        decimal M(XElement e, string nombre) => decimal.TryParse(e.Element(nombre)?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : 0;
        var doc = new XElement("docSustento", new XElement("codSustento", f.CodigoSustento), new XElement("codDocSustento", "01"),
            new XElement("numDocSustento", sustento.NumeroComprobante.Replace("-", "")), new XElement("fechaEmisionDocSustento", sustento.FechaEmision.ToString("dd/MM/yyyy")),
            new XElement("fechaRegistroContable", f.FechaEmision.ToString("dd/MM/yyyy")), new XElement("numAutDocSustento", sustento.NumeroAutorizacion),
            new XElement("pagoLocExt", "01"), new XElement("totalSinImpuestos", FacturaXml.Monto(sustento.Subtotal)), new XElement("importeTotal", FacturaXml.Monto(sustento.Total)),
            new XElement("impuestosDocSustento", impuestos.Select(i => new XElement("impuestoDocSustento", new XElement("codImpuestoDocSustento", i.Element("codigo")?.Value),
                new XElement("codigoPorcentaje", i.Element("codigoPorcentaje")?.Value), new XElement("baseImponible", i.Element("baseImponible")?.Value),
                new XElement("tarifa", i.Element("tarifa")?.Value ?? (M(i, "baseImponible") == 0 ? "0" : Math.Round(M(i, "valor") / M(i, "baseImponible") * 100, 2).ToString(CultureInfo.InvariantCulture))),
                new XElement("valorImpuesto", i.Element("valor")?.Value)))),
            new XElement("retenciones", f.Retenciones.Select(r => new XElement("retencion", new XElement("codigo", r.CodigoImpuesto), new XElement("codigoRetencion", r.CodigoRetencion),
                new XElement("baseImponible", FacturaXml.Monto(r.BaseImponible)), new XElement("porcentajeRetener", r.Porcentaje.ToString("0.####", CultureInfo.InvariantCulture)), new XElement("valorRetenido", FacturaXml.Monto(r.Valor))))),
            new XElement("pagos", new XElement("pago", new XElement("formaPago", f.FormaPago), new XElement("total", FacturaXml.Monto(sustento.Total)))));
        var info = new XElement("infoCompRetencion", new XElement("fechaEmision", f.FechaEmision.ToString("dd/MM/yyyy")), Opcional("dirEstablecimiento", d.DireccionEstablecimiento ?? d.DireccionMatriz),
            Opcional("contribuyenteEspecial", d.ContribuyenteEspecial), new XElement("obligadoContabilidad", d.ObligadoContabilidad ? "SI" : "NO"),
            new XElement("tipoIdentificacionSujetoRetenido", "04"), new XElement("parteRel", "NO"), new XElement("razonSocialSujetoRetenido", f.RazonSocialComprador),
            new XElement("identificacionSujetoRetenido", f.IdentificacionComprador), new XElement("periodoFiscal", f.FechaEmision.ToString("MM/yyyy")));
        return Serializar(new XElement("comprobanteRetencion", new XAttribute("id", "comprobante"), new XAttribute("version", "2.0.0"), Tributaria(f, u, d), info, new XElement("docsSustento", doc)));
    }
}
