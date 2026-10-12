namespace Back.Models;

public enum FinalidadComprobante { SinDefinir, Personal, Negocio, NoAplicable, Mixto }
public enum TratamientoIva { Pendiente, ConCredito, SinCredito }
public enum EstadoPeriodo { Abierto, Cerrado }
public class PeriodoContable
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public EstadoPeriodo Estado { get; set; }
    public DateTime? CerradoEn { get; set; }
    public int? CerradoPorId { get; set; }
}
public class AuditoriaContable
{
    public long Id { get; set; }
    public int UsuarioId { get; set; }
    /// <summary>
    /// Quién hizo la acción (el titular o su contador). Sin llave foránea a propósito: el registro de auditoría
    /// debe sobrevivir aunque la cuenta del actor se borre.
    /// </summary>
    public int ActorId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Accion { get; set; } = "";
    public string Entidad { get; set; } = "";
    public string Referencia { get; set; } = "";
    public string Detalle { get; set; } = "";
}
