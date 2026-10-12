using Back.Seguridad;
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
        public bool AgenteRetencion { get; init; }
        public static DatosEmisorDto From(DatosEmisor d, List<string> pendientes) => new(
            d.Ambiente, d.Establecimiento, d.PuntoEmision, d.SiguienteSecuencial, d.NombreComercial, d.DireccionMatriz,
            d.DireccionEstablecimiento, d.ObligadoContabilidad, d.ContribuyenteEspecial, pendientes) { AgenteRetencion = d.AgenteRetencion };
    }

    public record DatosEmisorRequest(
        [Required, EnumDataType(typeof(AmbienteSri))] AmbienteSri Ambiente,
        [Required, RegularExpression(@"^[0-9]{3}$", ErrorMessage = "El establecimiento son 3 dígitos (por ejemplo 001).")] string Establecimiento,
        [Required, RegularExpression(@"^[0-9]{3}$", ErrorMessage = "El punto de emisión son 3 dígitos (por ejemplo 001).")] string PuntoEmision,
        [Range(1, 999_999_999)] int SiguienteSecuencial,
        [TextoPlano, StringLength(300)] string? NombreComercial,
        [TextoPlano, Required, StringLength(300, MinimumLength = 3)] string DireccionMatriz,
        [TextoPlano, StringLength(300)] string? DireccionEstablecimiento,
        bool ObligadoContabilidad,
        [RegularExpression(@"^[0-9]{3,13}$", ErrorMessage = "El número de contribuyente especial son solo dígitos.")] string? ContribuyenteEspecial)
    { public bool AgenteRetencion { get; init; } }

    public record LineaFacturaRequest(
        [TextoPlano, StringLength(25)] string? Codigo,
        [TextoPlano, Required, StringLength(300, MinimumLength = 1)] string Descripcion,
        [Range(0.000001, 999_999_999)] decimal Cantidad,
        [Range(0, 999_999_999)] decimal PrecioUnitario,
        [Range(0, 999_999_999)] decimal Descuento,
        [Required, EnumDataType(typeof(TarifaIva))] TarifaIva Tarifa);

    public record EmitirFacturaRequest(
        [Required, RegularExpression("^(04|05|06|07|08)$")] string TipoIdentificacion,
        [TextoPlano, Required, StringLength(20)] string Identificacion,
        [TextoPlano, Required, StringLength(300, MinimumLength = 2)] string RazonSocial,
        [TextoPlano, EmailAddress, StringLength(150)] string? Correo,
        [TextoPlano, StringLength(300)] string? Direccion,
        [TextoPlano, Required] string FormaPago,
        [Required, MaxLength(100), MinLength(1, ErrorMessage = "Agrega al menos un producto o servicio.")] List<LineaFacturaRequest> Detalles);

    public record LineaFacturaDto(string? Codigo, string Descripcion, decimal Cantidad, decimal PrecioUnitario, decimal Descuento, decimal Subtotal, TarifaIva Tarifa, decimal Iva) { public int Id { get; init; } }

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
        public string TipoDocumento { get; init; } = "01";
        public int? DocumentoSustentoId { get; init; }
        public int? ComprobanteSustentoId { get; init; }
        public string? NumeroDocumentoSustento { get; init; }
        public string? Motivo { get; init; }
        public List<LineaRetencionDto>? Retenciones { get; init; }
        public static FacturaEmitidaDto From(FacturaEmitida f, bool conDetalles = false) => new(
            f.Id, f.Numero, f.ClaveAcceso, f.Ambiente, f.FechaEmision, f.TipoIdentificacionComprador, f.IdentificacionComprador,
            f.RazonSocialComprador, f.CorreoComprador, f.DireccionComprador, f.FormaPago, f.Subtotal, f.Descuento, f.Iva, f.Total,
            f.Estado, f.Mensajes, f.NumeroAutorizacion, f.FechaAutorizacion,
            conDetalles ? f.Detalles.Select(d => new LineaFacturaDto(d.Codigo, d.Descripcion, d.Cantidad, d.PrecioUnitario, d.Descuento, d.Subtotal, d.Tarifa, d.Iva) { Id = d.Id }).ToList() : null)
            { TipoDocumento = f.TipoDocumento, DocumentoSustentoId = f.DocumentoSustentoId, ComprobanteSustentoId = f.ComprobanteSustentoId, NumeroDocumentoSustento = f.NumeroDocumentoSustento, Motivo = f.Motivo,
              Retenciones = conDetalles ? f.Retenciones.Select(r => new LineaRetencionDto(r.CodigoImpuesto, r.CodigoRetencion, r.BaseImponible, r.Porcentaje, r.Valor)).ToList() : null };
    }

    public record ServicioFacturableDto(int Id, string? Codigo, string Descripcion, decimal PrecioUnitario, TarifaIva Tarifa)
    {
        public static ServicioFacturableDto From(ServicioFacturable s) => new(s.Id, s.Codigo, s.Descripcion, s.PrecioUnitario, s.Tarifa);
    }

    public record ServicioFacturableRequest(
        [TextoPlano, StringLength(25)] string? Codigo,
        [TextoPlano, Required, StringLength(300, MinimumLength = 2)] string Descripcion,
        [Range(0, 999_999_999)] decimal PrecioUnitario,
        [Required, EnumDataType(typeof(TarifaIva))] TarifaIva Tarifa);
}
