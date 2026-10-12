using System.ComponentModel.DataAnnotations;
using Back.Models;
using Back.Seguridad;
namespace Back.Dtos;

public record SolicitarVinculoRequest([Required, EmailAddress, TextoPlano, StringLength(150)] string Correo,
    [EnumDataType(typeof(PermisoContador))] PermisoContador Permiso);
public record VinculoContadorDto(int Id, int PropietarioId, int ContadorId, string NombrePropietario, string? Ruc,
    string NombreContador, string CorreoContador, PermisoContador Permiso, EstadoVinculo Estado,
    DateTime SolicitadoEn, DateTime ExpiraEn, bool PuedeResponder);
public record ContribuyenteAccesibleDto(int Id, string Nombre, string? Ruc, PermisoContador Permiso, bool Propio);
