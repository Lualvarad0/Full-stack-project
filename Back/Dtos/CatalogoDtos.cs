using System.ComponentModel.DataAnnotations;

namespace Back.Dtos
{
    public record ClienteRequest(
        [Required, StringLength(20, MinimumLength = 10)] string Identificacion,
        [Required, StringLength(150)] string Nombre,
        [StringLength(20)] string? Telefono,
        [EmailAddress, StringLength(150)] string? Correo);

    public record ProductoRequest(
        [Required, StringLength(150)] string Nombre,
        string? Descripcion,
        [Range(0, 9_999_999)] decimal Precio,
        [Range(0, int.MaxValue)] int Stock);
}
