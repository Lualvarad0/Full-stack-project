using Back.Avisos;
using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Microsoft.EntityFrameworkCore;

namespace Back.Services;

/// <summary>Datos personales y configuración del sistema del propio usuario.</summary>
public class CuentaUsuarioService(AppDbContext db, AvisoService avisos)
{
    public async Task<DatosPersonalesDto> DatosAsync(int usuarioId, CancellationToken ct) =>
        DatosPersonalesDto.From(await UsuarioAsync(usuarioId, ct));

    public async Task<DatosPersonalesActualizadosDto> ActualizarDatosAsync(int usuarioId, DatosPersonalesRequest r, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
        if (r.FechaNacimiento > hoy.AddYears(-18)) throw new BusinessException("Debes ser mayor de edad para usar Nudo.");
        if (r.FechaNacimiento < new DateOnly(1900, 1, 1)) throw new BusinessException("Revisa tu fecha de nacimiento.");

        var u = await UsuarioAsync(usuarioId, ct);
        u.Nombre = r.Nombre.Trim();
        u.Apellido = r.Apellido.Trim();
        u.FechaNacimiento = r.FechaNacimiento;
        u.Telefono = Limpio(r.Telefono);
        u.Ciudad = Limpio(r.Ciudad);
        u.Direccion = Limpio(r.Direccion);
        u.Ocupacion = Limpio(r.Ocupacion);
        await db.SaveChangesAsync(ct);
        await avisos.NotificarAsync(u.Id, TipoAviso.PerfilActualizado, "Datos personales actualizados",
            "Se cambiaron tus datos personales (nombre, contacto o dirección).");
        return new DatosPersonalesActualizadosDto(DatosPersonalesDto.From(u), UsuarioDto.From(u));
    }

    public async Task<ConfiguracionSistemaDto> ConfiguracionAsync(int usuarioId, CancellationToken ct) =>
        ConfiguracionSistemaDto.From(await UsuarioAsync(usuarioId, ct));

    public async Task<ConfiguracionSistemaDto> GuardarConfiguracionAsync(int usuarioId, ConfiguracionSistemaRequest r, CancellationToken ct)
    {
        var u = await UsuarioAsync(usuarioId, ct);
        var desactivaAvisos = u.AvisarIngresosPorCorreo && !r.AvisarIngresosPorCorreo;
        u.PaginaInicio = r.PaginaInicio;
        u.MesAlEntrar = r.MesAlEntrar;
        u.AvisarIngresosPorCorreo = r.AvisarIngresosPorCorreo;
        await db.SaveChangesAsync(ct);
        // Apagar los avisos de ingreso es un cambio de seguridad: se avisa siempre (este aviso sí va por correo).
        if (desactivaAvisos)
            await avisos.NotificarAsync(u.Id, TipoAviso.PerfilActualizado, "Avisos de ingreso desactivados",
                "Ya no te enviaremos un correo cada vez que alguien ingrese a tu cuenta. Puedes verlo en tu actividad.");
        return ConfiguracionSistemaDto.From(u);
    }

    private async Task<Usuario> UsuarioAsync(int usuarioId, CancellationToken ct) =>
        await db.Usuarios.SingleOrDefaultAsync(u => u.Id == usuarioId, ct) ?? throw BusinessException.NotFound("Usuario no encontrado.");

    private static string? Limpio(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
