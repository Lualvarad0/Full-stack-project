using Back.Dtos;
using Back.Helpers;
using Back.Interfaces;
using Back.Models;

namespace Back.Services
{
    public class ClienteService
    {
        private readonly IClienteRepository _clienteRepository;

        public ClienteService(IClienteRepository clienteRepository)
        {
            _clienteRepository = clienteRepository;
        }

        public Task<List<Cliente>> ObtenerClientesAsync(int usuarioId, string? busqueda) =>
            _clienteRepository.GetAllAsync(usuarioId, busqueda);

        public async Task<Cliente> ObtenerClientePorIdAsync(int usuarioId, int id) =>
            await _clienteRepository.GetByIdAsync(usuarioId, id) ?? throw BusinessException.NotFound("Cliente no encontrado.");

        public async Task<Cliente> CrearClienteAsync(int usuarioId, ClienteRequest request)
        {
            if (await _clienteRepository.ExisteIdentificacionAsync(usuarioId, request.Identificacion.Trim()))
                throw BusinessException.Conflict("Ya existe un cliente con esa identificación.");

            var cliente = new Cliente { UsuarioId = usuarioId };
            Mapear(request, cliente);
            await _clienteRepository.AddAsync(cliente);
            return cliente;
        }

        public async Task<Cliente> ActualizarClienteAsync(int usuarioId, int id, ClienteRequest request)
        {
            var cliente = await ObtenerClientePorIdAsync(usuarioId, id);
            if (await _clienteRepository.ExisteIdentificacionAsync(usuarioId, request.Identificacion.Trim(), id))
                throw BusinessException.Conflict("Ya existe un cliente con esa identificación.");

            Mapear(request, cliente);
            await _clienteRepository.UpdateAsync(cliente);
            return cliente;
        }

        public async Task EliminarClienteAsync(int usuarioId, int id)
        {
            var cliente = await ObtenerClientePorIdAsync(usuarioId, id);
            if (await _clienteRepository.TieneFacturasAsync(usuarioId, id))
                throw BusinessException.Conflict("El cliente tiene facturas registradas y no puede eliminarse.");

            await _clienteRepository.DeleteAsync(cliente);
        }

        private static void Mapear(ClienteRequest request, Cliente cliente)
        {
            cliente.Identificacion = request.Identificacion.Trim();
            cliente.Nombre = request.Nombre.Trim();
            cliente.Telefono = request.Telefono?.Trim();
            cliente.Correo = request.Correo?.Trim().ToLowerInvariant();
        }
    }
}
