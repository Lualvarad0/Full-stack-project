using Back.Dtos;
using Back.Helpers;
using Back.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Back.Controllers;

/// <summary>Configuración → Mi perfil (datos personales) y Sistema (preferencias guardadas en el servidor).</summary>
[ApiController, Authorize, Route("api/cuenta")]
public class CuentaUsuarioController(CuentaUsuarioService cuenta) : ControllerBase
{
    // Siempre la cuenta propia, aunque un contador esté trabajando con un contribuyente.
    private int UsuarioId => JwtHelper.GetUsuarioId(User);

    [HttpGet("datos")] public Task<DatosPersonalesDto> Datos(CancellationToken ct) => cuenta.DatosAsync(UsuarioId, ct);
    [HttpPut("datos")] public Task<DatosPersonalesActualizadosDto> GuardarDatos(DatosPersonalesRequest r, CancellationToken ct) => cuenta.ActualizarDatosAsync(UsuarioId, r, ct);
    [HttpPut("fecha-nacimiento")] public Task<UsuarioDto> CompletarFechaNacimiento(FechaNacimientoRequest r, CancellationToken ct) => cuenta.CompletarFechaNacimientoAsync(UsuarioId, r.FechaNacimiento, ct);
    [HttpGet("configuracion")] public Task<ConfiguracionSistemaDto> Configuracion(CancellationToken ct) => cuenta.ConfiguracionAsync(UsuarioId, ct);
    [HttpPut("configuracion")] public Task<ConfiguracionSistemaDto> GuardarConfiguracion(ConfiguracionSistemaRequest r, CancellationToken ct) => cuenta.GuardarConfiguracionAsync(UsuarioId, r, ct);
}
