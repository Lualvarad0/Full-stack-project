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

        public Task<List<Cliente>> GetAllAsync(int usuarioId, string? busqueda)
        {
            var query = _context.Clientes.AsNoTracking().Where(c => c.UsuarioId == usuarioId);
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var patron = $"%{busqueda.Trim()}%";
                query = query.Where(c => EF.Functions.ILike(c.Nombre, patron) || EF.Functions.ILike(c.Identificacion, patron));
            }
            return query.OrderBy(c => c.Nombre).ToListAsync();
        }

        public Task<Cliente?> GetByIdAsync(int usuarioId, int id) =>
            _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.ClienteId == id);

        public Task<bool> ExisteIdentificacionAsync(int usuarioId, string identificacion, int? excluirId = null) =>
            _context.Clientes.AnyAsync(c => c.UsuarioId == usuarioId && c.Identificacion == identificacion && c.ClienteId != excluirId);

        public Task<bool> TieneFacturasAsync(int usuarioId, int id) =>
            _context.Facturas.AnyAsync(f => f.UsuarioId == usuarioId && f.ClienteId == id);

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
