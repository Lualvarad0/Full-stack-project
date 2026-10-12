namespace Back.Models
{
    /// <summary>
    /// Firma electrónica (.p12) del usuario. El archivo y su contraseña se guardan cifrados con
    /// ASP.NET Data Protection; los demás campos son datos públicos del certificado para mostrarlos.
    /// </summary>
    public class FirmaElectronica
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public byte[] ArchivoCifrado { get; set; } = Array.Empty<byte>();
        public byte[] ClaveCifrada { get; set; } = Array.Empty<byte>();
        public string Titular { get; set; } = string.Empty;
        /// <summary>Cédula o RUC que consta en el certificado, si la entidad emisora lo incluye.</summary>
        public string? Identificacion { get; set; }
        public string Emisor { get; set; } = string.Empty;
        public string NumeroSerie { get; set; } = string.Empty;
        public DateTime ValidaDesde { get; set; }
        public DateTime ValidaHasta { get; set; }
        public DateTime SubidaEn { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Datos con los que el usuario emite sus comprobantes (establecimiento, punto de emisión, secuencial…).</summary>
    public class DatosEmisor
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public AmbienteSri Ambiente { get; set; } = AmbienteSri.Pruebas;
        public string Establecimiento { get; set; } = "001";
        public string PuntoEmision { get; set; } = "001";
        /// <summary>Próximo secuencial de factura para el ambiente actual.</summary>
        public int SiguienteSecuencial { get; set; } = 1;
        public string? NombreComercial { get; set; }
        public string DireccionMatriz { get; set; } = string.Empty;
        public string? DireccionEstablecimiento { get; set; }
        public bool ObligadoContabilidad { get; set; }
        public string? ContribuyenteEspecial { get; set; }
        public bool AgenteRetencion { get; set; }
    }

    public enum AmbienteSri
    {
        Pruebas = 1,
        Produccion = 2,
    }

    /// <summary>Factura emitida desde Nudo: su XML firmado y lo que respondió el SRI.</summary>
    public class FacturaEmitida
    {
        public string TipoDocumento { get; set; } = TiposComprobante.Factura;
        public int? DocumentoSustentoId { get; set; }
        public int? ComprobanteSustentoId { get; set; }
        public string? NumeroDocumentoSustento { get; set; }
        public DateOnly? FechaDocumentoSustento { get; set; }
        public string? Motivo { get; set; }
        public string? CodigoSustento { get; set; }
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public AmbienteSri Ambiente { get; set; }
        public string ClaveAcceso { get; set; } = string.Empty;
        public string Establecimiento { get; set; } = string.Empty;
        public string PuntoEmision { get; set; } = string.Empty;
        public int Secuencial { get; set; }
        public DateOnly FechaEmision { get; set; }
        public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

        public string TipoIdentificacionComprador { get; set; } = string.Empty;
        public string IdentificacionComprador { get; set; } = string.Empty;
        public string RazonSocialComprador { get; set; } = string.Empty;
        public string? CorreoComprador { get; set; }
        public string? DireccionComprador { get; set; }

        public string FormaPago { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }

        public EstadoFactura Estado { get; set; } = EstadoFactura.Firmada;
        /// <summary>Mensajes del SRI (recepción o autorización), separados por " | ".</summary>
        public string? Mensajes { get; set; }
        public string? NumeroAutorizacion { get; set; }
        public DateTime? FechaAutorizacion { get; set; }
        public string XmlFirmado { get; set; } = string.Empty;

        public List<FacturaEmitidaDetalle> Detalles { get; set; } = new();
        public List<RetencionEmitidaDetalle> Retenciones { get; set; } = [];

        public string Numero => $"{Establecimiento}-{PuntoEmision}-{Secuencial:D9}";
    }

    public class FacturaEmitidaDetalle
    {
        public int? DetalleSustentoId { get; set; }
        public int Id { get; set; }
        public int FacturaEmitidaId { get; set; }
        public string? Codigo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal Subtotal { get; set; }
        public TarifaIva Tarifa { get; set; }
        public decimal Iva { get; set; }
    }

    /// <summary>
    /// Firmada: lista para enviar (o el envío falló por red). Recibida/EnProceso: el SRI la tiene y aún no
    /// la autoriza. Devuelta: rechazada en la recepción (hay que corregir y emitir otra).
    /// </summary>
    public enum EstadoFactura
    {
        Firmada,
        Recibida,
        EnProceso,
        Devuelta,
        Autorizada,
        NoAutorizada,
        /// <summary>Autorizada y luego anulada (la anulación se completa en SRI en Línea).</summary>
        Anulada,
    }

    /// <summary>Producto o servicio que el usuario factura a menudo (se elige al crear una factura).</summary>
    public class ServicioFacturable
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string? Codigo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public TarifaIva Tarifa { get; set; } = TarifaIva.Iva15;
        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Tarifas de IVA (tabla 17 de la ficha técnica: código de porcentaje).</summary>
    public enum TarifaIva
    {
        Iva0 = 0,
        Iva5 = 5,
        Iva15 = 4,
        NoObjeto = 6,
        Exento = 7,
    }
}
