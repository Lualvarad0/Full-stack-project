using Back.Dtos;
using Back.Helpers;
using Back.Interfaces;
using Back.Models;
using Back.Contabilidad;
using Back.Data;

namespace Back.Services
{
    public class FacturaService
    {
        private readonly IFacturaRepository _facturaRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IProductoRepository _productoRepository;
        private readonly ControlPeriodoService _periodos;
        private readonly AppDbContext _db;

        public FacturaService(
            IFacturaRepository facturaRepository,
            IClienteRepository clienteRepository,
            IProductoRepository productoRepository, ControlPeriodoService periodos, AppDbContext db)
        {
            _facturaRepository = facturaRepository;
            _clienteRepository = clienteRepository;
            _productoRepository = productoRepository;
            _periodos = periodos;
            _db = db;
        }

        public async Task<List<FacturaDto>> ObtenerFacturasAsync(int usuarioId, DateTime? desde, DateTime? hasta, int? clienteId) =>
            (await _facturaRepository.GetAllAsync(usuarioId, desde, hasta, clienteId)).Select(FacturaDto.From).ToList();

        public async Task<FacturaDto> ObtenerFacturaPorIdAsync(int usuarioId, int id) =>
            FacturaDto.From(await ObtenerEntidadAsync(usuarioId, id));

        public async Task<FacturaDto> CrearFacturaAsync(FacturaRequest request, int usuarioId)
        {
            var fecha = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)); await using var tx = await _periodos.AbrirAsync(usuarioId, [fecha]);
            if (await _clienteRepository.GetByIdAsync(usuarioId, request.ClienteId) is null)
                throw BusinessException.NotFound("Cliente no encontrado.");

            // Agrupa líneas repetidas del mismo producto para validar el stock total pedido.
            var cantidades = request.Detalles
                .GroupBy(d => d.ProductoId)
                .ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));

            var productos = (await _productoRepository.GetByIdsAsync(usuarioId, cantidades.Keys)).ToDictionary(p => p.ProductoId);
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
            _periodos.Auditar(usuarioId, "CrearCatalogo", "FacturaCatalogo", factura.FacturaId.ToString(), $"Total: {factura.MontoTotal}");
            await _db.SaveChangesAsync();
            // El repositorio comparte la misma transacción y guarda stock/factura.
            await tx.ConfirmarAsync();
            return await ObtenerFacturaPorIdAsync(usuarioId, factura.FacturaId);
        }

        public async Task EliminarFacturaAsync(int usuarioId, int id)
        {
            var factura = await ObtenerEntidadAsync(usuarioId, id);
            await using var tx = await _periodos.AbrirAsync(usuarioId, [DateOnly.FromDateTime(factura.Fecha.AddHours(-5))]);
            _periodos.Auditar(usuarioId, "EliminarCatalogo", "FacturaCatalogo", id.ToString(), $"Total: {factura.MontoTotal}");

            // Devuelve el stock de los productos facturados.
            foreach (var detalle in factura.Detalles)
                detalle.Producto.Stock += detalle.Cantidad;

            await _facturaRepository.DeleteAsync(factura);
            await tx.ConfirmarAsync();
        }

        private async Task<Factura> ObtenerEntidadAsync(int usuarioId, int id) =>
            await _facturaRepository.GetByIdAsync(usuarioId, id) ?? throw BusinessException.NotFound("Factura no encontrada.");
    }
}
