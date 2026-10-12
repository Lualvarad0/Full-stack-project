using Back.Models;
using Back.Seguridad;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System.Xml.Linq;
using ZXing;

namespace Back.Facturacion;

/// <summary>Representación de la factura original, sin leer datos posteriores del perfil.</summary>
public static class FacturaPdf
{
    private static readonly object FuentesLock = new();
    public static byte[] Crear(FacturaEmitida factura)
    {
        lock (FuentesLock) GlobalFontSettings.FontResolver ??= new Fuentes();
        using var doc = new PdfDocument();
        var raiz = XmlSeguro.Documento(factura.XmlFirmado).Root!;
        var nombre = raiz.Name.LocalName switch { "notaCredito" => "Nota de crédito", "comprobanteRetencion" => "Comprobante de retención", _ => "Factura" };
        doc.Info.Title = $"{nombre} {factura.Numero}";
        var tributaria = raiz.Element("infoTributaria")!;
        var info = raiz.Element("infoFactura") ?? raiz.Element("infoNotaCredito") ?? raiz.Element("infoCompRetencion")!;
        var esRetencion = raiz.Name.LocalName == "comprobanteRetencion";
        string Valor(XElement nodo, string nombre) => nodo.Element(nombre)?.Value ?? "";
        var normal = new XFont("Ubuntu", 10);
        var negrita = new XFont("Ubuntu", 10, XFontStyleEx.Bold);
        XGraphics? g = null;
        double y = 0;
        void Pagina()
        {
            g?.Dispose();
            var p = doc.AddPage(); p.Size = PdfSharp.PageSize.A4;
            g = XGraphics.FromPdfPage(p); y = 42;
            g.DrawString($"{nombre.ToUpperInvariant()} {factura.Numero}", negrita, XBrushes.Black, 36, y);
            y += 24;
            g.DrawString($"Estado registrado: {factura.Estado} | {factura.Ambiente}", normal, XBrushes.Black, 36, y);
            y += 22;
            if (factura.Estado != EstadoFactura.Autorizada || factura.Ambiente == AmbienteSri.Pruebas)
            { g.DrawString("SIN VALIDEZ TRIBUTARIA / VERIFICAR ESTADO EN EL SRI", negrita, XBrushes.DarkRed, 36, y); y += 22; }
        }
        // Ajuste por caracteres: también divide identificaciones y palabras largas.
        IEnumerable<string> Lineas(string texto, XFont fuente)
        {
            foreach (var parrafo in texto.Replace('\r', ' ').Split('\n'))
            {
                var restante = parrafo;
                while (restante.Length > 0)
                {
                    var n = Math.Min(95, restante.Length);
                    while (n > 1 && g!.MeasureString(restante[..n], fuente).Width > 520) n--;
                    if (n < restante.Length && restante.LastIndexOf(' ', n - 1, n) is var corte && corte > n / 2) n = corte;
                    yield return restante[..n]; restante = restante[n..].TrimStart();
                }
            }
        }
        void Texto(string texto, bool bold = false)
        {
            foreach (var linea in Lineas(texto, bold ? negrita : normal))
            { if (y > 760) Pagina(); g!.DrawString(linea, bold ? negrita : normal, XBrushes.Black, 36, y); y += 15; }
        }
        void Seccion(string titulo) { y += 12; Texto(titulo, true); y += 4; }
        Pagina();
        Texto(Valor(tributaria, "razonSocial"), true);
        Texto(Valor(tributaria, "nombreComercial"));
        Texto($"RUC: {Valor(tributaria, "ruc")}");
        Texto($"Dirección matriz: {Valor(tributaria, "dirMatriz")}");
        if (Valor(info, "dirEstablecimiento").Length > 0) Texto($"Dirección establecimiento: {Valor(info, "dirEstablecimiento")}");
        Texto($"Obligado a llevar contabilidad: {Valor(info, "obligadoContabilidad")}");
        Texto(Valor(tributaria, "contribuyenteRimpe"));
        if (Valor(tributaria, "agenteRetencion") is { Length: > 0 } agente) Texto($"Agente de retención: {agente}");
        var especial = Valor(info, "contribuyenteEspecial");
        if (especial.Length > 0) Texto($"Contribuyente especial: {especial}");
        Seccion("AUTORIZACIÓN Y CLAVE DE ACCESO");
        Texto($"Autorización: {factura.NumeroAutorizacion ?? "Pendiente"}");
        Texto($"Fecha autorización (Ecuador): {factura.FechaAutorizacion?.ToUniversalTime().AddHours(-5):dd/MM/yyyy HH:mm:ss}");
        Texto("Emisión: NORMAL");
        Texto(factura.ClaveAcceso);
        if (factura.ClaveAcceso.Length == 49 && factura.ClaveAcceso.All(char.IsAsciiDigit))
        {
            var barras = new MultiFormatWriter().encode(factura.ClaveAcceso, BarcodeFormat.CODE_128, 0, 1);
            var unidad = 520d / barras.Width;
            for (var x = 0; x < barras.Width; x++) if (barras[x, 0]) g!.DrawRectangle(XBrushes.Black, 36 + x * unidad, y, unidad, 38);
            y += 50;
        }
        Seccion(esRetencion ? "SUJETO RETENIDO" : "COMPRADOR");
        Texto($"Razón social: {Valor(info, esRetencion ? "razonSocialSujetoRetenido" : "razonSocialComprador")}");
        Texto($"Identificación: {Valor(info, esRetencion ? "identificacionSujetoRetenido" : "identificacionComprador")}");
        if (Valor(info, "direccionComprador").Length > 0) Texto($"Dirección: {Valor(info, "direccionComprador")}");
        Texto($"Fecha emisión: {Valor(info, "fechaEmision")}");
        if (esRetencion) Texto($"Período fiscal: {Valor(info, "periodoFiscal")}");
        if (raiz.Name.LocalName == "notaCredito") { Texto($"Documento modificado: {Valor(info, "numDocModificado")} | fecha: {Valor(info, "fechaEmisionDocSustento")}"); Texto($"Motivo: {Valor(info, "motivo")}"); }
        Seccion("DETALLE");
        foreach (var d in raiz.Element("detalles")?.Elements("detalle") ?? [])
        {
            if (y > 655) Pagina();
            Texto($"{Valor(d, raiz.Name.LocalName == "notaCredito" ? "codigoInterno" : "codigoPrincipal")} | {Valor(d, "descripcion")}", true);
            Texto($"Cantidad: {Valor(d, "cantidad")} | Precio: ${Valor(d, "precioUnitario")} | Descuento: ${Valor(d, "descuento")} | Subtotal: ${Valor(d, "precioTotalSinImpuesto")}");
            foreach (var impuesto in d.Element("impuestos")?.Elements("impuesto") ?? [])
                Texto($"IVA: {Valor(impuesto, "tarifa")}% | Valor: ${Valor(impuesto, "valor")}");
            y += 6;
        }
        if (esRetencion)
        {
            var retenciones = raiz.Descendants("retencion").Concat(raiz.Element("impuestos")?.Elements("impuesto") ?? []).ToList();
            foreach (var r in retenciones)
            {
                Texto($"Impuesto {Valor(r, "codigo")} | Código retención {Valor(r, "codigoRetencion")}", true);
                Texto($"Base: ${Valor(r, "baseImponible")} | Porcentaje: {Valor(r, "porcentajeRetener")}% | Retenido: ${Valor(r, "valorRetenido")}");
            }
            foreach (var docSustento in raiz.Descendants("docSustento")) Texto($"Sustento: {Valor(docSustento, "numDocSustento")}" + (Valor(docSustento, "fechaEmisionDocSustento").Length > 0 ? $" | fecha: {Valor(docSustento, "fechaEmisionDocSustento")}" : ""));
            var totalRetenido = retenciones.Sum(r => decimal.Parse(Valor(r, "valorRetenido"), System.Globalization.CultureInfo.InvariantCulture));
            Texto($"TOTAL RETENIDO: ${FacturaXml.Monto(totalRetenido)}", true);
        }
        else {
        Seccion("TOTALES (USD)");
        var descuento = Valor(info, "totalDescuento");
        if (descuento.Length == 0) descuento = FacturaXml.Monto((raiz.Element("detalles")?.Elements("detalle") ?? []).Sum(d => decimal.Parse(Valor(d, "descuento"), System.Globalization.CultureInfo.InvariantCulture)));
        Texto($"Subtotal sin impuestos: ${Valor(info, "totalSinImpuestos")} | Descuento: ${descuento}");
        foreach (var i in info.Element("totalConImpuestos")?.Elements("totalImpuesto") ?? [])
            Texto($"Impuesto código {Valor(i, "codigo")}/{Valor(i, "codigoPorcentaje")}: base ${Valor(i, "baseImponible")} | valor ${Valor(i, "valor")}");
        Texto((Valor(info, "propina").Length > 0 ? $"Propina: ${Valor(info, "propina")} | " : "") + $"TOTAL: ${Valor(info, raiz.Name.LocalName == "notaCredito" ? "valorModificacion" : "importeTotal")}", true);
        foreach (var pago in info.Element("pagos")?.Elements("pago") ?? [])
            Texto($"Pago: {FacturaXml.FormasPago.GetValueOrDefault(Valor(pago, "formaPago"), Valor(pago, "formaPago"))} | ${Valor(pago, "total")}");
        }
        var adicionales = raiz.Element("infoAdicional")?.Elements("campoAdicional").ToList() ?? [];
        if (adicionales.Count > 0) Seccion("INFORMACIÓN ADICIONAL");
        foreach (var campo in adicionales)
            Texto($"{campo.Attribute("nombre")?.Value}: {campo.Value}");
        g!.Dispose();
        for (var i = 0; i < doc.PageCount; i++)
        {
            using var pie = XGraphics.FromPdfPage(doc.Pages[i], XGraphicsPdfPageOptions.Append);
            pie.DrawString($"Representación impresa del documento electrónico | Página {i + 1} de {doc.PageCount}", normal, XBrushes.Gray, 36, 810);
        }
        using var salida = new MemoryStream(); doc.Save(salida, false); return salida.ToArray();
    }
    private sealed class Fuentes : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) => new(bold ? "Ubuntu-B" : "Ubuntu-R");
        public byte[] GetFont(string faceName)
        {
            using var s = typeof(FacturaPdf).Assembly.GetManifestResourceStream($"Back.Facturacion.Fonts.{faceName}.ttf")!;
            using var m = new MemoryStream(); s.CopyTo(m); return m.ToArray();
        }
    }
}
