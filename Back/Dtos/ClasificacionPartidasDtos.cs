using System.ComponentModel.DataAnnotations;
using Back.Models;
namespace Back.Dtos;

public record ClasificarPartidaRequest([Range(1, int.MaxValue)] int Id,
    [EnumDataType(typeof(FinalidadComprobante))] FinalidadComprobante Finalidad,
    [EnumDataType(typeof(CategoriaGasto))] CategoriaGasto? Categoria,
    [EnumDataType(typeof(CategoriaIngreso))] CategoriaIngreso? CategoriaIngreso,
    [EnumDataType(typeof(TratamientoIva))] TratamientoIva TratamientoIva);
public record ClasificarPartidasRequest([Required, MinLength(1), MaxLength(500)] List<ClasificarPartidaRequest> Partidas);
public record FinalidadComprobanteRequest([EnumDataType(typeof(FinalidadComprobante))] FinalidadComprobante Finalidad,
    [EnumDataType(typeof(TratamientoIva))] TratamientoIva TratamientoIva);
