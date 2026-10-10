using System.ComponentModel.DataAnnotations;
using Back.Models;

namespace Back.Dtos
{
    public record DetalleFacturaRequest(
        [Range(1, int.MaxValue)] int ProductoId,
        [Range(1, int.MaxValue)] int Cantidad);

    public record FacturaRequest(
        [Range(1, int.MaxValue)] int ClienteId,
        [Required, StringLength(50)] string FormaPago,
        [Required, MinLength(1)] List<DetalleFacturaRequest> Detalles);

    public record DetalleFacturaDto(int ProductoId, string Producto, int Cantidad, decimal PrecioUnitario, decimal PrecioTotal);

    public record FacturaDto(
        int Id,
        DateTime Fecha,
        int ClienteId,
        string Cliente,
        string Vendedor,
        string FormaPago,
        decimal MontoTotal,
        List<DetalleFacturaDto> Detalles)
    {
        public static FacturaDto From(Factura f) => new(
            f.FacturaId,
            f.Fecha,
            f.ClienteId,
            f.Cliente.Nombre,
            f.Usuario.Nombre,
            f.FormaPago,
            f.MontoTotal,
            f.Detalles.Select(d => new DetalleFacturaDto(d.ProductoId, d.Producto.Nombre, d.Cantidad, d.PrecioUnitario, d.PrecioTotal)).ToList());
    }
}
