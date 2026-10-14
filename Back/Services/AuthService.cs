using Back.Avisos;
using Back.Dtos;
using Back.Helpers;
using Back.Interfaces;
using Back.Models;
using Microsoft.Extensions.Options;
using Back.Data;
using System.IdentityModel.Tokens.Jwt;

namespace Back.Services
{
    public class AuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly AvisoService _avisos;
        private readonly JwtOptions _jwtOptions;
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public AuthService(IUsuarioRepository usuarioRepository, AvisoService avisos, IOptions<JwtOptions> jwtOptions, AppDbContext db, IHttpContextAccessor http)
        {
            _usuarioRepository = usuarioRepository;
            _avisos = avisos;
            _jwtOptions = jwtOptions.Value;
            _db = db; _http = http;
        }

        public async Task<AuthResponse> RegistrarAsync(RegisterRequest request)
        {
            var correo = request.Email.Trim().ToLowerInvariant();
            if (await _usuarioRepository.GetByCorreoAsync(correo) is not null)
                throw BusinessException.Conflict("Ya existe una cuenta con ese correo.");

            EdadMinima.Validar(request.FechaNacimiento);

            var usuario = new Usuario
            {
                Nombre = request.Nombre.Trim(),
                Apellido = request.Apellido.Trim(),
                FechaNacimiento = request.FechaNacimiento,
                // Los planes de pago se activan al pagar (SuscripcionController); hasta entonces es Personal.
                Plan = PlanSuscripcion.Personal,
                Correo = correo,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                // El registro público jamás concede privilegios administrativos.
                Rol = Roles.User,
            };
            await _usuarioRepository.AddAsync(usuario);
            await _avisos.NotificarAsync(usuario.Id, TipoAviso.Registro, "Te damos la bienvenida a Nudo",
                "Creaste tu cuenta. Te avisaremos por este medio cada vez que alguien ingrese o cambie la configuración.");

            return await CrearRespuestaAsync(usuario);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var usuario = await _usuarioRepository.GetByCorreoAsync(request.Email.Trim().ToLowerInvariant());
            if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
                throw new BusinessException("Correo o contraseña incorrectos.", StatusCodes.Status401Unauthorized);
            if (!usuario.Activo)
                throw new BusinessException("Tu cuenta está suspendida. Escríbenos para revisarla.", StatusCodes.Status403Forbidden);
            // Cuentas antiguas sin fecha entran, pero la app les pide completarla antes de seguir (UsuarioDto.FaltaFechaNacimiento).
            if (usuario.FechaNacimiento is { } nacimiento && !EdadMinima.EsMayorDeEdad(nacimiento))
                throw new BusinessException(EdadMinima.Mensaje, StatusCodes.Status403Forbidden);

            usuario.UltimoIngresoEn = DateTime.UtcNow;
            await _usuarioRepository.UpdateAsync(usuario);

            await _avisos.NotificarAsync(usuario.Id, TipoAviso.Ingreso, "Nuevo ingreso a tu cuenta",
                "Se inició sesión en tu cuenta de Nudo.");
            return await CrearRespuestaAsync(usuario);
        }

        private async Task<AuthResponse> CrearRespuestaAsync(Usuario usuario)
        {
            var (token, expiraEn) = JwtHelper.GenerateToken(_jwtOptions, usuario);
            var jti = new JwtSecurityTokenHandler().ReadJwtToken(token).Id;
            _db.Set<SesionCuenta>().Add(new() { Id = jti, UsuarioId = usuario.Id, ExpiraEn = expiraEn,
                Dispositivo = AvisoService.DescribirDispositivo(_http.HttpContext?.Request.Headers.UserAgent.ToString()) });
            await _db.SaveChangesAsync();
            return new AuthResponse(token, expiraEn, UsuarioDto.From(usuario));
        }
    }
}
