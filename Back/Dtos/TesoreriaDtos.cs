using System.ComponentModel.DataAnnotations;
using Back.Models;
using Back.Seguridad;
namespace Back.Dtos;

public record RegistrarPagoRequest([Range(1, int.MaxValue)] int ComprobanteId, DateOnly Fecha,
    [Range(0.01, 999999999)] decimal Importe, [EnumDataType(typeof(TipoAplicacionPago))] TipoAplicacionPago Tipo,
    [EnumDataType(typeof(MedioPago))] MedioPago Medio, [TextoPlano, StringLength(100)] string? Referencia, [Range(1, int.MaxValue)] int? CuentaBancariaId);
public record AnularPagoRequest([Required, TextoPlano, StringLength(1000, MinimumLength = 10)] string Motivo);
public record CuentaBancariaRequest([Required, TextoPlano, StringLength(100)] string Nombre, [Required, TextoPlano, StringLength(100)] string Entidad,
    [Required, RegularExpression("^[A-Za-z0-9 -]{4,50}$")] string Numero, [Range(-999999999, 999999999)] decimal SaldoInicial, DateOnly FechaInicial);
public record MovimientoBancarioRequest([Required, TextoPlano, StringLength(100)] string IdExterno, DateOnly Fecha,
    [Required, TextoPlano, StringLength(300)] string Concepto, [TextoPlano, StringLength(100)] string? Referencia, [Range(-999999999, 999999999)] decimal Importe);
public record AplicacionConciliacionRequest([Range(1, int.MaxValue)] int PagoId, [Range(0.01, 999999999)] decimal Importe);
public record ConciliarRequest([Required, MinLength(1), MaxLength(100)] List<AplicacionConciliacionRequest> Pagos);
public record SaldoComprobanteDto(int Id, string Numero, string Tercero, Direccion Direccion, DateOnly Fecha, DateOnly? Vencimiento, decimal Total, decimal Notas, decimal Aplicado, decimal Saldo) { public decimal Retenciones { get; init; } }
public record PagoDto(int Id, int ComprobanteId, string Numero, DateOnly Fecha, decimal Importe, TipoAplicacionPago Tipo, MedioPago Medio, string? Referencia, bool Anulado, decimal PendienteConciliar, bool Entrada);
public record CuentaBancariaDto(int Id, string Nombre, string Entidad, string UltimosDigitos, decimal SaldoInicial, DateOnly FechaInicial, decimal Saldo, bool Activa);
public record MovimientoBancarioDto(int Id, DateOnly Fecha, string Concepto, string? Referencia, decimal Importe, decimal Conciliado) { public bool RevisadoSinFactura { get; init; } }
