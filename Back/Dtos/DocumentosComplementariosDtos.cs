using System.ComponentModel.DataAnnotations;
using Back.Seguridad;
namespace Back.Dtos;

public record LineaCreditoRequest([Range(1, int.MaxValue)] int DetalleId, [Range(0.000001, 999999999)] decimal Cantidad);
public record EmitirNotaCreditoRequest([Range(1, int.MaxValue)] int FacturaId,
    [TextoPlano, Required, StringLength(300, MinimumLength = 5)] string Motivo,
    [Required, MinLength(1), MaxLength(100)] List<LineaCreditoRequest> Detalles);
public record LineaRetencionRequest([Required, RegularExpression("^(1|2|6)$")] string CodigoImpuesto,
    [Required, RegularExpression("^[0-9]{1,10}$")] string CodigoRetencion,
    [Range(0.01, 999999999)] decimal BaseImponible, [Range(0.01, 100)] decimal Porcentaje);
public record EmitirRetencionRequest([Range(1, int.MaxValue)] int ComprobanteId,
    [Required, RegularExpression("^[0-9]{2}$")] string CodigoSustento,
    [Required, MinLength(1), MaxLength(100)] List<LineaRetencionRequest> Detalles);
public record LineaRetencionDto(string CodigoImpuesto, string CodigoRetencion, decimal BaseImponible, decimal Porcentaje, decimal Valor);
