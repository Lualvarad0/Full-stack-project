using Back.Contabilidad;
using Back.Data;
using Back.Helpers;
using Back.Pagos;
using Back.Tributacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Back.Controllers;

[ApiController, Authorize, Route("api/contabilidad/exportaciones")]
public class ExportacionesController(ExportacionesService exportaciones, AppDbContext db) : ControllerBase
{
    private int Uid => JwtHelper.GetContribuyenteId(User);
    [HttpGet("comprobantes")]
    public async Task<IActionResult> Comprobantes(int anio, int mes, int meses, string formato = "xlsx", CancellationToken ct = default)
    {
        if (formato is not ("xlsx" or "csv")) throw new BusinessException("Selecciona Excel XLSX o CSV.");
        return File(await exportaciones.ComprobantesAsync(Uid, anio, mes, meses, formato == "xlsx", ct), formato == "xlsx" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "text/csv;charset=utf-8", $"comprobantes-{anio}-{mes:D2}.{formato}");
    }
    [HttpGet("balance")] public async Task<IActionResult> Balance(int anio, int mes, CancellationToken ct) => File(await exportaciones.BalanceAsync(Uid, anio, mes, ct), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"balance-{anio}-{mes:D2}.xlsx");
    [HttpGet("paquete")] public async Task<IActionResult> Paquete(int anio, int mes, int meses, CancellationToken ct) => File(await exportaciones.PaqueteAsync(Uid, anio, mes, meses, ct), "application/zip", $"entrega-contable-{anio}-{mes:D2}.zip");
    [HttpPost("validar-ats"), RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ValidacionAtsDto> Validar([FromForm] IFormFile archivo, CancellationToken ct)
    {
        AccesoPlan.ExigirProfesional(await db.Usuarios.SingleAsync(u => u.Id == Uid, ct), "La validación de anexos");
        if (archivo.Length > 5 * 1024 * 1024 || Path.GetExtension(archivo.FileName).ToLowerInvariant() != ".xml") throw new BusinessException("Adjunta un XML ATS de hasta 5 MB.");
        var ruc = (await db.Usuarios.SingleAsync(u => u.Id == Uid, ct)).Ruc;
        if (string.IsNullOrEmpty(ruc)) throw new BusinessException("Completa tu RUC antes de validar el ATS.");
        using var reader = new StreamReader(archivo.OpenReadStream()); return ValidadorAts.Validar(await reader.ReadToEndAsync(ct), ruc);
    }
}
