using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Back.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers
{
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly UsuarioService _usuarioService;

        public UsuariosController(UsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        public async Task<ActionResult<List<UsuarioDto>>> GetUsuarios() =>
            Ok(await _usuarioService.ObtenerUsuariosAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UsuarioDto>> GetUsuario(int id) =>
            Ok(await _usuarioService.ObtenerUsuarioPorIdAsync(id));

        [HttpPost]
        public async Task<ActionResult<UsuarioDto>> CreateUsuario(UsuarioRequest request)
        {
            var usuario = await _usuarioService.CrearUsuarioAsync(request);
            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, usuario);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            await _usuarioService.EliminarUsuarioAsync(id, JwtHelper.GetUsuarioId(User));
            return NoContent();
        }
    }
}
