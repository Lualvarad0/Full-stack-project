using System.Xml;
using System.Xml.Linq;

namespace Back.Seguridad;

public static class XmlSeguro
{
    public static XmlReader Leer(string xml) => XmlReader.Create(new StringReader(xml), new XmlReaderSettings
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersInDocument = 10 * 1024 * 1024,
        MaxCharactersFromEntities = 0,
    });

    public static XDocument Documento(string xml)
    {
        using var lector = Leer(xml);
        return XDocument.Load(lector);
    }
}
