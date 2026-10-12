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

        public Task<List<Producto>> ObtenerProductosAsync(int usuarioId, string? busqueda) =>
            _productoRepository.GetAllAsync(usuarioId, busqueda);

        public async Task<Producto> ObtenerProductoPorIdAsync(int usuarioId, int id) =>
            await _productoRepository.GetByIdAsync(usuarioId, id) ?? throw BusinessException.NotFound("Producto no encontrado.");

        public async Task<Producto> CrearProductoAsync(int usuarioId, ProductoRequest request)
        {
            var producto = new Producto { UsuarioId = usuarioId };
            Mapear(request, producto);
            await _productoRepository.AddAsync(producto);
            return producto;
        }

        public async Task<Producto> ActualizarProductoAsync(int usuarioId, int id, ProductoRequest request)
        {
            var producto = await ObtenerProductoPorIdAsync(usuarioId, id);
            Mapear(request, producto);
            await _productoRepository.UpdateAsync(producto);
            return producto;
        }

        public async Task EliminarProductoAsync(int usuarioId, int id)
        {
            var producto = await ObtenerProductoPorIdAsync(usuarioId, id);
            if (await _productoRepository.TieneFacturasAsync(usuarioId, id))
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
