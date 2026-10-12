using Back.Models;

namespace Back.Interfaces
{
    public interface IClienteRepository
    {
        Task<List<Cliente>> GetAllAsync(int usuarioId, string? busqueda);
        Task<Cliente?> GetByIdAsync(int usuarioId, int id);
        Task<bool> ExisteIdentificacionAsync(int usuarioId, string identificacion, int? excluirId = null);
        Task<bool> TieneFacturasAsync(int usuarioId, int id);
        Task AddAsync(Cliente cliente);
        Task UpdateAsync(Cliente cliente);
        Task DeleteAsync(Cliente cliente);
    }

    public interface IProductoRepository
    {
        Task<List<Producto>> GetAllAsync(int usuarioId, string? busqueda);
        Task<Producto?> GetByIdAsync(int usuarioId, int id);
        Task<List<Producto>> GetByIdsAsync(int usuarioId, IEnumerable<int> ids);
        Task<bool> TieneFacturasAsync(int usuarioId, int id);
        Task AddAsync(Producto producto);
        Task UpdateAsync(Producto producto);
        Task DeleteAsync(Producto producto);
    }

    public interface IUsuarioRepository
    {
        Task<List<Usuario>> GetAllAsync();
        Task<Usuario?> GetByIdAsync(int id);
        Task<Usuario?> GetByCorreoAsync(string correo);
        Task<bool> AnyAsync();
        Task<bool> TieneFacturasAsync(int id);
        Task AddAsync(Usuario usuario);
        Task UpdateAsync(Usuario usuario);
        Task DeleteAsync(Usuario usuario);
    }

    public interface IFacturaRepository
    {
        Task<List<Factura>> GetAllAsync(int usuarioId, DateTime? desde, DateTime? hasta, int? clienteId);
        Task<Factura?> GetByIdAsync(int usuarioId, int id);
        Task AddAsync(Factura factura);
        Task DeleteAsync(Factura factura);
    }
}
