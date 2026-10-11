namespace Back.Models
{
    /// <summary>
    /// Dirección de reenvío de cada usuario: &lt;Codigo&gt;@&lt;Correo:DominioEntrante&gt;.
    /// Los correos que llegan a ella se leen y sus XML se importan verificándolos con el SRI.
    /// </summary>
    public class BuzonCorreo
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
        public DateTime? UltimoCorreoEn { get; set; }

        /// <summary>Código que Gmail envía para confirmar la dirección de reenvío; se muestra al usuario.</summary>
        public string? ConfirmacionGmailCodigo { get; set; }
        public string? ConfirmacionGmailEnlace { get; set; }
        public DateTime? ConfirmacionGmailEn { get; set; }
    }

    /// <summary>Correo recibido en el buzón. Se guarda solo el texto útil (XML y cuerpo), nunca adjuntos completos.</summary>
    public class CorreoRecibido
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public DateTime RecibidoEn { get; set; } = DateTime.UtcNow;
        public string Remitente { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public EstadoCorreo Estado { get; set; } = EstadoCorreo.Pendiente;

        /// <summary>XML adjuntos y texto del cuerpo; se vacía al procesar.</summary>
        public string? Contenido { get; set; }
        public int ArchivosXml { get; set; }
        public int ClavesEncontradas { get; set; }
        public int Importados { get; set; }
        public int Ingresos { get; set; }
        public string? Detalle { get; set; }
        public DateTime? ProcesadoEn { get; set; }
        public int Intentos { get; set; }
    }

    public enum EstadoCorreo
    {
        Pendiente,
        Procesado,
        SinComprobantes,
        ConfirmacionGmail,
        Error,
    }
}
