namespace Back.Models;

public class RetencionSri
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Direccion Direccion { get; set; }
    public string ClaveAcceso { get; set; } = "";
    public string Numero { get; set; } = "";
    public DateOnly FechaEmision { get; set; }
    public DateOnly PeriodoFiscal { get; set; }
    public string RucEmisor { get; set; } = "";
    public string RazonSocialEmisor { get; set; } = "";
    public string IdentificacionRetenido { get; set; } = "";
    public string RazonSocialRetenido { get; set; } = "";
    public string Xml { get; set; } = "";
    public string NumeroAutorizacion { get; set; } = "";
    public DateTime? FechaAutorizacion { get; set; }
    public DateTime? RevisadoEn { get; set; }
    public List<RetencionSriDetalle> Detalles { get; set; } = [];
}
public class RetencionSriDetalle
{
    public int Id { get; set; }
    public int RetencionSriId { get; set; }
    public string CodigoImpuesto { get; set; } = "";
    public string CodigoRetencion { get; set; } = "";
    public decimal BaseImponible { get; set; }
    public decimal Porcentaje { get; set; }
    public decimal Valor { get; set; }
    public string? DocumentoSustento { get; set; }
    public int? ComprobanteId { get; set; }
}
public class RetencionEmitidaDetalle
{
    public int Id { get; set; }
    public int FacturaEmitidaId { get; set; }
    public string CodigoImpuesto { get; set; } = "";
    public string CodigoRetencion { get; set; } = "";
    public decimal BaseImponible { get; set; }
    public decimal Porcentaje { get; set; }
    public decimal Valor { get; set; }
}
