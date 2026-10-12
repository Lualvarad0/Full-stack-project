using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Back.Tributacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Back.Controllers;

[ApiController, Authorize, Route("api/contabilidad/tributacion")]
public class TributacionController(PerfilTributarioService perfil, PreparacionTributariaService preparacion, RevisionTributariaService revision) : ControllerBase
{
    private int Uid => JwtHelper.GetContribuyenteId(User);
    [HttpGet("retenciones/{anio:int}/{mes:int}/{meses:int}")] public Task<PreparacionRetencionesDto> Retenciones(int anio, int mes, int meses, CancellationToken ct) => revision.RetencionesAsync(Uid, anio, mes, meses, ct);
    [HttpGet("ats/{anio:int}/{mes:int}/{meses:int}")] public Task<PreparacionAtsDto> Ats(int anio, int mes, int meses, CancellationToken ct) => revision.AtsAsync(Uid, anio, mes, meses, ct);
    [HttpGet("revisiones")] public Task<List<RevisionTributariaDto>> Revisiones(int anio, CancellationToken ct) => revision.HistorialAsync(Uid, anio, ct);
    [HttpPost("revisiones")] public async Task<IActionResult> Revisar(RevisarTributoRequest r, CancellationToken ct) => Ok(new { id = await revision.RevisarAsync(Uid, r, ct) });
    [HttpPost("revisiones/{id:int}/presentacion")] public async Task<IActionResult> Presentar(int id, PresentacionManualRequest r, CancellationToken ct) { await revision.PresentarManualAsync(Uid, id, r, ct); return NoContent(); }
    [HttpGet("cargas")] public Task<List<CargaFamiliar>> Cargas(int anio, CancellationToken ct) => perfil.CargasAsync(Uid, anio, ct);
    [HttpPost("cargas")] public async Task<IActionResult> Carga(CargaFamiliarRequest r, CancellationToken ct) => Ok(new { id = await perfil.CargaAsync(Uid, r, ct) });
    [HttpDelete("cargas/{id:int}")] public async Task<IActionResult> Quitar(int id, CancellationToken ct) { await perfil.QuitarCargaAsync(Uid, id, ct); return NoContent(); }
    [HttpGet("perfil")] public Task<PerfilObligaciones> Perfil(CancellationToken ct) => perfil.PerfilAsync(Uid, ct);
    [HttpPut("perfil")] public async Task<IActionResult> GuardarPerfil(PerfilObligacionesRequest r, CancellationToken ct) { await perfil.GuardarPerfilAsync(Uid, r, ct); return NoContent(); }
    [HttpGet("calendario")] public Task<List<VencimientoTributarioDto>> Calendario(int anio, CancellationToken ct) => perfil.CalendarioAsync(Uid, anio, ct);
    [HttpGet("documentos")] public Task<List<DocumentoFiscalDto>> Documentos(int anio, int mes, int meses, CancellationToken ct) => preparacion.DocumentosAsync(Uid, anio, mes, meses, ct);
    [HttpPut("documentos/{id:int}")] public async Task<IActionResult> RevisarDocumento(int id, RevisionFiscalRequest r, CancellationToken ct) { await preparacion.RevisarDocumentoAsync(Uid, id, r, ct); return NoContent(); }
    [HttpGet("gastos-personales/{anio:int}")] public Task<GastosPersonalesDto> Gastos(int anio, CancellationToken ct) => preparacion.GastosAsync(Uid, anio, ct);
    [HttpGet("renta/{anio:int}")] public Task<RentaPreparadaDto> Renta(int anio, CancellationToken ct) => preparacion.RentaAsync(Uid, anio, ct);
    [HttpPut("renta/{anio:int}")] public async Task<IActionResult> GuardarRenta(int anio, RentaRequest r, CancellationToken ct) { await preparacion.GuardarRentaAsync(Uid, anio, r, ct); return NoContent(); }
    [HttpGet("iva/{anio:int}/{mes:int}/{meses:int}")] public Task<IvaPreparadoDto> Iva(int anio, int mes, int meses, CancellationToken ct) => preparacion.IvaAsync(Uid, anio, mes, meses, ct);
    [HttpPut("iva/{anio:int}/{mes:int}/{meses:int}")] public async Task<IActionResult> GuardarIva(int anio, int mes, int meses, IvaRequest r, CancellationToken ct) { await preparacion.GuardarIvaAsync(Uid, anio, mes, meses, r, ct); return NoContent(); }
}
