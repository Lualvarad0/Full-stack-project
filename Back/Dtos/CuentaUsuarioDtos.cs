using System.ComponentModel.DataAnnotations;
using Back.Models;
using Back.Seguridad;

namespace Back.Dtos;

/// <summary>Datos personales del usuario (Configuración → Mi perfil).</summary>
public record DatosPersonalesDto(string Nombre, string Apellido, DateOnly? FechaNacimiento, string Correo, bool CorreoVerificado,
    string? Telefono, string? Ciudad, string? Direccion, string? Ocupacion, DateTime MiembroDesde)
{
    public static DatosPersonalesDto From(Usuario u) => new(u.Nombre, u.Apellido, u.FechaNacimiento, u.Correo, u.CorreoVerificado,
        u.Telefono, u.Ciudad, u.Direccion, u.Ocupacion, u.FechaCreacion);
}

public record DatosPersonalesRequest(
    [TextoPlano, Required, StringLength(100, MinimumLength = 2)] string Nombre,
    [TextoPlano, Required, StringLength(100, MinimumLength = 2)] string Apellido,
    [Required] DateOnly FechaNacimiento,
    [RegularExpression(@"^\+?[0-9\s-]{7,20}$", ErrorMessage = "El teléfono solo admite números, espacios, guiones y + al inicio.")] string? Telefono,
    [TextoPlano, StringLength(100)] string? Ciudad,
    [TextoPlano, StringLength(300)] string? Direccion,
    [TextoPlano, StringLength(120)] string? Ocupacion);

/// <summary>Tras guardar: los datos y el usuario de la sesión (el nombre puede haber cambiado).</summary>
public record DatosPersonalesActualizadosDto(DatosPersonalesDto Datos, UsuarioDto Usuario);

/// <summary>Configuración del sistema guardada en el servidor (vale en todos tus dispositivos).</summary>
public record ConfiguracionSistemaDto(PaginaInicio PaginaInicio, MesAlEntrar MesAlEntrar, bool AvisarIngresosPorCorreo)
{
    public static ConfiguracionSistemaDto From(Usuario u) => new(u.PaginaInicio, u.MesAlEntrar, u.AvisarIngresosPorCorreo);
}

public record ConfiguracionSistemaRequest(
    [EnumDataType(typeof(PaginaInicio))] PaginaInicio PaginaInicio,
    [EnumDataType(typeof(MesAlEntrar))] MesAlEntrar MesAlEntrar,
    bool AvisarIngresosPorCorreo);
