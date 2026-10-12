using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Back.Avisos;
using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Back.Seguridad;

public class SeguridadCuentaService(AppDbContext db, AvisoService avisos, IDataProtectionProvider proteccion,
    IConfiguration config, IOptions<SmtpOptions> smtp, IWebHostEnvironment entorno)
{
    public const string ProtectorCorreo = "nudo.correos-acceso.v1";
    public const string RespuestaRecuperacion = "Si la cuenta está activa, recibirás un enlace para recuperar tu contraseña. Revisa también la carpeta de correo no deseado.";
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.ToUpperInvariant())));

    public async Task SolicitarRecuperacionAsync(string email, CancellationToken ct)
    {
        var reloj = Stopwatch.StartNew();
        if (smtp.Value.Configurado) UrlConfiable();
        var id = await db.Usuarios.Where(u => u.Correo == email.Trim().ToLowerInvariant() && u.Activo).Select(u => (int?)u.Id).SingleOrDefaultAsync(ct);
        if (id.HasValue && smtp.Value.Configurado) await EmitirEnlaceAsync(id.Value, PropositoTokenCuenta.RecuperarClave, ct);
        // Respuesta y tiempo mínimo iguales para cuentas existentes, desconocidas y suspendidas.
        var espera = 700 + RandomNumberGenerator.GetInt32(100) - (int)reloj.ElapsedMilliseconds;
        if (espera > 0) await Task.Delay(espera, ct);
    }
    public async Task SolicitarVerificacionAsync(int usuarioId, CancellationToken ct)
    {
        if (!smtp.Value.Configurado) throw BusinessException.Conflict("El envío de correo aún no está configurado. Contacta al soporte.");
        await EmitirEnlaceAsync(usuarioId, PropositoTokenCuenta.VerificarCorreo, ct);
    }
    private async Task EmitirEnlaceAsync(int id, PropositoTokenCuenta proposito, CancellationToken ct)
    {
        var baseUrl = UrlConfiable();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var usuario = await BloquearUsuarioAsync(id, ct);
        if (!usuario.Activo || (proposito == PropositoTokenCuenta.VerificarCorreo && usuario.CorreoVerificado)) return;
        var ahora = DateTime.UtcNow;
        var recientes = db.Set<TokenCuenta>().Where(t => t.UsuarioId == id && t.Proposito == proposito && t.CreadoEn > ahora.AddHours(-1));
        if (await recientes.CountAsync(ct) >= 3 || await recientes.AnyAsync(t => t.CreadoEn > ahora.AddMinutes(-1), ct)) return;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var recuperar = proposito == PropositoTokenCuenta.RecuperarClave;
        var expira = ahora.AddMinutes(recuperar ? 30 : 1440);
        var enlace = baseUrl.TrimEnd('/') + (recuperar ? "/restablecer-clave" : "/verificar-correo") + "#token=" + token;
        var titulo = recuperar ? "Recupera tu contraseña de Nudo" : "Verifica tu correo en Nudo";
        var texto = $"Hola, {usuario.Nombre}.\n{titulo}:\n{enlace}\nEl enlace vence en {(recuperar ? "30 minutos" : "24 horas")} y es de un solo uso. Si no lo solicitaste, ignora este mensaje. Nunca compartas este enlace.";
        var html = $"<p>Hola, {WebUtility.HtmlEncode(usuario.Nombre)}.</p><p>{titulo}</p><p><a href=\"{WebUtility.HtmlEncode(enlace)}\">{(recuperar ? "Elegir nueva contraseña" : "Verificar mi correo")}</a></p><p>El enlace vence en {(recuperar ? "30 minutos" : "24 horas")} y es de un solo uso. Si no lo solicitaste, ignora este mensaje. Nunca compartas este enlace.</p>";
        db.Set<TokenCuenta>().Add(new() { UsuarioId = id, Hash = Hash(token), Proposito = proposito, ExpiraEn = expira });
        db.Set<CorreoAccesoPendiente>().Add(new() { UsuarioId = id, ExpiraEn = expira,
            ContenidoCifrado = proteccion.CreateProtector(ProtectorCorreo).Protect(JsonSerializer.Serialize(new CorreoSaliente(usuario.Correo, titulo, html, texto))) });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    private string UrlConfiable()
    {
        var baseUrl = config["Frontend:Url"] ?? "http://localhost:4200";
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0
            || (uri.Scheme != "https" && !((entorno.IsDevelopment() || entorno.IsEnvironment("Testing")) && uri.Scheme == "http" && uri.IsLoopback)))
            throw new InvalidOperationException("Frontend:Url debe ser la dirección HTTPS confiable del sistema.");
        return baseUrl;
    }
    public async Task RestablecerAsync(RestablecerClaveRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var t = await BuscarTokenAsync(request.Token, PropositoTokenCuenta.RecuperarClave, ct);
        var u = await BloquearUsuarioAsync(t.UsuarioId, ct);
        await ValidarTokenBajoBloqueoAsync(t.Id, PropositoTokenCuenta.RecuperarClave, u, ct);
        await AplicarClaveAsync(u, request.Password, ct);
        await tx.CommitAsync(ct);
        await avisos.NotificarAsync(u.Id, TipoAviso.ClaveCambiada, "Se recuperó tu contraseña", "Cambiaste tu contraseña mediante el enlace de recuperación. Todas las sesiones anteriores quedaron cerradas. Si no fuiste tú, recupera tu cuenta y contacta al soporte.");
    }
    public async Task CambiarClaveAsync(int id, CambiarClaveRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var u = await BloquearUsuarioAsync(id, ct);
        if (!BCrypt.Net.BCrypt.Verify(request.Actual, u.PasswordHash)) throw new BusinessException("La contraseña actual no es correcta.");
        await AplicarClaveAsync(u, request.Password, ct); await tx.CommitAsync(ct);
        await avisos.NotificarAsync(id, TipoAviso.ClaveCambiada, "Cambiaste tu contraseña", "Todas tus sesiones anteriores se cerraron. Ingresa con tu nueva contraseña.");
    }
    private async Task AplicarClaveAsync(Usuario u, string password, CancellationToken ct)
    {
        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password); u.VersionSeguridad++;
        var ahora = DateTime.UtcNow;
        await db.Set<TokenCuenta>().Where(t => t.UsuarioId == u.Id && t.UsadoEn == null && t.Proposito == PropositoTokenCuenta.RecuperarClave)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsadoEn, ahora), ct);
        await db.Set<SesionCuenta>().Where(s => s.UsuarioId == u.Id && s.RevocadaEn == null).ExecuteUpdateAsync(s => s.SetProperty(t => t.RevocadaEn, ahora), ct);
        // Impedir enviar enlaces antiguos tras un cambio de contraseña.
        await db.Set<CorreoAccesoPendiente>().Where(x => x.UsuarioId == u.Id && x.EnviadoEn == null).ExecuteDeleteAsync(ct);
        await db.SaveChangesAsync(ct);
    }
    public async Task VerificarCorreoAsync(string token, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var t = await BuscarTokenAsync(token, PropositoTokenCuenta.VerificarCorreo, ct);
        var u = await BloquearUsuarioAsync(t.UsuarioId, ct);
        await ValidarTokenBajoBloqueoAsync(t.Id, PropositoTokenCuenta.VerificarCorreo, u, ct);
        u.CorreoVerificado = true;
        await db.Set<TokenCuenta>().Where(x => x.UsuarioId == u.Id && x.Proposito == PropositoTokenCuenta.VerificarCorreo && x.UsadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsadoEn, DateTime.UtcNow), ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        await avisos.NotificarAsync(u.Id, TipoAviso.CorreoVerificado, "Verificaste tu correo", "Se confirmó que tienes acceso al correo de tu cuenta.");
    }
    private async Task<TokenCuenta> BuscarTokenAsync(string token, PropositoTokenCuenta proposito, CancellationToken ct) =>
        await db.Set<TokenCuenta>().AsNoTracking().SingleOrDefaultAsync(t => t.Hash == Hash(token) && t.Proposito == proposito && t.UsadoEn == null && t.ExpiraEn > DateTime.UtcNow, ct)
        ?? throw new BusinessException("El enlace no es válido o ha vencido. Solicita uno nuevo.");
    private Task<Usuario> BloquearUsuarioAsync(int id, CancellationToken ct) => db.Usuarios.FromSqlInterpolated($"SELECT * FROM usuarios WHERE id = {id} FOR UPDATE").SingleAsync(ct);
    private async Task ValidarTokenBajoBloqueoAsync(int id, PropositoTokenCuenta proposito, Usuario u, CancellationToken ct)
    {
        if (!u.Activo || !await db.Set<TokenCuenta>().AnyAsync(t => t.Id == id && t.Proposito == proposito && t.UsadoEn == null && t.ExpiraEn > DateTime.UtcNow, ct))
            throw new BusinessException("El enlace no es válido o ha vencido. Solicita uno nuevo.");
    }
    public async Task<SeguridadCuentaDto> EstadoAsync(int id, CancellationToken ct)
    {
        var u = await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == id, ct); return new(u.CorreoVerificado, u.Correo);
    }
    public Task<List<SesionCuentaDto>> SesionesAsync(int id, string actual, CancellationToken ct) =>
        db.Set<SesionCuenta>().AsNoTracking().Where(s => s.UsuarioId == id && s.RevocadaEn == null && s.ExpiraEn > DateTime.UtcNow)
            .OrderByDescending(s => s.CreadaEn).Select(s => new SesionCuentaDto(s.Id, s.CreadaEn, s.ExpiraEn, s.Dispositivo, s.Id == actual)).ToListAsync(ct);
    public async Task RevocarSesionAsync(int id, string sesion, CancellationToken ct)
    {
        if (await db.Set<SesionCuenta>().Where(s => s.UsuarioId == id && s.Id == sesion && s.RevocadaEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevocadaEn, DateTime.UtcNow), ct) == 0) throw BusinessException.NotFound("Sesión no encontrada.");
    }
    public async Task RevocarOtrasAsync(int id, string actual, CancellationToken ct)
    {
        await db.Set<SesionCuenta>().Where(s => s.UsuarioId == id && s.Id != actual && s.RevocadaEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevocadaEn, DateTime.UtcNow), ct);
        await avisos.NotificarAsync(id, TipoAviso.SesionesRevocadas, "Cerraste otras sesiones", "Revocaste el acceso de tus otros dispositivos.");
    }
}
