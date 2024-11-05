using Back.Models;

namespace Back.Interfaces
{
    public interface IFacturaRepository
    {
        Task<IEnumerable<Factura>> GetFacturasAsync();
        Task<Factura> GetFacturaByIdAsync(int id);
        Task AddFacturaAsync(Factura factura);
        Task UpdateFacturaAsync(Factura factura);
        Task DeleteFacturaAsync(int id);
    }

}
