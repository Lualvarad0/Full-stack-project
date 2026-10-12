namespace Back.Models;

public enum PermisoContador { Consulta, Edicion }
public enum EstadoVinculo { Pendiente, Activo, Rechazado, Revocado }
public class VinculoContador
{
    public int Id { get; set; }
    public int PropietarioId { get; set; }
    public int ContadorId { get; set; }
    public int SolicitadoPorId { get; set; }
    public PermisoContador Permiso { get; set; }
    public EstadoVinculo Estado { get; set; } = EstadoVinculo.Pendiente;
    public DateTime SolicitadoEn { get; set; } = DateTime.UtcNow;
    public DateTime ExpiraEn { get; set; } = DateTime.UtcNow.AddDays(7);
    public DateTime? RespondidoEn { get; set; }
}
