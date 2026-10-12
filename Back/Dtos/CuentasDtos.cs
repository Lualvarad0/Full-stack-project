using System.ComponentModel.DataAnnotations;
using Back.Models;
using Back.Seguridad;

namespace Back.Dtos;

public record CuentaPersonalDto(int Id, string Nombre, CategoriaCuenta Categoria, decimal Monto, int? DiaVencimiento, DateOnly? PagadaEl)
{
    public static CuentaPersonalDto From(CuentaPersonal c) => new(c.Id, c.Nombre, c.Categoria, c.Monto, c.DiaVencimiento, c.PagadaEl);
}

/// <summary>Cuentas del mes con el ingreso declarado y cuánto queda.</summary>
public record MesCuentasDto(int Anio, int Mes, decimal? Ingreso, decimal Total, decimal Pagado, decimal Pendiente, decimal? Disponible,
    List<CuentaPersonalDto> Cuentas);

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
