namespace Back.Models
{
    public class Factura
    {
        public int FacturaId { get; set; }
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public string FormaPago { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public List<DetalleFactura> Detalles { get; set; } = new();
    }
}
