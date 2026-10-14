using System.ComponentModel.DataAnnotations;
using Back.Models;
using Back.Seguridad;

namespace Back.Dtos;

/// <summary>Trasladada: quedó pendiente al cierre y pasó al mes siguiente. DelMesAnterior: deuda que viene del mes anterior.</summary>
public record CuentaPersonalDto(int Id, string Nombre, CategoriaCuenta Categoria, decimal Monto, int? DiaVencimiento, DateOnly? PagadaEl,
    RespaldoPago? Respaldo = null, bool Trasladada = false, bool DelMesAnterior = false, int? TarjetaCreditoId = null)
{
    public static CuentaPersonalDto From(CuentaPersonal c) => new(c.Id, c.Nombre, c.Categoria, c.Monto, c.DiaVencimiento, c.PagadaEl,
        c.Respaldo, c.TrasladadaACuentaId is not null, c.TrasladadaDesdeId is not null, c.TarjetaCreditoId);
}

/// <summary>Cuentas del mes con el ingreso declarado y cuánto queda. Trasladado: lo que quedó pendiente y pasó al mes siguiente.</summary>
public record MesCuentasDto(int Anio, int Mes, decimal? Ingreso, decimal Total, decimal Pagado, decimal Pendiente, decimal? Disponible,
    List<CuentaPersonalDto> Cuentas, decimal Trasladado = 0);

/// <summary>Factura o correo que parece respaldar el pago de una cuenta pendiente al cierre.</summary>
public record SugerenciaPagoDto(RespaldoPago Tipo, DateOnly Fecha, string Descripcion, decimal? Monto, int? ComprobanteId, int? CorreoId);

public record CuentaCierreDto(CuentaPersonalDto Cuenta, SugerenciaPagoDto? Sugerencia);

/// <summary>Cuentas que quedaron pendientes en un mes que ya terminó (planes de pago).</summary>
public record CierreMesDto(int Anio, int Mes, List<CuentaCierreDto> Pendientes);

public enum AccionCierre
{
    /// <summary>Ya la pagó: con la fecha de la factura o del correo, o el día que indique.</summary>
    Pagada,
    /// <summary>La paga el mes siguiente: pasa como deuda a ese mes.</summary>
    Trasladar,
}

public record DecisionCierreRequest(
    [Range(1, int.MaxValue)] int CuentaId,
    [EnumDataType(typeof(AccionCierre))] AccionCierre Accion,
    DateOnly? Fecha,
    int? ComprobanteId,
    int? CorreoId);

public record CierreMesRequest(
    [Range(2000, 2100)] int Anio,
    [Range(1, 12)] int Mes,
    [Required, MinLength(1), MaxLength(CuentasLimites.MaxCuentasPorMes)] List<DecisionCierreRequest> Decisiones);

public record CierreMesResultadoDto(int Pagadas, int Trasladadas);

public static class CuentasLimites
{
    public const int MaxCuentasPorMes = 100;
}

public record CuentaPersonalRequest(
    [Range(2000, 2100)] int Anio,
    [Range(1, 12)] int Mes,
    [Required, TextoPlano, StringLength(120, MinimumLength = 2)] string Nombre,
    [EnumDataType(typeof(CategoriaCuenta))] CategoriaCuenta Categoria,
    [Range(0.01, 999999999)] decimal Monto,
    [Range(1, 31)] int? DiaVencimiento);

public record PagoCuentaRequest(DateOnly Fecha);

public record IngresoMensualRequest([Range(2000, 2100)] int Anio, [Range(1, 12)] int Mes, [Range(0, 999999999)] decimal Monto);

public record CopiarMesRequest([Range(2000, 2100)] int Anio, [Range(1, 12)] int Mes);
