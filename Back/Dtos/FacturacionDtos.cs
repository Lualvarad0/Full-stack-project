using System.ComponentModel.DataAnnotations;
using Back.Models;

namespace Back.Dtos
{
    public record FirmaElectronicaDto(
        string Titular,
        string? Identificacion,
        string Emisor,
        string NumeroSerie,
        DateTime ValidaDesde,
        DateTime ValidaHasta,
        DateTime SubidaEn,
        bool Vigente,
        int DiasRestantes,
        /// <summary>false si el certificado trae una cédula/RUC que no coincide con el perfil.</summary>
        bool? CoincideConPerfil)
    {
        public static FirmaElectronicaDto From(FirmaElectronica f, Usuario usuario)
        {
            var ahora = DateTime.UtcNow;
            bool? coincide = f.Identificacion is null || (usuario.Cedula is null && usuario.Ruc is null)
                ? null
                : usuario.Identificaciones().Any(id => id == f.Identificacion || (f.Identificacion.Length == 10 && id.StartsWith(f.Identificacion)));
            return new FirmaElectronicaDto(f.Titular, f.Identificacion, f.Emisor, f.NumeroSerie, f.ValidaDesde, f.ValidaHasta, f.SubidaEn,
                f.ValidaDesde <= ahora && ahora <= f.ValidaHasta, (int)Math.Floor((f.ValidaHasta - ahora).TotalDays), coincide);
        }
    }

    public record DatosEmisorDto(
        AmbienteSri Ambiente,
        string Establecimiento,
        string PuntoEmision,
        int SiguienteSecuencial,
        string? NombreComercial,
        string DireccionMatriz,
        string? DireccionEstablecimiento,
        bool ObligadoContabilidad,
        string? ContribuyenteEspecial,
        /// <summary>Lo que falta para poder emitir (RUC, razón social, firma, dirección…); vacío si está listo.</summary>
        List<string> Pendientes)
    {
        public static DatosEmisorDto From(DatosEmisor d, List<string> pendientes) => new(
            d.Ambiente, d.Establecimiento, d.PuntoEmision, d.SiguienteSecuencial, d.NombreComercial, d.DireccionMatriz,
            d.DireccionEstablecimiento, d.ObligadoContabilidad, d.ContribuyenteEspecial, pendientes);
    }

    public record DatosEmisorRequest(
        [Required] AmbienteSri Ambiente,
        [Required, RegularExpression(@"^\d{3}$", ErrorMessage = "El establecimiento son 3 dígitos (por ejemplo 001).")] string Establecimiento,
        [Required, RegularExpression(@"^\d{3}$", ErrorMessage = "El punto de emisión son 3 dígitos (por ejemplo 001).")] string PuntoEmision,
        [Range(1, 999_999_999)] int SiguienteSecuencial,
        [StringLength(300)] string? NombreComercial,
        [Required, StringLength(300, MinimumLength = 3)] string DireccionMatriz,
        [StringLength(300)] string? DireccionEstablecimiento,
        bool ObligadoContabilidad,
        [RegularExpression(@"^\d{3,13}$", ErrorMessage = "El número de contribuyente especial son solo dígitos.")] string? ContribuyenteEspecial);

    public record LineaFacturaRequest(
        [StringLength(25)] string? Codigo,
        [Required, StringLength(300, MinimumLength = 1)] string Descripcion,
        [Range(0.000001, 999_999_999)] decimal Cantidad,
        [Range(0, 999_999_999)] decimal PrecioUnitario,
        [Range(0, 999_999_999)] decimal Descuento,
        [Required] TarifaIva Tarifa);

    public record EmitirFacturaRequest(
        [Required] string TipoIdentificacion,
        [Required, StringLength(20)] string Identificacion,
        [Required, StringLength(300, MinimumLength = 2)] string RazonSocial,
        [EmailAddress, StringLength(150)] string? Correo,
        [StringLength(300)] string? Direccion,
        [Required] string FormaPago,
        [Required, MinLength(1, ErrorMessage = "Agrega al menos un producto o servicio.")] List<LineaFacturaRequest> Detalles);

    public record LineaFacturaDto(string? Codigo, string Descripcion, decimal Cantidad, decimal PrecioUnitario, decimal Descuento, decimal Subtotal, TarifaIva Tarifa, decimal Iva);

    public record FacturaEmitidaDto(
        int Id,
        string Numero,
        string ClaveAcceso,
        AmbienteSri Ambiente,
        DateOnly FechaEmision,
        string TipoIdentificacionComprador,
        string IdentificacionComprador,
        string RazonSocialComprador,
        string? CorreoComprador,
        string? DireccionComprador,
        string FormaPago,
        decimal Subtotal,
        decimal Descuento,
        decimal Iva,
        decimal Total,
        EstadoFactura Estado,
        string? Mensajes,
        string? NumeroAutorizacion,
        DateTime? FechaAutorizacion,
        List<LineaFacturaDto>? Detalles)
    {
        public static FacturaEmitidaDto From(FacturaEmitida f, bool conDetalles = false) => new(
            f.Id, f.Numero, f.ClaveAcceso, f.Ambiente, f.FechaEmision, f.TipoIdentificacionComprador, f.IdentificacionComprador,
            f.RazonSocialComprador, f.CorreoComprador, f.DireccionComprador, f.FormaPago, f.Subtotal, f.Descuento, f.Iva, f.Total,
            f.Estado, f.Mensajes, f.NumeroAutorizacion, f.FechaAutorizacion,
            conDetalles ? f.Detalles.Select(d => new LineaFacturaDto(d.Codigo, d.Descripcion, d.Cantidad, d.PrecioUnitario, d.Descuento, d.Subtotal, d.Tarifa, d.Iva)).ToList() : null);
    }

    public record ServicioFacturableDto(int Id, string? Codigo, string Descripcion, decimal PrecioUnitario, TarifaIva Tarifa)
    {
        public static ServicioFacturableDto From(ServicioFacturable s) => new(s.Id, s.Codigo, s.Descripcion, s.PrecioUnitario, s.Tarifa);
    }

    public record ServicioFacturableRequest(
        [StringLength(25)] string? Codigo,
        [Required, StringLength(300, MinimumLength = 2)] string Descripcion,
        [Range(0, 999_999_999)] decimal PrecioUnitario,
        [Required] TarifaIva Tarifa);
}
