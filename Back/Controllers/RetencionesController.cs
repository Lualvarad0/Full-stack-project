using System.ComponentModel.DataAnnotations;
using System.Text;
using Back.Contabilidad;
using Back.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Back.Controllers;

public record ImportarRetencionesRequest([Required, StringLength(1024 * 1024)] string Texto);
[ApiController, Authorize, Route("api/contabilidad/retenciones")]
public class RetencionesController(RetencionesService servicio) : ControllerBase
{
    private int UsuarioId => JwtHelper.GetContribuyenteId(User);
    [HttpPost("importar")] public Task<ResultadoRetencionesDto> Importar(ImportarRetencionesRequest request, CancellationToken ct) => servicio.ImportarAsync(UsuarioId, request.Texto, ct);
    [HttpPost("importar/archivos"), RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ResultadoRetencionesDto> Archivos([FromForm] List<IFormFile> archivos, CancellationToken ct)
    {
        if (archivos.Count is < 1 or > 20 || archivos.Sum(a => a.Length) > 10 * 1024 * 1024) throw new BusinessException("Adjunta hasta 20 XML/TXT, con un máximo total de 10 MB.");
        var claves = new HashSet<string>();
        foreach (var archivo in archivos) {
            if (Path.GetExtension(archivo.FileName).ToLowerInvariant() is not (".xml" or ".txt")) throw new BusinessException("Adjunta archivos XML o TXT.");
            using var lector = new StreamReader(archivo.OpenReadStream());
            foreach (var clave in Back.Sri.ClaveAcceso.Extraer(await lector.ReadToEndAsync(ct))) claves.Add(clave);
        }
        return await servicio.ImportarAsync(UsuarioId, string.Join("\n", claves), ct);
    }
    [HttpGet] public Task<List<RetencionResumenDto>> Listar([FromQuery, Range(2000, 2100)] int anio, [FromQuery, Range(1, 12)] int? mes, CancellationToken ct) => servicio.ListarAsync(UsuarioId, anio, mes, ct);
    [HttpGet("{id:int}")] public async Task<IActionResult> Obtener(int id, CancellationToken ct) => await servicio.ObtenerAsync(UsuarioId, id, ct) is { } r ? Ok(r) : NotFound();
    [HttpGet("{id:int}/xml")] public async Task<IActionResult> Xml(int id, CancellationToken ct) => await servicio.ObtenerAsync(UsuarioId, id, ct) is { } r ? File(Encoding.UTF8.GetBytes(r.Xml), "application/xml", $"retencion-{r.Numero}.xml") : NotFound();
    [HttpGet("{id:int}/pdf")] public async Task<IActionResult> Pdf(int id, CancellationToken ct) => File(await servicio.PdfAsync(UsuarioId, id, ct), "application/pdf", $"retencion-{id}.pdf");
    [HttpPost("{id:int}/revisar")] public async Task<IActionResult> Revisar(int id, CancellationToken ct) { await servicio.RevisarAsync(UsuarioId, id, ct); return NoContent(); }
}
