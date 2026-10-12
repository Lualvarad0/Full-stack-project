using Back.Seguridad;
using System.ComponentModel.DataAnnotations;

namespace Back.Dtos
{
    public record ClienteRequest(
        [TextoPlano, Required, StringLength(20, MinimumLength = 10)] string Identificacion,
        [TextoPlano, Required, StringLength(150)] string Nombre,
        [TextoPlano, StringLength(20)] string? Telefono,
        [TextoPlano, EmailAddress, StringLength(150)] string? Correo);

    public record ProductoRequest(
        [TextoPlano, Required, StringLength(150)] string Nombre,
        [TextoPlano, StringLength(2000)] string? Descripcion,
        [Range(0, 9_999_999)] decimal Precio,
        [Range(0, int.MaxValue)] int Stock);
}
