using Back.Dtos;
using Back.Facturacion;
using Back.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Back.Controllers;

[ApiController, Authorize, Route("api/facturacion/complementarios")]
public class DocumentosComplementariosController(DocumentosComplementariosService servicio) : ControllerBase
{
    [HttpPost("nota-credito")] public Task<FacturaEmitidaDto> Credito(EmitirNotaCreditoRequest request, CancellationToken ct) => servicio.NotaCreditoAsync(JwtHelper.GetContribuyenteId(User), request, ct);
    [HttpPost("retencion")] public Task<FacturaEmitidaDto> Retencion(EmitirRetencionRequest request, CancellationToken ct) => servicio.RetencionAsync(JwtHelper.GetContribuyenteId(User), request, ct);
}
