namespace Back.Models
{
    public class Cliente
    {
        public int ClienteId { get; set; }
        public string Identificacion { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
    }
}
