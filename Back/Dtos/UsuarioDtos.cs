using Back.Seguridad;
using System.ComponentModel.DataAnnotations;
using Back.Models;

namespace Back.Dtos
{
    /// <summary>Nombre: nombre y apellido juntos, para mostrar.</summary>
    public record UsuarioDto(int Id, string Nombre, string Email, string Rol, PlanSuscripcion Plan)
    {
        public static UsuarioDto From(Usuario u) => new(u.Id, $"{u.Nombre} {u.Apellido}".Trim(), u.Correo, u.Rol, u.Plan);
    }

    public record UsuarioRequest(
        [TextoPlano, Required, StringLength(100, MinimumLength = 2)] string Nombre,
        [TextoPlano, Required, EmailAddress, StringLength(150)] string Email,
        [Required, StringLength(100, MinimumLength = 8)] string Password,
        [Required, RegularExpression("^(admin|user)$")] string Rol);
}
