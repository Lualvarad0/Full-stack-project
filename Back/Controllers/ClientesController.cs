using System.ComponentModel.DataAnnotations;
using Back.Helpers;
using Back.Dtos;
using Back.Models;
using Back.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly ClienteService _clienteService;

        public ClientesController(ClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        // GET api/clientes?q=texto  (busca por nombre o identificación)
        private int UsuarioId => JwtHelper.GetContribuyenteId(User);

        [HttpGet]
        public async Task<ActionResult<List<Cliente>>> GetClientes([FromQuery, StringLength(200)] string? q) =>
            Ok(await _clienteService.ObtenerClientesAsync(UsuarioId, q));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Cliente>> GetCliente(int id) =>
            Ok(await _clienteService.ObtenerClientePorIdAsync(UsuarioId, id));

        [HttpPost]
        public async Task<ActionResult<Cliente>> CreateCliente(ClienteRequest request)
        {
            var cliente = await _clienteService.CrearClienteAsync(UsuarioId, request);
            return CreatedAtAction(nameof(GetCliente), new { id = cliente.ClienteId }, cliente);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<Cliente>> UpdateCliente(int id, ClienteRequest request) =>
            Ok(await _clienteService.ActualizarClienteAsync(UsuarioId, id, request));

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            await _clienteService.EliminarClienteAsync(UsuarioId, id);
            return NoContent();
        }
    }
}
