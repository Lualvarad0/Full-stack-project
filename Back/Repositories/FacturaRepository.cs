using Back.Data;
using Back.Interfaces;
using Back.Models;
using Microsoft.EntityFrameworkCore;

namespace Back.Repositories
{
    public class FacturaRepository : IFacturaRepository
    {
        private readonly AppDbContext _context;

        public FacturaRepository(AppDbContext context)
        {
            _context = context;
        }

        private IQueryable<Factura> ConDetalles() => _context.Facturas
            .Include(f => f.Cliente)
            .Include(f => f.Usuario)
            .Include(f => f.Detalles).ThenInclude(d => d.Producto);

        public Task<List<Factura>> GetAllAsync(int usuarioId, DateTime? desde, DateTime? hasta, int? clienteId)
        {
            var query = Propias(usuarioId).AsNoTracking();
            if (desde.HasValue) query = query.Where(f => f.Fecha >= desde.Value);
            if (hasta.HasValue) query = query.Where(f => f.Fecha < hasta.Value);
            if (clienteId.HasValue) query = query.Where(f => f.ClienteId == clienteId.Value);
            return query.OrderByDescending(f => f.Fecha).ToListAsync();
        }

        public Task<Factura?> GetByIdAsync(int usuarioId, int id) =>
            Propias(usuarioId).FirstOrDefaultAsync(f => f.FacturaId == id);

        // Las facturas antiguas con referencias compartidas o sin dueño quedan fuera de la API.
        private IQueryable<Factura> Propias(int usuarioId) => ConDetalles().Where(f =>
            f.UsuarioId == usuarioId && f.Cliente.UsuarioId == usuarioId &&
            f.Detalles.All(d => d.Producto.UsuarioId == usuarioId));

        // Un solo SaveChanges: la factura y el stock de los productos (cargados con tracking
        // en el mismo DbContext) se guardan en la misma transacción.
        public async Task AddAsync(Factura factura)
        {
            _context.Facturas.Add(factura);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Factura factura)
        {
            _context.Facturas.Remove(factura);
            await _context.SaveChangesAsync();
        }
    }
}
