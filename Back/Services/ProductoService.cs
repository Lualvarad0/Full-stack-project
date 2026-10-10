using Back.Dtos;
using Back.Helpers;
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

        public Task<List<Producto>> ObtenerProductosAsync(string? busqueda) =>
            _productoRepository.GetAllAsync(busqueda);

        public async Task<Producto> ObtenerProductoPorIdAsync(int id) =>
            await _productoRepository.GetByIdAsync(id) ?? throw BusinessException.NotFound("Producto no encontrado.");

        public async Task<Producto> CrearProductoAsync(ProductoRequest request)
        {
            var producto = new Producto();
            Mapear(request, producto);
            await _productoRepository.AddAsync(producto);
            return producto;
        }

        public async Task<Producto> ActualizarProductoAsync(int id, ProductoRequest request)
        {
            var producto = await ObtenerProductoPorIdAsync(id);
            Mapear(request, producto);
            await _productoRepository.UpdateAsync(producto);
            return producto;
        }

        public async Task EliminarProductoAsync(int id)
        {
            var producto = await ObtenerProductoPorIdAsync(id);
            if (await _productoRepository.TieneFacturasAsync(id))
                throw BusinessException.Conflict("El producto aparece en facturas y no puede eliminarse.");

            await _productoRepository.DeleteAsync(producto);
        }

        private static void Mapear(ProductoRequest request, Producto producto)
        {
            producto.Nombre = request.Nombre.Trim();
            producto.Descripcion = request.Descripcion?.Trim();
            producto.Precio = request.Precio;
            producto.Stock = request.Stock;
        }
    }
}
