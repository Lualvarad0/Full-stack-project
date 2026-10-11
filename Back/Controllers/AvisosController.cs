using Back.Avisos;
using Back.Dtos;
using Back.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers
{
    /// <summary>Actividad de seguridad de la cuenta: ingresos y cambios de configuración.</summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AvisosController : ControllerBase
    {
        private readonly AvisoService _avisoService;

        public AvisosController(AvisoService avisoService)
        {
            _avisoService = avisoService;
        }

        [HttpGet]
        public async Task<ActionResult<List<AvisoCuentaDto>>> GetAvisos() =>
            Ok(await _avisoService.ObtenerRecientesAsync(JwtHelper.GetUsuarioId(User)));
    }
}
