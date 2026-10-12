using Back.Dtos;
using Back.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Back.Seguridad;

namespace Back.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request) =>
            Ok(await _authService.RegistrarAsync(request));

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request) =>
            Ok(await _authService.LoginAsync(request));

        [HttpPost("recuperar-clave")]
        public async Task<IActionResult> Recuperar(SolicitarRecuperacionRequest request, [FromServices] SeguridadCuentaService seguridad, CancellationToken ct)
        {
            await seguridad.SolicitarRecuperacionAsync(request.Email, ct);
            return Ok(new { mensaje = SeguridadCuentaService.RespuestaRecuperacion });
        }
        [HttpPost("restablecer-clave")]
        public async Task<IActionResult> Restablecer(RestablecerClaveRequest request, [FromServices] SeguridadCuentaService seguridad, CancellationToken ct)
        { await seguridad.RestablecerAsync(request, ct); return NoContent(); }
        [HttpPost("verificar-correo")]
        public async Task<IActionResult> Verificar(VerificarCorreoRequest request, [FromServices] SeguridadCuentaService seguridad, CancellationToken ct)
        { await seguridad.VerificarCorreoAsync(request.Token, ct); return NoContent(); }
    }
}
