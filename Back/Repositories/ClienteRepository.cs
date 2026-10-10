using Back.Data;
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

        public Task<List<Cliente>> GetAllAsync(string? busqueda)
        {
            var query = _context.Clientes.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var patron = $"%{busqueda.Trim()}%";
                query = query.Where(c => EF.Functions.ILike(c.Nombre, patron) || EF.Functions.ILike(c.Identificacion, patron));
            }
            return query.OrderBy(c => c.Nombre).ToListAsync();
        }

        public Task<Cliente?> GetByIdAsync(int id) =>
            _context.Clientes.FirstOrDefaultAsync(c => c.ClienteId == id);

        public Task<bool> ExisteIdentificacionAsync(string identificacion, int? excluirId = null) =>
            _context.Clientes.AnyAsync(c => c.Identificacion == identificacion && c.ClienteId != excluirId);

        public Task<bool> TieneFacturasAsync(int id) =>
            _context.Facturas.AnyAsync(f => f.ClienteId == id);

        public async Task AddAsync(Cliente cliente)
        {
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
        }

        public Task UpdateAsync(Cliente cliente) => _context.SaveChangesAsync();

        public async Task DeleteAsync(Cliente cliente)
        {
            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
        }
    }
}
