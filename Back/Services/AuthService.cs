using Back.Avisos;
using Back.Dtos;
using Back.Helpers;
using Back.Interfaces;
using Back.Models;
using Microsoft.Extensions.Options;

namespace Back.Services
{
    public class AuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly AvisoService _avisos;
        private readonly JwtOptions _jwtOptions;

        public AuthService(IUsuarioRepository usuarioRepository, AvisoService avisos, IOptions<JwtOptions> jwtOptions)
        {
            _usuarioRepository = usuarioRepository;
            _avisos = avisos;
            _jwtOptions = jwtOptions.Value;
        }

        public async Task<AuthResponse> RegistrarAsync(RegisterRequest request)
        {
            var correo = request.Email.Trim().ToLowerInvariant();
            if (await _usuarioRepository.GetByCorreoAsync(correo) is not null)
                throw BusinessException.Conflict("Ya existe una cuenta con ese correo.");

            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
            if (request.FechaNacimiento > hoy.AddYears(-18))
                throw new BusinessException("Debes ser mayor de edad para crear una cuenta.");
            if (request.FechaNacimiento < new DateOnly(1900, 1, 1))
                throw new BusinessException("Revisa tu fecha de nacimiento.");

            var usuario = new Usuario
            {
                Nombre = request.Nombre.Trim(),
                Apellido = request.Apellido.Trim(),
                FechaNacimiento = request.FechaNacimiento,
                // Los planes de pago se activan al pagar (SuscripcionController); hasta entonces es Personal.
                Plan = PlanSuscripcion.Personal,
                Correo = correo,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                // El primer usuario del sistema queda como administrador.
                Rol = await _usuarioRepository.AnyAsync() ? Roles.User : Roles.Admin,
            };
            await _usuarioRepository.AddAsync(usuario);
            await _avisos.NotificarAsync(usuario.Id, TipoAviso.Registro, "Te damos la bienvenida a Nudo",
                "Creaste tu cuenta. Te avisaremos por este medio cada vez que alguien ingrese o cambie la configuración.");

            return CrearRespuesta(usuario);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var usuario = await _usuarioRepository.GetByCorreoAsync(request.Email.Trim().ToLowerInvariant());
            if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
                throw new BusinessException("Correo o contraseña incorrectos.", StatusCodes.Status401Unauthorized);
            if (!usuario.Activo)
                throw new BusinessException("Tu cuenta está suspendida. Escríbenos para revisarla.", StatusCodes.Status403Forbidden);

            usuario.UltimoIngresoEn = DateTime.UtcNow;
            await _usuarioRepository.UpdateAsync(usuario);

            await _avisos.NotificarAsync(usuario.Id, TipoAviso.Ingreso, "Nuevo ingreso a tu cuenta",
                "Se inició sesión en tu cuenta de Nudo.");
            return CrearRespuesta(usuario);
        }

        private AuthResponse CrearRespuesta(Usuario usuario)
        {
            var (token, expiraEn) = JwtHelper.GenerateToken(_jwtOptions, usuario);
            return new AuthResponse(token, expiraEn, UsuarioDto.From(usuario));
        }
    }
}
