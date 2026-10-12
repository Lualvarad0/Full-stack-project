using Back.Seguridad;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace Back.Facturacion
{
    /// <summary>
    /// Firma XAdES-BES enveloped de comprobantes electrónicos, con la estructura que publica el SRI en su
    /// ficha técnica: RSA-SHA1, C14N inclusiva y tres referencias (SignedProperties, certificado y comprobante).
    /// </summary>
    public static class FirmadorXades
    {
        public const string Ds = "http://www.w3.org/2000/09/xmldsig#";
        public const string Etsi = "http://uri.etsi.org/01903/v1.3.2#";
        private const string C14n = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
        private const string RsaSha1 = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
        private const string Sha1 = "http://www.w3.org/2000/09/xmldsig#sha1";

        /// <summary>Devuelve el XML del comprobante con la firma como último hijo del elemento raíz.</summary>
        public static string Firmar(string xmlComprobante, X509Certificate2 certificado, DateTimeOffset? momento = null)
        {
            var documento = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            using (var lector = XmlSeguro.Leer(xmlComprobante)) documento.Load(lector);
            var raiz = documento.DocumentElement ?? throw new ArgumentException("XML vacío.", nameof(xmlComprobante));

            // Referencia al comprobante con transformación enveloped: es el documento antes de agregar la firma.
            var digestComprobante = Digest(Canonicalizar(documento));

            var ids = new Ids(RandomNumberGenerator.GetInt32(100_000, 999_999));
            var rsa = certificado.GetRSAPublicKey() ?? throw new ArgumentException("El certificado no tiene clave RSA.");
            var publica = rsa.ExportParameters(false);
            var hora = (momento ?? DateTimeOffset.UtcNow).ToOffset(TimeSpan.FromHours(-5));
            var serie = new BigInteger(certificado.GetSerialNumber(), isUnsigned: true, isBigEndian: false);

            var firmaXml =
                $"<ds:Signature xmlns:ds=\"{Ds}\" Id=\"{ids.Firma}\">" +
                $"<ds:SignedInfo Id=\"{ids.Firma}-SignedInfo{ids.N}\">" +
                $"<ds:CanonicalizationMethod Algorithm=\"{C14n}\"/>" +
                $"<ds:SignatureMethod Algorithm=\"{RsaSha1}\"/>" +
                $"<ds:Reference Id=\"SignedPropertiesID{ids.N}\" Type=\"http://uri.etsi.org/01903#SignedProperties\" URI=\"#{ids.Propiedades}\">" +
                $"<ds:DigestMethod Algorithm=\"{Sha1}\"/><ds:DigestValue></ds:DigestValue></ds:Reference>" +
                $"<ds:Reference URI=\"#{ids.Certificado}\"><ds:DigestMethod Algorithm=\"{Sha1}\"/><ds:DigestValue></ds:DigestValue></ds:Reference>" +
                $"<ds:Reference Id=\"{ids.Referencia}\" URI=\"#comprobante\">" +
                "<ds:Transforms><ds:Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\"/></ds:Transforms>" +
                $"<ds:DigestMethod Algorithm=\"{Sha1}\"/><ds:DigestValue>{digestComprobante}</ds:DigestValue></ds:Reference>" +
                "</ds:SignedInfo>" +
                $"<ds:SignatureValue Id=\"SignatureValue{ids.N}\"></ds:SignatureValue>" +
                $"<ds:KeyInfo Id=\"{ids.Certificado}\">" +
                $"<ds:X509Data><ds:X509Certificate>{Convert.ToBase64String(certificado.RawData)}</ds:X509Certificate></ds:X509Data>" +
                "<ds:KeyValue><ds:RSAKeyValue>" +
                $"<ds:Modulus>{Convert.ToBase64String(publica.Modulus!)}</ds:Modulus><ds:Exponent>{Convert.ToBase64String(publica.Exponent!)}</ds:Exponent>" +
                "</ds:RSAKeyValue></ds:KeyValue></ds:KeyInfo>" +
                $"<ds:Object Id=\"{ids.Firma}-Object{ids.N}\">" +
                $"<etsi:QualifyingProperties xmlns:etsi=\"{Etsi}\" Target=\"#{ids.Firma}\">" +
                $"<etsi:SignedProperties Id=\"{ids.Propiedades}\">" +
                "<etsi:SignedSignatureProperties>" +
                $"<etsi:SigningTime>{hora.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture)}</etsi:SigningTime>" +
                "<etsi:SigningCertificate><etsi:Cert><etsi:CertDigest>" +
                $"<ds:DigestMethod Algorithm=\"{Sha1}\"/><ds:DigestValue>{Digest(certificado.RawData)}</ds:DigestValue>" +
                "</etsi:CertDigest><etsi:IssuerSerial>" +
                $"<ds:X509IssuerName>{System.Security.SecurityElement.Escape(certificado.IssuerName.Name)}</ds:X509IssuerName>" +
                $"<ds:X509SerialNumber>{serie}</ds:X509SerialNumber>" +
                "</etsi:IssuerSerial></etsi:Cert></etsi:SigningCertificate>" +
                "</etsi:SignedSignatureProperties>" +
                "<etsi:SignedDataObjectProperties>" +
                $"<etsi:DataObjectFormat ObjectReference=\"#{ids.Referencia}\">" +
                "<etsi:Description>contenido comprobante</etsi:Description><etsi:MimeType>text/xml</etsi:MimeType>" +
                "</etsi:DataObjectFormat></etsi:SignedDataObjectProperties>" +
                "</etsi:SignedProperties></etsi:QualifyingProperties></ds:Object>" +
                "</ds:Signature>";

            var fragmento = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            fragmento.LoadXml(firmaXml);
            var firma = (XmlElement)raiz.AppendChild(documento.ImportNode(fragmento.DocumentElement!, true))!;

            var ns = new XmlNamespaceManager(documento.NameTable);
            ns.AddNamespace("ds", Ds);
            ns.AddNamespace("etsi", Etsi);
            var referencias = firma.SelectNodes("ds:SignedInfo/ds:Reference/ds:DigestValue", ns)!;

            // Los digests de SignedProperties y KeyInfo se calculan ya dentro del documento, con los xmlns heredados.
            // xmlns:etsi va en QualifyingProperties (no en Signature): así SignedInfo y KeyInfo solo heredan xmlns:ds,
            // que usan, y su forma canónica es la misma en cualquier implementación de C14N (Java, .NET, la del SRI).
            referencias[0]!.InnerText = Digest(CanonicalizarElemento((XmlElement)firma.SelectSingleNode("ds:Object/etsi:QualifyingProperties/etsi:SignedProperties", ns)!));
            referencias[1]!.InnerText = Digest(CanonicalizarElemento((XmlElement)firma.SelectSingleNode("ds:KeyInfo", ns)!));

            var signedInfo = CanonicalizarElemento((XmlElement)firma.SelectSingleNode("ds:SignedInfo", ns)!);
            using var privada = certificado.GetRSAPrivateKey() ?? throw new ArgumentException("El certificado no tiene clave privada.");
            firma.SelectSingleNode("ds:SignatureValue", ns)!.InnerText =
                Convert.ToBase64String(privada.SignData(signedInfo, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));

            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + raiz.OuterXml;
        }

        /// <summary>
        /// Comprueba digests y firma de un comprobante firmado por <see cref="Firmar"/> (la usan las pruebas y
        /// sirve para detectar un XML alterado antes de enviarlo).
        /// </summary>
        public static bool Verificar(string xmlFirmado)
        {
            var documento = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            using (var lector = XmlSeguro.Leer(xmlFirmado)) documento.Load(lector);
            var ns = new XmlNamespaceManager(documento.NameTable);
            ns.AddNamespace("ds", Ds);
            ns.AddNamespace("etsi", Etsi);
            var firma = (XmlElement)documento.DocumentElement!.SelectSingleNode("ds:Signature", ns)!;
            var digests = firma.SelectNodes("ds:SignedInfo/ds:Reference/ds:DigestValue", ns)!;

            var propiedades = Digest(CanonicalizarElemento((XmlElement)firma.SelectSingleNode("ds:Object/etsi:QualifyingProperties/etsi:SignedProperties", ns)!));
            var keyInfo = Digest(CanonicalizarElemento((XmlElement)firma.SelectSingleNode("ds:KeyInfo", ns)!));
            var signedInfo = CanonicalizarElemento((XmlElement)firma.SelectSingleNode("ds:SignedInfo", ns)!);
            var valor = Convert.FromBase64String(firma.SelectSingleNode("ds:SignatureValue", ns)!.InnerText);
            var certificado = new X509Certificate2(Convert.FromBase64String(firma.SelectSingleNode("ds:KeyInfo/ds:X509Data/ds:X509Certificate", ns)!.InnerText));

            firma.ParentNode!.RemoveChild(firma);
            var comprobante = Digest(Canonicalizar(documento));

            using var rsa = certificado.GetRSAPublicKey()!;
            return digests[0]!.InnerText == propiedades
                && digests[1]!.InnerText == keyInfo
                && digests[2]!.InnerText == comprobante
                && rsa.VerifyData(signedInfo, valor, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        }

        private static byte[] Canonicalizar(XmlDocument documento)
        {
            var transform = new XmlDsigC14NTransform();
            transform.LoadInput(documento);
            using var salida = (Stream)transform.GetOutput(typeof(Stream));
            using var memoria = new MemoryStream();
            salida.CopyTo(memoria);
            return memoria.ToArray();
        }

        /// <summary>C14N inclusiva de un elemento como subconjunto del documento: incluye los xmlns heredados.</summary>
        private static byte[] CanonicalizarElemento(XmlElement elemento)
        {
            var aislado = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            var copia = (XmlElement)aislado.ImportNode(elemento, true);
            aislado.AppendChild(copia);
            for (var ancestro = elemento.ParentNode as XmlElement; ancestro is not null; ancestro = ancestro.ParentNode as XmlElement)
            {
                foreach (XmlAttribute atributo in ancestro.Attributes)
                {
                    if (atributo.Prefix == "xmlns" && !copia.HasAttribute(atributo.Name))
                    {
                        var declaracion = aislado.CreateAttribute("xmlns", atributo.LocalName, "http://www.w3.org/2000/xmlns/");
                        declaracion.Value = atributo.Value;
                        copia.SetAttributeNode(declaracion);
                    }
                }
            }
            return Canonicalizar(aislado);
        }

        private static string Digest(byte[] datos) => Convert.ToBase64String(SHA1.HashData(datos));

        private sealed record Ids(int N)
        {
            public string Firma => $"Signature{N}";
            public string Propiedades => $"Signature{N}-SignedProperties{N}";
            public string Certificado => $"Certificate{N}";
            public string Referencia => $"Reference-ID-{N}";
        }
    }
}
