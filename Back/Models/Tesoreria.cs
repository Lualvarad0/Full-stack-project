namespace Back.Models;

public enum TipoAplicacionPago { Pago, Reembolso }
public enum MedioPago { Efectivo, Transferencia, Tarjeta, Otro }
public class PagoComprobante
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int ComprobanteId { get; set; }
    public Comprobante Comprobante { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public decimal Importe { get; set; }
    public TipoAplicacionPago Tipo { get; set; }
    public MedioPago Medio { get; set; }
    public string? Referencia { get; set; }
    public int? CuentaBancariaId { get; set; }
    public DateTime? AnuladoEn { get; set; }
    public string? MotivoAnulacion { get; set; }
}
public class CuentaBancaria
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
    public string Entidad { get; set; } = "";
    public string NumeroCifrado { get; set; } = "";
    public string UltimosDigitos { get; set; } = "";
    public decimal SaldoInicial { get; set; }
    public DateOnly FechaInicial { get; set; }
    public bool Activa { get; set; } = true;
}
public class MovimientoBancario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int CuentaBancariaId { get; set; }
    public string IdExterno { get; set; } = "";
    public DateOnly Fecha { get; set; }
    public string Concepto { get; set; } = "";
    public string? Referencia { get; set; }
    public decimal Importe { get; set; }
    public DateTime? RevisadoSinFacturaEn { get; set; }
    public string? MotivoRevision { get; set; }
}
public class ConciliacionBancaria
{
    public int Id { get; set; }
    public int MovimientoBancarioId { get; set; }
    public int PagoComprobanteId { get; set; }
    public decimal Importe { get; set; }
}
