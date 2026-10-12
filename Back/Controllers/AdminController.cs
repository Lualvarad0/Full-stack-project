using System.ComponentModel.DataAnnotations;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Back.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers
{
    /// <summary>Panel de superadministrador (solo el rol admin).</summary>
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _service;

        public AdminController(AdminService service)
        {
            _service = service;
        }

        private int AdminId => JwtHelper.GetUsuarioId(User);

        [HttpGet("metricas")]
        public async Task<ActionResult<MetricasAdminDto>> GetMetricas() => Ok(await _service.MetricasAsync());

        [HttpGet("usuarios")]
        public async Task<ActionResult<List<UsuarioAdminDto>>> GetUsuarios([FromQuery, StringLength(200)] string? q, [FromQuery] PlanSuscripcion? plan) =>
            Ok(await _service.UsuariosAsync(q, plan));

        [HttpGet("usuarios/{id:int}/actividad")]
        public async Task<ActionResult<List<AvisoCuentaDto>>> GetActividad(int id) => Ok(await _service.ActividadAsync(id));

        [HttpPut("usuarios/{id:int}/plan")]
        public async Task<IActionResult> PutPlan(int id, CambiarPlanAdminRequest request)
        {
            await _service.CambiarPlanAsync(AdminId, id, request.Plan);
            return NoContent();
        }

        [HttpPut("usuarios/{id:int}/estado")]
        public async Task<IActionResult> PutEstado(int id, CambiarEstadoAdminRequest request)
        {
            await _service.CambiarEstadoAsync(AdminId, id, request.Activo);
            return NoContent();
        }

        [HttpDelete("usuarios/{id:int}/suscripcion")]
        public async Task<IActionResult> DeleteSuscripcion(int id, CancellationToken ct)
        {
            await _service.CancelarSuscripcionAsync(id, ct);
            return NoContent();
        }

        [HttpDelete("usuarios/{id:int}")]
        public async Task<IActionResult> DeleteUsuario(int id, CancellationToken ct)
        {
            await _service.EliminarUsuarioAsync(AdminId, id, ct);
            return NoContent();
        }

        [HttpGet("pagos")]
        public async Task<ActionResult<List<PagoAdminDto>>> GetPagos() => Ok(await _service.PagosAsync());

        [HttpGet("sistema")]
        public async Task<ActionResult<EstadoSistemaDto>> GetSistema() => Ok(await _service.SistemaAsync());
    }
}
