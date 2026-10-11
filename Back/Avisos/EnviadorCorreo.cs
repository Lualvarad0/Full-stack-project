using System.Threading.Channels;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Back.Avisos
{
    public class SmtpOptions
    {
        public const string Section = "Smtp";

        public string Host { get; set; } = string.Empty;
        public int Puerto { get; set; } = 587;
        public string Usuario { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
        public string Remitente { get; set; } = "Nudo <no-responder@localhost>";

        public bool Configurado => !string.IsNullOrWhiteSpace(Host);
    }

    public record CorreoSaliente(string Para, string Asunto, string Html, string Texto);

    /// <summary>Cola en memoria de correos por enviar: el envío no retrasa el login ni el guardado.</summary>
    public class ColaCorreoSaliente
    {
        private readonly Channel<CorreoSaliente> _canal = Channel.CreateBounded<CorreoSaliente>(
            new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest });

        public bool Encolar(CorreoSaliente correo) => _canal.Writer.TryWrite(correo);
        public ChannelReader<CorreoSaliente> Lector => _canal.Reader;
    }

    /// <summary>Envía los correos de la cola por SMTP. Sin SMTP configurado, solo los registra en el log.</summary>
    public class EnvioCorreosWorker : BackgroundService
    {
        private readonly ColaCorreoSaliente _cola;
        private readonly SmtpOptions _smtp;
        private readonly ILogger<EnvioCorreosWorker> _logger;

        public EnvioCorreosWorker(ColaCorreoSaliente cola, IOptions<SmtpOptions> smtp, ILogger<EnvioCorreosWorker> logger)
        {
            _cola = cola;
            _smtp = smtp.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var correo in _cola.Lector.ReadAllAsync(stoppingToken))
            {
                if (!_smtp.Configurado)
                {
                    _logger.LogInformation("Aviso por correo (SMTP no configurado) para {Para}: {Asunto}", correo.Para, correo.Asunto);
                    continue;
                }

                try
                {
                    var mensaje = new MimeMessage();
                    mensaje.From.Add(MailboxAddress.Parse(_smtp.Remitente));
                    mensaje.To.Add(MailboxAddress.Parse(correo.Para));
                    mensaje.Subject = correo.Asunto;
                    mensaje.Body = new BodyBuilder { HtmlBody = correo.Html, TextBody = correo.Texto }.ToMessageBody();

                    using var cliente = new SmtpClient { Timeout = 20_000 };
                    await cliente.ConnectAsync(_smtp.Host, _smtp.Puerto, SecureSocketOptions.StartTlsWhenAvailable, stoppingToken);
                    if (!string.IsNullOrEmpty(_smtp.Usuario))
                        await cliente.AuthenticateAsync(_smtp.Usuario, _smtp.Clave, stoppingToken);
                    await cliente.SendAsync(mensaje, stoppingToken);
                    await cliente.DisconnectAsync(true, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "No se pudo enviar el aviso por correo a {Para}", correo.Para);
                }
            }
        }
    }
}
