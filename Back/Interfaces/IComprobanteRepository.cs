using Back.Models;

namespace Back.Interfaces
{
    /// <param name="Direccion">Null = ambos sentidos (gastos e ingresos).</param>
    /// <param name="Cliente">Identificación del comprador (para ingresos por cliente).</param>
    public record FiltroComprobantes(
        Direccion? Direccion,
        int? Anio = null,
        int? Mes = null,
        CategoriaGasto? Categoria = null,
        string? Ruc = null,
        string? Busqueda = null,
        string? Cliente = null);

    public interface IComprobanteRepository
    {
        Task<HashSet<string>> GetClavesExistentesAsync(int usuarioId, IEnumerable<string> claves);
        Task<List<Comprobante>> GetAllAsync(int usuarioId, FiltroComprobantes filtro);
        Task<Comprobante?> GetByIdAsync(int usuarioId, int id, bool conDetalles = false);
        Task<string?> GetXmlAsync(int usuarioId, int id);
        Task AddRangeAsync(IEnumerable<Comprobante> comprobantes);
        Task DeleteAsync(Comprobante comprobante);
        Task SaveChangesAsync();

        Task<Dictionary<string, CategoriaGasto>> GetCategoriasProveedorAsync(int usuarioId, IEnumerable<string>? rucs = null);
        Task AsignarCategoriaProveedorAsync(int usuarioId, string ruc, CategoriaGasto categoria);

        /// <summary>Marca como emitidos los comprobantes cuyo emisor es el RUC del usuario y el resto como recibidos.</summary>
        Task<(int Emitidos, int Recibidos)> ReclasificarAsync(int usuarioId, string? rucUsuario);
    }
}
