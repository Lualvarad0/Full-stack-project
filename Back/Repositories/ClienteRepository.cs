using Back.Interfaces;
using Back.Models;
using Microsoft.EntityFrameworkCore;

namespace Back.Repositories
{

    public class ClienteRepository : IClienteRepository
    {
        private readonly AppDbContext _context;

        public ClienteRepository(AppDbContext context)
        {
            _context = context;
        }

        // Obtiene todos los clientes
        public async Task<IEnumerable<Cliente>> GetClientesAsync()
        {
            return await _context.Clientes.ToListAsync();
        }

        // Obtiene un cliente por su ID
        public async Task<Cliente> GetClienteByIdAsync(int id)
        {
            return await _context.Clientes.FindAsync(id);
        }

        // Agrega un nuevo cliente
        public async Task AddClienteAsync(Cliente cliente)
        {
            await _context.Clientes.AddAsync(cliente);
            await _context.SaveChangesAsync();
        }

        // Actualiza un cliente existente
        public async Task UpdateClienteAsync(Cliente cliente)
        {
            // Si solo quieres actualizar propiedades específicas, puedes hacer algo como:
            // var existingClient = await _context.Clientes.FindAsync(cliente.Id);
            // if (existingClient != null) { existingClient.Nombre = cliente.Nombre; ... }
            _context.Entry(cliente).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        // Elimina un cliente por su ID
        public async Task DeleteClienteAsync(int id)
        {
            var cliente = await GetClienteByIdAsync(id);
            if (cliente != null)
            {
                _context.Clientes.Remove(cliente);
                await _context.SaveChangesAsync();
            }
        }
    }

}
