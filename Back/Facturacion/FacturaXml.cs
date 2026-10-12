using System.Globalization;
using System.Security.Cryptography;
using System.Xml.Linq;
using Back.Models;
using Back.Sri;

namespace Back.Facturacion
{
    /// <summary>Construye la clave de acceso y el XML de una factura (versión 1.1.0 de la ficha técnica del SRI).</summary>
    public static class FacturaXml
    {
        public const string ConsumidorFinal = "9999999999999";

        /// <summary>Tabla 6 de la ficha técnica: tipo de identificación del comprador.</summary>
        public static class TiposIdentificacion
        {
            public const string Ruc = "04";
            public const string Cedula = "05";
            public const string Pasaporte = "06";
            public const string ConsumidorFinal = "07";
            public const string Exterior = "08";
            public static readonly string[] Todos = { Ruc, Cedula, Pasaporte, ConsumidorFinal, Exterior };
        }

        /// <summary>Tabla 24: formas de pago.</summary>
        public static readonly IReadOnlyDictionary<string, string> FormasPago = new Dictionary<string, string>
        {
            ["01"] = "Sin utilización del sistema financiero (efectivo)",
            ["15"] = "Compensación de deudas",
            ["16"] = "Tarjeta de débito",
            ["17"] = "Dinero electrónico",
            ["18"] = "Tarjeta prepago",
            ["19"] = "Tarjeta de crédito",
            ["20"] = "Otros con utilización del sistema financiero (transferencia)",
            ["21"] = "Endoso de títulos",
        };

        public static decimal Porcentaje(TarifaIva tarifa) => tarifa switch
        {
            TarifaIva.Iva15 => 15m,
            TarifaIva.Iva5 => 5m,
            _ => 0m,
        };

        /// <summary>
        /// Fecha (ddMMyyyy) | 01 | RUC | ambiente | establecimiento + punto | secuencial (9) | código numérico (8) |
        /// tipo de emisión 1 | dígito verificador módulo 11.
        /// </summary>
        public static string GenerarClave(DateOnly fecha, string ruc, AmbienteSri ambiente, string establecimiento, string puntoEmision, int secuencial, int? codigoNumerico = null, string tipoDocumento = "01")
        {
            var codigo = codigoNumerico ?? RandomNumberGenerator.GetInt32(10_000_000, 100_000_000);
            if (tipoDocumento is not ("01" or "04" or "07")) throw new ArgumentException("Tipo de documento no soportado.");
            var base48 = $"{fecha:ddMMyyyy}{tipoDocumento}{ruc}{(int)ambiente}{establecimiento}{puntoEmision}{secuencial:D9}{codigo:D8}1";
            return base48 + ClaveAcceso.DigitoVerificador(base48);
        }

        public static string Construir(FacturaEmitida factura, Usuario emisor, DatosEmisor datos)
        {
            var porTarifa = factura.Detalles
                .GroupBy(d => d.Tarifa)
                .Select(g => new XElement("totalImpuesto",
                    new XElement("codigo", "2"),
                    new XElement("codigoPorcentaje", (int)g.Key),
                    new XElement("baseImponible", Monto(g.Sum(d => d.Subtotal))),
                    new XElement("valor", Monto(g.Sum(d => d.Iva)))));

            var infoTributaria = new XElement("infoTributaria",
                new XElement("ambiente", (int)factura.Ambiente),
                new XElement("tipoEmision", "1"),
                new XElement("razonSocial", emisor.RazonSocial),
                Opcional("nombreComercial", datos.NombreComercial),
                new XElement("ruc", emisor.Ruc),
                new XElement("claveAcceso", factura.ClaveAcceso),
                new XElement("codDoc", factura.TipoDocumento),
                new XElement("estab", factura.Establecimiento),
                new XElement("ptoEmi", factura.PuntoEmision),
                new XElement("secuencial", factura.Secuencial.ToString("D9")),
                new XElement("dirMatriz", datos.DireccionMatriz),
                Opcional("contribuyenteRimpe", emisor.Regimen switch
                {
                    RegimenTributario.RimpeEmprendedor => "CONTRIBUYENTE RÉGIMEN RIMPE",
                    RegimenTributario.RimpeNegocioPopular => "CONTRIBUYENTE NEGOCIO POPULAR - RÉGIMEN RIMPE",
                    _ => null,
                }));

            var infoFactura = new XElement("infoFactura",
                new XElement("fechaEmision", factura.FechaEmision.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
                Opcional("dirEstablecimiento", datos.DireccionEstablecimiento ?? datos.DireccionMatriz),
                Opcional("contribuyenteEspecial", datos.ContribuyenteEspecial),
                new XElement("obligadoContabilidad", datos.ObligadoContabilidad ? "SI" : "NO"),
                new XElement("tipoIdentificacionComprador", factura.TipoIdentificacionComprador),
                new XElement("razonSocialComprador", factura.RazonSocialComprador),
                new XElement("identificacionComprador", factura.IdentificacionComprador),
                Opcional("direccionComprador", factura.DireccionComprador),
                new XElement("totalSinImpuestos", Monto(factura.Subtotal)),
                new XElement("totalDescuento", Monto(factura.Descuento)),
                new XElement("totalConImpuestos", porTarifa),
                new XElement("propina", "0.00"),
                new XElement("importeTotal", Monto(factura.Total)),
                new XElement("moneda", "DOLAR"),
                new XElement("pagos", new XElement("pago",
                    new XElement("formaPago", factura.FormaPago),
                    new XElement("total", Monto(factura.Total)))));

            var detalles = new XElement("detalles", factura.Detalles.Select((d, i) => new XElement("detalle",
                new XElement("codigoPrincipal", string.IsNullOrWhiteSpace(d.Codigo) ? $"ITEM{i + 1:D3}" : d.Codigo),
                new XElement("descripcion", d.Descripcion),
                new XElement("cantidad", Decimal6(d.Cantidad)),
                new XElement("precioUnitario", Decimal6(d.PrecioUnitario)),
                new XElement("descuento", Monto(d.Descuento)),
                new XElement("precioTotalSinImpuesto", Monto(d.Subtotal)),
                new XElement("impuestos", new XElement("impuesto",
                    new XElement("codigo", "2"),
                    new XElement("codigoPorcentaje", (int)d.Tarifa),
                    new XElement("tarifa", Porcentaje(d.Tarifa).ToString("0", CultureInfo.InvariantCulture)),
                    new XElement("baseImponible", Monto(d.Subtotal)),
                    new XElement("valor", Monto(d.Iva)))))));

            var adicional = new[]
            {
                Campo("Email", factura.CorreoComprador),
                Campo("Dirección", factura.DireccionComprador),
            }.Where(c => c is not null).ToList();

            var raiz = new XElement("factura",
                new XAttribute("id", "comprobante"),
                new XAttribute("version", "1.1.0"),
                infoTributaria,
                infoFactura,
                detalles,
                adicional.Count > 0 ? new XElement("infoAdicional", adicional) : null);

            return new XDocument(new XDeclaration("1.0", "UTF-8", null), raiz).Declaration + raiz.ToString(SaveOptions.DisableFormatting);
        }

        private static XElement? Opcional(string nombre, string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : new XElement(nombre, valor.Trim());

        private static XElement? Campo(string nombre, string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : new XElement("campoAdicional", new XAttribute("nombre", nombre), valor.Trim());

        public static string Monto(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture);

        private static string Decimal6(decimal valor) => Math.Round(valor, 6, MidpointRounding.AwayFromZero).ToString("0.00####", CultureInfo.InvariantCulture);
    }
}
