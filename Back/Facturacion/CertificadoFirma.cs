using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Back.Helpers;

namespace Back.Facturacion
{
    /// <summary>Datos visibles del certificado de una firma electrónica.</summary>
    public record DatosCertificado(
        string Titular,
        string? Identificacion,
        string Emisor,
        string NumeroSerie,
        DateTime ValidaDesde,
        DateTime ValidaHasta);

    /// <summary>
    /// Abre un archivo .p12/.pfx de firma electrónica (Banco Central, Security Data, ANF, Uanataca…)
    /// y elige el certificado con clave privada que sirve para firmar.
    /// </summary>
    public static partial class CertificadoFirma
    {
        [GeneratedRegex(@"(?<!\d)(\d{13}|\d{10})(?!\d)")]
        private static partial Regex NumeroIdentificacion();

        /// <summary>Carga el certificado de firma; lanza BusinessException si el archivo o la contraseña no sirven.</summary>
        public static X509Certificate2 Abrir(byte[] archivo, string clave)
        {
            X509Certificate2Collection coleccion;
            try
            {
                coleccion = new X509Certificate2Collection();
                // EphemeralKeySet: la clave privada vive solo en memoria, no se escribe en el almacén del sistema.
                coleccion.Import(archivo, clave, X509KeyStorageFlags.EphemeralKeySet);
            }
            catch (CryptographicException)
            {
                throw new BusinessException("No se pudo abrir la firma: revisa que el archivo sea .p12 y que la contraseña sea correcta.");
            }

            var conClave = coleccion.Where(c => c.HasPrivateKey && c.GetRSAPrivateKey() is not null).ToList();
            // Algunas entidades incluyen dos certificados con clave (firma y cifrado): se prefiere el de firma digital.
            var firma = conClave.FirstOrDefault(EsDeFirma) ?? conClave.FirstOrDefault();
            return firma ?? throw new BusinessException("El archivo no contiene un certificado con clave privada para firmar.");
        }

        public static DatosCertificado Describir(X509Certificate2 certificado) => new(
            certificado.GetNameInfo(X509NameType.SimpleName, forIssuer: false),
            BuscarIdentificacion(certificado),
            certificado.GetNameInfo(X509NameType.SimpleName, forIssuer: true),
            certificado.SerialNumber,
            certificado.NotBefore.ToUniversalTime(),
            certificado.NotAfter.ToUniversalTime());

        private static bool EsDeFirma(X509Certificate2 certificado)
        {
            var uso = certificado.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault();
            return uso is null || uso.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature) || uso.KeyUsages.HasFlag(X509KeyUsageFlags.NonRepudiation);
        }

        /// <summary>
        /// Cédula o RUC del titular. Las entidades ecuatorianas la ponen en el SERIALNUMBER del sujeto o en
        /// extensiones propias (texto UTF-8 dentro de la extensión), así que se busca en ambos lugares.
        /// </summary>
        private static string? BuscarIdentificacion(X509Certificate2 certificado)
        {
            var candidatos = new List<string>();
            foreach (var parte in certificado.SubjectName.Name.Split(','))
            {
                var limpio = parte.Trim();
                if (limpio.StartsWith("SERIALNUMBER=", StringComparison.OrdinalIgnoreCase) || limpio.StartsWith("OID.2.5.4.5=", StringComparison.OrdinalIgnoreCase))
                    candidatos.Add(limpio[(limpio.IndexOf('=') + 1)..]);
            }
            foreach (var extension in certificado.Extensions)
            {
                if (extension.Oid?.Value?.StartsWith("1.3.6.1.4.1.") == true)
                    candidatos.Add(Encoding.UTF8.GetString(extension.RawData));
            }

            var numeros = candidatos.SelectMany(c => NumeroIdentificacion().Matches(c).Select(m => m.Value)).ToList();
            // Un RUC (13 dígitos) identifica mejor al emisor que la cédula.
            return numeros.FirstOrDefault(n => n.Length == 13) ?? numeros.FirstOrDefault();
        }
    }
}
