using Back.Data;
using Back.Interfaces;
using Back.Models;
using Microsoft.EntityFrameworkCore;

namespace Back.Repositories
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly AppDbContext _context;

        public ProductoRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<Producto>> GetAllAsync(int usuarioId, string? busqueda)
        {
            var query = _context.Productos.AsNoTracking().Where(p => p.UsuarioId == usuarioId);
            if (!string.IsNullOrWhiteSpace(busqueda))
                query = query.Where(p => EF.Functions.ILike(p.Nombre, $"%{busqueda.Trim()}%"));
            return query.OrderBy(p => p.Nombre).ToListAsync();
        }

        public Task<Producto?> GetByIdAsync(int usuarioId, int id) =>
            _context.Productos.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId && p.ProductoId == id);

        // Devuelve entidades con tracking: FacturaService descuenta stock sobre ellas.
        public Task<List<Producto>> GetByIdsAsync(int usuarioId, IEnumerable<int> ids)
        {
            var lista = ids.Distinct().ToList();
            return _context.Productos.Where(p => p.UsuarioId == usuarioId && lista.Contains(p.ProductoId)).ToListAsync();
        }

        public Task<bool> TieneFacturasAsync(int usuarioId, int id) =>
            _context.DetallesFactura.AnyAsync(d => d.Factura.UsuarioId == usuarioId && d.ProductoId == id);

        public async Task AddAsync(Producto producto)
        {
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
        }

        public Task UpdateAsync(Producto producto) => _context.SaveChangesAsync();

        public async Task DeleteAsync(Producto producto)
        {
            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
        }
    }
}
