using Back.Dtos;
using Back.Helpers;
using Back.Tarjetas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers;

/// <summary>Tarjetas de crédito y sus estados de cuenta (planes de pago). Finanzas personales: solo la cuenta propia.</summary>
[ApiController, Authorize, Route("api/tarjetas")]
public class TarjetasController(TarjetasService tarjetas) : ControllerBase
{
    private int UsuarioId => JwtHelper.GetUsuarioId(User);

    [HttpGet] public Task<TarjetasDto> Listar(CancellationToken ct) => tarjetas.ListarAsync(UsuarioId, ct);
    [HttpPost] public Task<TarjetaDto> Crear(TarjetaRequest r, CancellationToken ct) => tarjetas.CrearAsync(UsuarioId, r, ct);
    [HttpDelete("{id:int}")] public async Task<IActionResult> Eliminar(int id, CancellationToken ct) { await tarjetas.EliminarAsync(UsuarioId, id, ct); return NoContent(); }

    /// <summary>Sube el PDF del estado de cuenta (y su clave, si tiene). Lo leído queda por confirmar; el PDF no se guarda.</summary>
    [HttpPost("{id:int}/estado-cuenta")]
    [RequestSizeLimit(LectorEstadoCuenta.MaxBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = LectorEstadoCuenta.MaxBytes + 64 * 1024)]
    public async Task<EstadoCuentaDto> SubirEstadoCuenta(int id, IFormFile archivo, [FromForm] string? clave, CancellationToken ct)
    {
        if (archivo.Length > LectorEstadoCuenta.MaxBytes) throw new BusinessException("El estado de cuenta debe ser un PDF de hasta 10 MB.");
        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria, ct);
        return await tarjetas.SubirEstadoCuentaAsync(UsuarioId, id, memoria.ToArray(), clave, ct);
    }

    [HttpPost("{id:int}/estados")] public Task<EstadoCuentaDto> RegistrarManual(int id, EstadoCuentaRequest r, CancellationToken ct) => tarjetas.RegistrarManualAsync(UsuarioId, id, r, ct);
    [HttpPut("estados/{id:int}")] public Task<EstadoCuentaDto> Confirmar(int id, EstadoCuentaRequest r, CancellationToken ct) => tarjetas.ConfirmarAsync(UsuarioId, id, r, ct);
    [HttpDelete("estados/{id:int}")] public async Task<IActionResult> Descartar(int id, CancellationToken ct) { await tarjetas.DescartarAsync(UsuarioId, id, ct); return NoContent(); }
}
