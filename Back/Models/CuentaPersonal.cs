namespace Back.Models;

/// <summary>Tipo de cuenta que el usuario registra a mano (plan Personal y superiores).</summary>
public enum CategoriaCuenta
{
    Vivienda,
    ServiciosBasicos,
    Alimentacion,
    Transporte,
    Salud,
    Educacion,
    Deudas,
    Entretenimiento,
    Otros,
}

/// <summary>
/// Una cuenta por pagar del mes (arriendo, luz, tarjeta…), registrada a mano. Se marca pagada con el día en que se pagó.
/// </summary>
public class CuentaPersonal
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string Nombre { get; set; } = "";
    public CategoriaCuenta Categoria { get; set; }
    public decimal Monto { get; set; }
    /// <summary>Día del mes en que vence (opcional), para ordenar y avisar.</summary>
    public int? DiaVencimiento { get; set; }
    /// <summary>Día en que se pagó; null mientras está pendiente.</summary>
    public DateOnly? PagadaEl { get; set; }
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
}

/// <summary>Lo que el usuario dice que gana en un mes; base para calcular cuánto le queda.</summary>
public class IngresoMensual
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal Monto { get; set; }
}
