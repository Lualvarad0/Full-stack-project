using System.ComponentModel.DataAnnotations;
using Back.Models;

namespace Back.Dtos
{
    public record UsuarioDto(int Id, string Nombre, string Email, string Rol)
    {
        public static UsuarioDto From(Usuario u) => new(u.Id, u.Nombre, u.Correo, u.Rol);
    }

    public record UsuarioRequest(
        [Required, StringLength(100, MinimumLength = 2)] string Nombre,
        [Required, EmailAddress] string Email,
        [Required, StringLength(100, MinimumLength = 8)] string Password,
        [Required, RegularExpression("^(admin|user)$")] string Rol);
}
