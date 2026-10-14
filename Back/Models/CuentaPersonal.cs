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
    /// <summary>Qué respalda el día de pago (planes de pago): factura, correo o lo que dijo el usuario.</summary>
    public RespaldoPago? Respaldo { get; set; }
    /// <summary>Factura (gasto) que respalda el pago, si la hay.</summary>
    public int? ComprobanteId { get; set; }
    /// <summary>Al cierre del mes quedó pendiente y el usuario la pasó al mes siguiente (esa cuenta nueva).</summary>
    public int? TrasladadaACuentaId { get; set; }
    /// <summary>Cuenta del mes anterior de la que viene esta deuda.</summary>
    public int? TrasladadaDesdeId { get; set; }
    /// <summary>Deuda de una tarjeta de crédito (según su estado de cuenta).</summary>
    public int? TarjetaCreditoId { get; set; }
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

    /// <summary>Ni pagada ni trasladada: sigue por pagar en su mes.</summary>
    public bool Pendiente => PagadaEl is null && TrasladadaACuentaId is null;
}

public enum RespaldoPago
{
    /// <summary>El usuario dijo el día (no hay factura ni correo que lo respalde).</summary>
    Manual,
    Factura,
    Correo,
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
