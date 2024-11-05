using Back.Interfaces;
using Back.Models;

namespace Back.Services
{
    public class ProductoService
    {
        private readonly IProductoRepository _productoRepository;

        public ProductoService(IProductoRepository productoRepository)
        {
            _productoRepository = productoRepository;
        }

        public async Task<IEnumerable<Producto>> ObtenerProductosAsync()
        {
            return await _productoRepository.GetAllAsync();
        }

        public async Task<Producto> ObtenerProductoPorIdAsync(int id)
        {
            return await _productoRepository.GetByIdAsync(id);
        }

        public async Task CrearProductoAsync(Producto producto)
        {
            await _productoRepository.AddAsync(producto);
        }

        public async Task ActualizarProductoAsync(Producto producto)
        {
            await _productoRepository.UpdateAsync(producto);
        }

        public async Task EliminarProductoAsync(int id)
        {
            await _productoRepository.DeleteAsync(id);
        }
    }
}
