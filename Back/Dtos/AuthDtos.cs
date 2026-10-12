using Back.Seguridad;
using System.ComponentModel.DataAnnotations;
using Back.Models;

namespace Back.Dtos
{
    public record RegisterRequest(
        [TextoPlano, Required, StringLength(100, MinimumLength = 2)] string Nombre,
        [TextoPlano, Required, StringLength(100, MinimumLength = 2)] string Apellido,
        [Required] DateOnly FechaNacimiento,
        [Required, EnumDataType(typeof(PlanSuscripcion))] PlanSuscripcion Plan,
        [TextoPlano, Required, EmailAddress, StringLength(150)] string Email,
        [Required, ClaveCuenta] string Password);

    public record LoginRequest(
        [TextoPlano, Required, EmailAddress, StringLength(150)] string Email,
        [Required, StringLength(100, MinimumLength = 8)] string Password);

    public record AuthResponse(string Token, DateTime ExpiraEn, UsuarioDto Usuario);

    public record SolicitarRecuperacionRequest([TextoPlano, Required, EmailAddress, StringLength(150)] string Email);
    public record RestablecerClaveRequest([Required, RegularExpression("^[a-fA-F0-9]{64}$")] string Token,
        [Required, ClaveCuenta] string Password, [Required, ConfirmacionClave] string Confirmacion);
    public record CambiarClaveRequest([Required, StringLength(100)] string Actual,
        [Required, ClaveCuenta] string Password, [Required, ConfirmacionClave] string Confirmacion);
    public record VerificarCorreoRequest([Required, RegularExpression("^[a-fA-F0-9]{64}$")] string Token);
    public record SeguridadCuentaDto(bool CorreoVerificado, string Correo);
    public record SesionCuentaDto(string Id, DateTime CreadaEn, DateTime ExpiraEn, string? Dispositivo, bool Actual);
}
