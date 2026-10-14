namespace Back.Models;

/// <summary>
/// Tarjeta de crédito del usuario (planes de pago). Nunca se guarda el número completo: solo el BIN (6 primeros
/// dígitos, que identifican al banco) y los 4 últimos. Marca, banco y tipo vienen de la consulta del BIN.
/// </summary>
public class TarjetaCredito
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Alias { get; set; } = "";
    public string Bin { get; set; } = "";
    public string Ultimos4 { get; set; } = "";
    public string Marca { get; set; } = "";
    /// <summary>Banco emisor según el BIN; null si no se pudo consultar.</summary>
    public string? Banco { get; set; }
    /// <summary>true: el BIN se verificó con el procesador de pagos (banco emisor real, tarjeta de crédito).</summary>
    public bool Verificada { get; set; }
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Lo que dice el estado de cuenta mensual de una tarjeta: saldo, pago mínimo y fecha máxima de pago.
/// Se lee del PDF (subido o reenviado al buzón) y el usuario lo confirma antes de convertirlo en deuda del mes.
/// El PDF no se guarda.
/// </summary>
public class EstadoCuentaTarjeta
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int TarjetaCreditoId { get; set; }
    public DateOnly? FechaCorte { get; set; }
    public DateOnly? FechaMaximaPago { get; set; }
    public decimal? SaldoTotal { get; set; }
    public decimal? PagoMinimo { get; set; }
    public OrigenEstadoCuenta Origen { get; set; }
    public EstadoRevision Estado { get; set; } = EstadoRevision.PorConfirmar;
    /// <summary>Deuda creada en Mis cuentas al confirmar.</summary>
    public int? CuentaPersonalId { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

public enum OrigenEstadoCuenta
{
    Subido,
    Correo,
    Manual,
}

public enum EstadoRevision
{
    PorConfirmar,
    Confirmado,
    Descartado,
}
