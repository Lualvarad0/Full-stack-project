using Back.Models;

namespace Back.Interfaces
{
    public interface IBuzonRepository
    {
        Task<BuzonCorreo?> GetByUsuarioAsync(int usuarioId);
        Task<BuzonCorreo?> GetByCodigoAsync(string codigo);
        Task<bool> CodigoExisteAsync(string codigo);
        Task AddAsync(BuzonCorreo buzon);
        Task AddCorreoAsync(CorreoRecibido correo);
        Task<List<CorreoRecibido>> GetCorreosRecientesAsync(int usuarioId, int cantidad);
        Task<List<CorreoRecibido>> GetPendientesAsync(int cantidad);
        Task SaveChangesAsync();
    }
}
