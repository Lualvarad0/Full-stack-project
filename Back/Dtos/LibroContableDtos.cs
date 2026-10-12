using System.ComponentModel.DataAnnotations;
using Back.Models;
using Back.Seguridad;
namespace Back.Dtos;

public record CuentaContableRequest([Required, RegularExpression("^[0-9]+(\\.[0-9]+)*$"), StringLength(30)] string Codigo,
    [Required, TextoPlano, StringLength(150)] string Nombre, [EnumDataType(typeof(TipoCuentaContable))] TipoCuentaContable Tipo);
public record LineaAsientoRequest([Range(1, int.MaxValue)] int CuentaId, [Range(0, 999999999)] decimal Debe, [Range(0, 999999999)] decimal Haber);
public record AsientoRequest(DateOnly Fecha, [Required, TextoPlano, StringLength(500)] string Concepto,
    [Required, MinLength(2), MaxLength(200)] List<LineaAsientoRequest> Lineas);
public record CuentaContableDto(int Id, string Codigo, string Nombre, TipoCuentaContable Tipo, bool Activa);
public record LineaAsientoDto(int CuentaId, string Codigo, string Nombre, decimal Debe, decimal Haber);
public record AsientoDto(int Id, DateOnly Fecha, string Concepto, EstadoAsiento Estado, int? ComprobanteId, int? PagoComprobanteId, List<LineaAsientoDto> Lineas);
public record SaldoCuentaDto(int Id, string Codigo, string Nombre, TipoCuentaContable Tipo, decimal Debe, decimal Haber, decimal Saldo);
public record MayorDto(int AsientoId, DateOnly Fecha, string Concepto, decimal Debe, decimal Haber, decimal Saldo);
public record EstadosContablesDto(decimal Activos, decimal Pasivos, decimal Patrimonio, decimal Ingresos, decimal Gastos, decimal Resultado, decimal DiferenciaBalance, List<SaldoCuentaDto> Cuentas);
