namespace Back.Models
{
    public class Producto
    {
        public int ProductoId { get; set; }
        public int? UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }

        // Mapeado a la columna de sistema xmin de PostgreSQL: evita sobrevender stock en ventas concurrentes.
        public uint Version { get; set; }
    }
}
