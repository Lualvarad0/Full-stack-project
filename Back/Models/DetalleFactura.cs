namespace Back.Models
{
    public class DetalleFactura
    {
        public int DetalleFacturaId { get; set; }
        public int FacturaId { get; set; }
        public Factura Factura { get; set; }
        public int ProductoId { get; set; }
        public Producto Producto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PrecioTotal { get => Cantidad * PrecioUnitario; }
    }

}
