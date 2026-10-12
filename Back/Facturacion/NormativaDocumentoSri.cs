using System.Xml.Linq;
using System.Text.RegularExpressions;
using Back.Helpers;
using Back.Models;
using Back.Seguridad;
namespace Back.Facturacion;

public static class NormativaDocumentoSri
{
    public static string AgregarProveedor(string xml, AmbienteSri ambiente, IConfiguration config)
    {
        var ruc = config["Sri:RucProveedor"];
        if (string.IsNullOrWhiteSpace(ruc))
        {
            if (ambiente == AmbienteSri.Produccion) throw BusinessException.Conflict("La facturación en producción requiere configurar el RUC del proveedor del sistema. Contacta al administrador.");
            return xml;
        }
        if (!Regex.IsMatch(ruc, "^[0-9]{13}$")) throw new InvalidOperationException("Sri:RucProveedor debe contener trece dígitos.");
        var doc = XmlSeguro.Documento(xml); var raiz = doc.Root!;
        var adicional = raiz.Element("infoAdicional");
        if (adicional is null) { adicional = new XElement("infoAdicional"); raiz.Add(adicional); }
        adicional.Add(new XElement("campoAdicional", new XAttribute("nombre", "RUC Proveedor"), ruc));
        return doc.Declaration + raiz.ToString(SaveOptions.DisableFormatting);
    }
}
