using Back.Data;
using Back.Interfaces;
using Back.Models;
using Microsoft.EntityFrameworkCore;

namespace Back.Repositories
{
    public class BuzonRepository : IBuzonRepository
    {
        private readonly AppDbContext _context;

        public BuzonRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<BuzonCorreo?> GetByUsuarioAsync(int usuarioId) =>
            _context.BuzonesCorreo.FirstOrDefaultAsync(b => b.UsuarioId == usuarioId);

        public Task<BuzonCorreo?> GetByCodigoAsync(string codigo) =>
            _context.BuzonesCorreo.FirstOrDefaultAsync(b => b.Codigo == codigo);

        public Task<bool> CodigoExisteAsync(string codigo) =>
            _context.BuzonesCorreo.AnyAsync(b => b.Codigo == codigo);

        public async Task AddAsync(BuzonCorreo buzon)
        {
            _context.BuzonesCorreo.Add(buzon);
            await _context.SaveChangesAsync();
        }

        public async Task AddCorreoAsync(CorreoRecibido correo)
        {
            _context.CorreosRecibidos.Add(correo);
            await _context.SaveChangesAsync();
        }

        public Task<List<CorreoRecibido>> GetCorreosRecientesAsync(int usuarioId, int cantidad) =>
            _context.CorreosRecibidos.AsNoTracking()
                .Where(c => c.UsuarioId == usuarioId)
                .OrderByDescending(c => c.RecibidoEn)
                .Take(cantidad)
                .ToListAsync();

        public Task<List<CorreoRecibido>> GetPendientesAsync(int cantidad) =>
            _context.CorreosRecibidos
                .Where(c => c.Estado == EstadoCorreo.Pendiente)
                .OrderBy(c => c.RecibidoEn)
                .Take(cantidad)
                .ToListAsync();

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
