using Back.Data;
using Back.Interfaces;
using Back.Models;
using Microsoft.EntityFrameworkCore;

namespace Back.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly AppDbContext _context;

        public UsuarioRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<Usuario>> GetAllAsync() =>
            _context.Usuarios.AsNoTracking().OrderBy(u => u.Nombre).ToListAsync();

        public Task<Usuario?> GetByIdAsync(int id) =>
            _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

        public Task<Usuario?> GetByCorreoAsync(string correo) =>
            _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);

        public Task<bool> AnyAsync() => _context.Usuarios.AnyAsync();

        public Task<bool> TieneFacturasAsync(int id) =>
            _context.Facturas.AnyAsync(f => f.UsuarioId == id);

        public async Task AddAsync(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
        }

        public Task UpdateAsync(Usuario usuario) => _context.SaveChangesAsync();

        public async Task DeleteAsync(Usuario usuario)
        {
            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
        }
    }
}
