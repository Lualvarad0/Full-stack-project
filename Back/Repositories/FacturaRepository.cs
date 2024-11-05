using Back.Interfaces;
using Back.Models;

namespace Back.Repositories
{
    public class FacturaRepository : IFacturaRepository
    {
        private readonly ApplicationDbContext _context;

        public FacturaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Factura>> GetAllAsync()
        {
            return await _context.Facturas.Include(f => f.Cliente)
                                           .Include(f => f.Vendedor)
                                           .ToListAsync();
        }

        public async Task<Factura> GetByIdAsync(int id)
        {
            return await _context.Facturas.Include(f => f.Cliente)
                                           .Include(f => f.Vendedor)
                                           .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task AddAsync(Factura factura)
        {
            await _context.Facturas.AddAsync(factura);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Factura factura)
        {
            _context.Facturas.Update(factura);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var factura = await GetByIdAsync(id);
            if (factura != null)
            {
                _context.Facturas.Remove(factura);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Factura>> BuscarPorCriteriosAsync(DateTime? fecha, decimal? monto, int? numero)
        {
            var query = _context.Facturas.AsQueryable();

            if (fecha.HasValue)
                query = query.Where(f => f.Fecha == fecha.Value);
            if (monto.HasValue)
                query = query.Where(f => f.Total == monto.Value);
            if (numero.HasValue)
                query = query.Where(f => f.Id == numero.Value);

            return await query.ToListAsync();
        }
    }
}
