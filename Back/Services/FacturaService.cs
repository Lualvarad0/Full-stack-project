using Back.Dtos;
using Back.Helpers;
using Back.Interfaces;
using Back.Models;

namespace Back.Services
{
    public class FacturaService
    {
        private readonly IFacturaRepository _facturaRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IProductoRepository _productoRepository;

        public FacturaService(
            IFacturaRepository facturaRepository,
            IClienteRepository clienteRepository,
            IProductoRepository productoRepository)
        {
            _facturaRepository = facturaRepository;
            _clienteRepository = clienteRepository;
            _productoRepository = productoRepository;
        }

        public async Task<List<FacturaDto>> ObtenerFacturasAsync(DateTime? desde, DateTime? hasta, int? clienteId) =>
            (await _facturaRepository.GetAllAsync(desde, hasta, clienteId)).Select(FacturaDto.From).ToList();

        public async Task<FacturaDto> ObtenerFacturaPorIdAsync(int id) =>
            FacturaDto.From(await ObtenerEntidadAsync(id));

        public async Task<FacturaDto> CrearFacturaAsync(FacturaRequest request, int usuarioId)
        {
            if (await _clienteRepository.GetByIdAsync(request.ClienteId) is null)
                throw BusinessException.NotFound("Cliente no encontrado.");

            // Agrupa líneas repetidas del mismo producto para validar el stock total pedido.
            var cantidades = request.Detalles
                .GroupBy(d => d.ProductoId)
                .ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));

            var productos = (await _productoRepository.GetByIdsAsync(cantidades.Keys)).ToDictionary(p => p.ProductoId);
            var faltantes = cantidades.Keys.Where(id => !productos.ContainsKey(id)).ToList();
            if (faltantes.Count > 0)
                throw BusinessException.NotFound($"Productos no encontrados: {string.Join(", ", faltantes)}.");

            var factura = new Factura
            {
                ClienteId = request.ClienteId,
                UsuarioId = usuarioId,
                FormaPago = request.FormaPago.Trim(),
            };

            foreach (var (productoId, cantidad) in cantidades)
            {
                var producto = productos[productoId];
                if (producto.Stock < cantidad)
                    throw new BusinessException($"Stock insuficiente para '{producto.Nombre}' (disponible: {producto.Stock}).");

                // El precio se toma del catálogo, nunca del cliente.
                producto.Stock -= cantidad;
                factura.Detalles.Add(new DetalleFactura
                {
                    ProductoId = productoId,
                    Cantidad = cantidad,
                    PrecioUnitario = producto.Precio,
                    PrecioTotal = producto.Precio * cantidad,
                });
            }
            factura.MontoTotal = factura.Detalles.Sum(d => d.PrecioTotal);

            await _facturaRepository.AddAsync(factura);
            return await ObtenerFacturaPorIdAsync(factura.FacturaId);
        }

        public async Task EliminarFacturaAsync(int id)
        {
            var factura = await ObtenerEntidadAsync(id);

            // Devuelve el stock de los productos facturados.
            foreach (var detalle in factura.Detalles)
                detalle.Producto.Stock += detalle.Cantidad;

            await _facturaRepository.DeleteAsync(factura);
        }

        private async Task<Factura> ObtenerEntidadAsync(int id) =>
            await _facturaRepository.GetByIdAsync(id) ?? throw BusinessException.NotFound("Factura no encontrada.");
    }
}
