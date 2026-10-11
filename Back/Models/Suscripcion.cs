namespace Back.Models
{
    /// <summary>
    /// Suscripción de pago de un usuario (plan Profesional o Contadores) cobrada con tarjeta por Kushki.
    /// La tarjeta nunca pasa por Nudo: Kushki la tokeniza en el navegador y guarda los datos.
    /// </summary>
    public class Suscripcion
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public PlanSuscripcion Plan { get; set; }
        public Periodicidad Periodicidad { get; set; }
        public EstadoSuscripcion Estado { get; set; } = EstadoSuscripcion.EnPrueba;
        /// <summary>Id de la suscripción en Kushki (para cancelarla y reconocer sus webhooks).</summary>
        public string KushkiId { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
        /// <summary>Fecha del próximo cobro (el primero, al terminar la prueba gratis).</summary>
        public DateOnly ProximoCobro { get; set; }
        public DateTime? UltimoPagoEn { get; set; }
        public DateTime? CanceladaEn { get; set; }
        public string? MarcaTarjeta { get; set; }
        public string? UltimosDigitos { get; set; }

        public bool Vigente => Estado is EstadoSuscripcion.EnPrueba or EstadoSuscripcion.Activa or EstadoSuscripcion.PagoPendiente;
    }

    /// <summary>Cobro registrado a partir de los webhooks de Kushki.</summary>
    public class PagoSuscripcion
    {
        public int Id { get; set; }
        public int SuscripcionId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public decimal Monto { get; set; }
        public bool Aprobado { get; set; }
        /// <summary>Número de ticket de Kushki; evita registrar dos veces el mismo webhook.</summary>
        public string? Ticket { get; set; }
        public string? Detalle { get; set; }
    }

    public enum Periodicidad
    {
        Mensual,
        Anual,
    }

    /// <summary>
    /// EnPrueba: días gratis antes del primer cobro. PagoPendiente: un cobro falló y Kushki está reintentando.
    /// Cancelada: la canceló el usuario o se agotaron los reintentos (vuelve al plan Personal).
    /// </summary>
    public enum EstadoSuscripcion
    {
        EnPrueba,
        Activa,
        PagoPendiente,
        Cancelada,
    }
}
