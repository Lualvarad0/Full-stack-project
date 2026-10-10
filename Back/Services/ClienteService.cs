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

        public Task<List<Cliente>> ObtenerClientesAsync(string? busqueda) =>
            _clienteRepository.GetAllAsync(busqueda);

        public async Task<Cliente> ObtenerClientePorIdAsync(int id) =>
            await _clienteRepository.GetByIdAsync(id) ?? throw BusinessException.NotFound("Cliente no encontrado.");

        public async Task<Cliente> CrearClienteAsync(ClienteRequest request)
        {
            if (await _clienteRepository.ExisteIdentificacionAsync(request.Identificacion.Trim()))
                throw BusinessException.Conflict("Ya existe un cliente con esa identificación.");

            var cliente = new Cliente();
            Mapear(request, cliente);
            await _clienteRepository.AddAsync(cliente);
            return cliente;
        }

        public async Task<Cliente> ActualizarClienteAsync(int id, ClienteRequest request)
        {
            var cliente = await ObtenerClientePorIdAsync(id);
            if (await _clienteRepository.ExisteIdentificacionAsync(request.Identificacion.Trim(), id))
                throw BusinessException.Conflict("Ya existe un cliente con esa identificación.");

            Mapear(request, cliente);
            await _clienteRepository.UpdateAsync(cliente);
            return cliente;
        }

        public async Task EliminarClienteAsync(int id)
        {
            var cliente = await ObtenerClientePorIdAsync(id);
            if (await _clienteRepository.TieneFacturasAsync(id))
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
