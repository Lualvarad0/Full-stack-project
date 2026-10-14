using Back.Seguridad;
using System.ComponentModel.DataAnnotations;
using Back.Models;

namespace Back.Dtos
{
    /// <summary>Nombre: nombre y apellido juntos, para mostrar.</summary>
    /// <summary>FaltaFechaNacimiento: cuenta antigua que debe registrar su fecha de nacimiento antes de usar la app.</summary>
    public record UsuarioDto(int Id, string Nombre, string Email, string Rol, PlanSuscripcion Plan, PaginaInicio PaginaInicio = PaginaInicio.Resumen,
        MesAlEntrar MesAlEntrar = MesAlEntrar.Actual, bool FaltaFechaNacimiento = false)
    {
        public static UsuarioDto From(Usuario u) => new(u.Id, $"{u.Nombre} {u.Apellido}".Trim(), u.Correo, u.Rol, u.Plan, u.PaginaInicio, u.MesAlEntrar,
            u.FechaNacimiento is null);
    }

    public record UsuarioRequest(
        [TextoPlano, Required, StringLength(100, MinimumLength = 2)] string Nombre,
        [TextoPlano, Required, EmailAddress, StringLength(150)] string Email,
        [Required, StringLength(100, MinimumLength = 8)] string Password,
        [Required, RegularExpression("^(admin|user)$")] string Rol,
        [Required] DateOnly FechaNacimiento);
}
