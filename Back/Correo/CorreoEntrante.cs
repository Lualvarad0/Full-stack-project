using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using MimeKit;

namespace Back.Correo
{
    /// <summary>Lo que importa de un correo entrante, sin importar el proveedor que lo entregó.</summary>
    /// <summary>Pdfs: adjuntos PDF (posibles estados de cuenta de tarjetas); se leen en memoria y nunca se guardan.</summary>
    public record CorreoEntrante(
        IReadOnlyList<string> Destinatarios,
        string Remitente,
        string Asunto,
        IReadOnlyList<string> Xmls,
        string Texto,
        IReadOnlyList<AdjuntoPdf>? Pdfs = null);

    public record AdjuntoPdf(string Nombre, byte[] Contenido);

    /// <summary>
    /// Convierte correos entrantes en <see cref="CorreoEntrante"/>: MIME crudo (Cloudflare Email Workers,
    /// SendGrid "raw") o formularios multipart con adjuntos (Mailgun, SendGrid).
    /// </summary>
    public static partial class CorreoEntranteParser
    {
        public const long MaxBytesAdjunto = 5 * 1024 * 1024;
        private const int MaxPdfs = 3;

        private static bool EsPdf(string nombre, string? tipo) =>
            nombre.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || tipo is "application/pdf";

        // Encabezados donde aparece la dirección real de entrega cuando el correo fue reenviado.
        private static readonly string[] EncabezadosDestino = { "Delivered-To", "X-Original-To", "X-Forwarded-To", "Envelope-To" };

        public static async Task<CorreoEntrante> DesdeMimeAsync(Stream mime, IEnumerable<string> destinatariosExtra, CancellationToken ct)
        {
            var mensaje = await MimeMessage.LoadAsync(mime, ct);

            var destinatarios = destinatariosExtra
                .Concat(mensaje.To.Mailboxes.Select(m => m.Address))
                .Concat(mensaje.Cc.Mailboxes.Select(m => m.Address))
                .Concat(EncabezadosDestino.SelectMany(h => mensaje.Headers.Where(x => x.Field.Equals(h, StringComparison.OrdinalIgnoreCase)).Select(x => x.Value)))
                .ToList();

            var xmls = new List<string>();
            var pdfs = new List<AdjuntoPdf>();
            foreach (var adjunto in mensaje.Attachments.OfType<MimePart>())
            {
                if (adjunto.Content is null) continue;
                var nombre = adjunto.FileName ?? string.Empty;
                using var memoria = new MemoryStream();
                await adjunto.Content.DecodeToAsync(memoria, ct);
                if (memoria.Length > MaxBytesAdjunto) continue;
                xmls.AddRange(XmlDeArchivo(nombre, adjunto.ContentType.MimeType, memoria.ToArray()));
                if (EsPdf(nombre, adjunto.ContentType.MimeType) && pdfs.Count < MaxPdfs) pdfs.Add(new AdjuntoPdf(nombre, memoria.ToArray()));
            }

            var texto = mensaje.TextBody ?? HtmlATexto(mensaje.HtmlBody);
            var remitente = mensaje.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty;
            return new CorreoEntrante(destinatarios, remitente, mensaje.Subject ?? string.Empty, xmls, texto ?? string.Empty, pdfs);
        }

        /// <summary>Formulario multipart: campos de destinatario/remitente/asunto y archivos adjuntos.</summary>
        public static async Task<CorreoEntrante> DesdeFormularioAsync(IFormCollection form, IEnumerable<string> destinatariosExtra, CancellationToken ct)
        {
            // SendGrid con "POST the raw, full MIME message" envía todo el correo en el campo "email".
            if (form.TryGetValue("email", out var crudo) && !string.IsNullOrWhiteSpace(crudo))
                return await DesdeMimeAsync(new MemoryStream(Encoding.UTF8.GetBytes(crudo.ToString())), destinatariosExtra, ct);

            string Campo(params string[] nombres) =>
                nombres.Select(n => form.TryGetValue(n, out var v) ? v.ToString() : null).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

            var destinatarios = destinatariosExtra.Concat(new[] { Campo("recipient", "to", "To") }).ToList();
            var xmls = new List<string>();
            var pdfs = new List<AdjuntoPdf>();
            foreach (var archivo in form.Files)
            {
                if (archivo.Length > MaxBytesAdjunto) continue;
                using var memoria = new MemoryStream();
                await archivo.CopyToAsync(memoria, ct);
                xmls.AddRange(XmlDeArchivo(archivo.FileName, archivo.ContentType, memoria.ToArray()));
                if (EsPdf(archivo.FileName, archivo.ContentType) && pdfs.Count < MaxPdfs) pdfs.Add(new AdjuntoPdf(archivo.FileName, memoria.ToArray()));
            }

            return new CorreoEntrante(
                destinatarios,
                Campo("sender", "from", "From"),
                Campo("subject", "Subject"),
                xmls,
                Campo("body-plain", "stripped-text", "text") is { Length: > 0 } t ? t : HtmlATexto(Campo("body-html", "html")),
                pdfs);
        }

        public static IEnumerable<string> XmlDeArchivo(string nombre, string? tipo, byte[] contenido)
        {
            var esZip = nombre.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || tipo is "application/zip" or "application/x-zip-compressed";
            var esXml = nombre.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || tipo is "application/xml" or "text/xml";
            if (esXml) return new[] { Encoding.UTF8.GetString(contenido) };
            if (!esZip) return Array.Empty<string>();

            try
            {
                using var zip = new ZipArchive(new MemoryStream(contenido), ZipArchiveMode.Read);
                var xmls = new List<string>();
                foreach (var entrada in zip.Entries)
                {
                    if (!entrada.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || entrada.Length > MaxBytesAdjunto) continue;
                    using var lector = new StreamReader(entrada.Open(), Encoding.UTF8);
                    xmls.Add(lector.ReadToEnd());
                }
                return xmls;
            }
            catch (InvalidDataException)
            {
                return Array.Empty<string>(); // ZIP dañado: se ignora
            }
        }

        [GeneratedRegex("<[^>]+>")]
        private static partial Regex Etiquetas();

        private static string HtmlATexto(string? html) => html is null ? string.Empty : Etiquetas().Replace(html, " ");
    }
}
