using System.ComponentModel.DataAnnotations;
using Back.Helpers;
using Back.Dtos;
using Back.Models;
using Back.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly ProductoService _productoService;

        public ProductosController(ProductoService productoService)
        {
            _productoService = productoService;
        }

        private int UsuarioId => JwtHelper.GetContribuyenteId(User);

        [HttpGet]
        public async Task<ActionResult<List<Producto>>> GetProductos([FromQuery, StringLength(200)] string? q) =>
            Ok(await _productoService.ObtenerProductosAsync(UsuarioId, q));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Producto>> GetProducto(int id) =>
            Ok(await _productoService.ObtenerProductoPorIdAsync(UsuarioId, id));

        [HttpPost]
        public async Task<ActionResult<Producto>> CreateProducto(ProductoRequest request)
        {
            var producto = await _productoService.CrearProductoAsync(UsuarioId, request);
            return CreatedAtAction(nameof(GetProducto), new { id = producto.ProductoId }, producto);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<Producto>> UpdateProducto(int id, ProductoRequest request) =>
            Ok(await _productoService.ActualizarProductoAsync(UsuarioId, id, request));

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProducto(int id)
        {
            await _productoService.EliminarProductoAsync(UsuarioId, id);
            return NoContent();
        }
    }
}
