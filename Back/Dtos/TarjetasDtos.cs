using System.ComponentModel.DataAnnotations;
using Back.Models;
using Back.Seguridad;

namespace Back.Dtos;

public record EstadoCuentaDto(int Id, int TarjetaId, DateOnly? FechaCorte, DateOnly? FechaMaximaPago, decimal? SaldoTotal, decimal? PagoMinimo,
    OrigenEstadoCuenta Origen, EstadoRevision Estado, int? CuentaPersonalId, DateTime CreadoEn)
{
    public static EstadoCuentaDto From(EstadoCuentaTarjeta e) => new(e.Id, e.TarjetaCreditoId, e.FechaCorte, e.FechaMaximaPago, e.SaldoTotal,
        e.PagoMinimo, e.Origen, e.Estado, e.CuentaPersonalId, e.CreadoEn);
}

/// <summary>Tarjeta con su último estado de cuenta confirmado (la deuda que se conoce).</summary>
public record TarjetaDto(int Id, string Alias, string Marca, string? Banco, string Ultimos4, bool Verificada, EstadoCuentaDto? UltimoEstado);

/// <summary>Tarjetas del usuario y los estados de cuenta leídos que esperan su confirmación.</summary>
public record TarjetasDto(List<TarjetaDto> Tarjetas, List<EstadoCuentaDto> PorConfirmar);

/// <summary>
/// El número completo nunca llega al servidor: el navegador valida el dígito de control (Luhn) y envía
/// solo el BIN (6 primeros dígitos, para identificar al banco) y los 4 últimos.
/// </summary>
public record TarjetaRequest(
    [Required, TextoPlano, StringLength(60, MinimumLength = 2)] string Alias,
    [Required, RegularExpression(@"^\d{6}$", ErrorMessage = "El BIN son los 6 primeros dígitos de la tarjeta.")] string Bin,
    [Required, RegularExpression(@"^\d{4}$", ErrorMessage = "Indica los 4 últimos dígitos de la tarjeta.")] string Ultimos4);

/// <summary>Datos del estado de cuenta tal como el usuario los confirma (o los escribe a mano).</summary>
public record EstadoCuentaRequest(
    [Range(0.01, 9999999)] decimal SaldoTotal,
    [Range(0, 9999999)] decimal? PagoMinimo,
    [Required] DateOnly FechaMaximaPago,
    DateOnly? FechaCorte,
    /// <summary>true: la deuda del mes es el pago mínimo; false: el saldo total.</summary>
    bool PagarMinimo);
