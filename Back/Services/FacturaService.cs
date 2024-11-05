public class FacturaService
{
    private readonly IFacturaRepository _facturaRepository;

    public FacturaService(IFacturaRepository facturaRepository)
    {
        _facturaRepository = facturaRepository;
    }

    public async Task<IEnumerable<Factura>> ObtenerFacturasAsync()
    {
        return await _facturaRepository.GetFacturasAsync();
    }

    public async Task<Factura> ObtenerFacturaPorIdAsync(int id)
    {
        return await _facturaRepository.GetFacturaByIdAsync(id);
    }

    public async Task CrearFacturaAsync(Factura factura)
    {
        factura.MontoTotal = factura.Detalles.Sum(d => d.PrecioTotal);
        await _facturaRepository.AddFacturaAsync(factura);
    }
}
