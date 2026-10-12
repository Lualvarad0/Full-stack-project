using System.Text.Json;
using Back.Avisos;
using Back.Data;
using Back.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Back.Seguridad;

public interface ITransporteCorreoAcceso { Task EnviarAsync(CorreoSaliente correo, CancellationToken ct); }
public class TransporteCorreoAcceso(IOptions<SmtpOptions> opciones) : ITransporteCorreoAcceso
{
    public async Task EnviarAsync(CorreoSaliente correo, CancellationToken ct)
    {
        var smtp = opciones.Value;
        var mensaje = new MimeMessage(); mensaje.From.Add(MailboxAddress.Parse(smtp.Remitente)); mensaje.To.Add(MailboxAddress.Parse(correo.Para));
        mensaje.Subject = correo.Asunto; mensaje.Body = new BodyBuilder { HtmlBody = correo.Html, TextBody = correo.Texto }.ToMessageBody();
        using var cliente = new SmtpClient { Timeout = 20_000 };
        await cliente.ConnectAsync(smtp.Host, smtp.Puerto, smtp.Puerto == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
        if (smtp.Usuario.Length > 0) await cliente.AuthenticateAsync(smtp.Usuario, smtp.Clave, ct);
        await cliente.SendAsync(mensaje, ct); await cliente.DisconnectAsync(true, ct);
    }
}
public class ProcesadorCorreoAcceso(AppDbContext db, IDataProtectionProvider proteccion, ITransporteCorreoAcceso transporte, ILogger<ProcesadorCorreoAcceso> logger)
{
    public async Task<bool> ProcesarUnoAsync(CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var ahora = DateTime.UtcNow;
        var fila = (await db.Set<CorreoAccesoPendiente>().FromSqlInterpolated($"SELECT * FROM correos_acceso_pendientes WHERE enviado_en IS NULL AND intentos < 5 AND proximo_intento_en <= {ahora} ORDER BY id LIMIT 1 FOR UPDATE SKIP LOCKED").ToListAsync(ct)).FirstOrDefault();
        if (fila is null) return false;
        if (fila.ExpiraEn <= ahora) { db.Remove(fila); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return true; }
        try
        {
            var texto = proteccion.CreateProtector(SeguridadCuentaService.ProtectorCorreo).Unprotect(fila.ContenidoCifrado);
            var correo = JsonSerializer.Deserialize<CorreoSaliente>(texto)!;
            await transporte.EnviarAsync(correo, ct);
            // Borrar el mensaje sensible después de enviarlo; el hash del token permanece independiente.
            db.Remove(fila);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            fila.Intentos++; fila.ProximoIntentoEn = ahora.AddMinutes(Math.Min(15, Math.Pow(2, fila.Intentos)));
            logger.LogWarning("No se pudo enviar el correo de acceso {Id}; intento {Intento}. Tipo de fallo: {Tipo}", fila.Id, fila.Intentos, ex.GetType().Name);
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return true;
    }
}
public class CorreoAccesoWorker(IServiceScopeFactory scopes, IOptions<SmtpOptions> smtp, IConfiguration config, ILogger<CorreoAccesoWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue("Acceso:ProcesarCorreos", true)) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (smtp.Value.Configurado) { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<ProcesadorCorreoAcceso>().ProcesarUnoAsync(stoppingToken); }
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogWarning("El procesador de correo de acceso requiere atención: {Tipo}", ex.GetType().Name); }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
