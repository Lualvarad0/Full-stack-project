using System.Globalization;
using System.Text.Json;
using Back.Avisos;
using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Back.Pagos
{
    /// <summary>Suscripciones a los planes de pago y su sincronización con Kushki (alta, cancelación y webhooks).</summary>
    public class SuscripcionService
    {
        private readonly AppDbContext _context;
        private readonly KushkiClient _kushki;
        private readonly KushkiOptions _options;
        private readonly AvisoService _avisos;
        private readonly ILogger<SuscripcionService> _logger;

        public SuscripcionService(AppDbContext context, KushkiClient kushki, IOptions<KushkiOptions> options, AvisoService avisos, ILogger<SuscripcionService> logger)
        {
            _context = context;
            _kushki = kushki;
            _options = options.Value;
            _avisos = avisos;
            _logger = logger;
        }

        public CatalogoPagosDto Catalogo() => new(
            CatalogoPlanes.Todos().Select(PrecioPlanDto.From).ToList(),
            CatalogoPlanes.DiasDePrueba,
            _options.Configurado,
            _options.Configurado ? _options.ClavePublica : null,
            _options.Pruebas);

        public async Task<UsoPlanDto> UsoAsync(int usuarioId)
        {
            var usuario = await UsuarioAsync(usuarioId);
            var inicioMes = AccesoPlan.InicioMesUtc();
            var usados = await _context.Comprobantes.CountAsync(c => c.UsuarioId == usuarioId && c.FechaImportacion >= inicioMes);
            var profesional = AccesoPlan.TieneProfesional(usuario);
            return new UsoPlanDto(usuario.Plan, profesional, profesional ? null : AccesoPlan.LimiteComprobantesPersonal, usados,
                profesional ? null : AccesoPlan.MesesHistorialPersonal);
        }

        public async Task<SuscripcionDto?> ObtenerAsync(int usuarioId) =>
            await VigenteAsync(usuarioId) is { } s ? SuscripcionDto.From(s) : null;

        /// <summary>Crea la suscripción en Kushki con días de prueba gratis y activa el plan de inmediato.</summary>
        public async Task<SuscripcionResultadoDto> SuscribirAsync(int usuarioId, SuscribirRequest request, CancellationToken ct)
        {
            if (!CatalogoPlanes.EsDePago(request.Plan))
                throw new BusinessException("El plan Personal es gratis: no necesita pago.");
            var usuario = await UsuarioAsync(usuarioId);
            if (await VigenteAsync(usuarioId) is { } actual)
                throw BusinessException.Conflict($"Ya tienes el plan {actual.Plan}. Cancélalo antes de cambiar de plan.");

            var precio = CatalogoPlanes.Precio(request.Plan, request.Periodicidad);
            var primerCobro = HoyEcuador().AddDays(CatalogoPlanes.DiasDePrueba);
            var kushkiId = await _kushki.CrearSuscripcionAsync(
                request.Token,
                precio,
                new ContactoKushki(usuario.Nombre, string.IsNullOrWhiteSpace(usuario.Apellido) ? usuario.Nombre : usuario.Apellido, usuario.Correo),
                primerCobro,
                new Dictionary<string, string> { ["usuarioId"] = usuarioId.ToString(CultureInfo.InvariantCulture), ["plan"] = request.Plan.ToString() },
                ct);

            var suscripcion = new Suscripcion
            {
                UsuarioId = usuarioId,
                Plan = request.Plan,
                Periodicidad = request.Periodicidad,
                KushkiId = kushkiId,
                Subtotal = precio.Subtotal,
                Iva = precio.Iva,
                Total = precio.Total,
                ProximoCobro = primerCobro,
                MarcaTarjeta = request.MarcaTarjeta,
                UltimosDigitos = request.UltimosDigitos,
            };
            _context.Suscripciones.Add(suscripcion);
            usuario.Plan = request.Plan;
            await _context.SaveChangesAsync(CancellationToken.None);

            await _avisos.NotificarAsync(usuarioId, TipoAviso.PlanCambiado, $"Te suscribiste al plan {request.Plan}",
                $"Tienes {CatalogoPlanes.DiasDePrueba} días gratis. El primer cobro de ${precio.Total.ToString("0.00", CultureInfo.InvariantCulture)} " +
                $"({(request.Periodicidad == Periodicidad.Anual ? "anual" : "mensual")}, IVA incluido) será el {primerCobro:dd/MM/yyyy}.");
            return new SuscripcionResultadoDto(UsuarioDto.From(usuario), SuscripcionDto.From(suscripcion));
        }

        /// <summary>Cancela en Kushki (no habrá más cobros) y vuelve al plan Personal.</summary>
        public async Task<SuscripcionResultadoDto> CancelarAsync(int usuarioId, CancellationToken ct)
        {
            var usuario = await UsuarioAsync(usuarioId);
            var suscripcion = await VigenteAsync(usuarioId) ?? throw BusinessException.NotFound("No tienes una suscripción activa.");
            await _kushki.CancelarAsync(suscripcion.KushkiId, ct);
            await TerminarAsync(suscripcion, usuario, "Cancelaste tu plan", $"Cancelaste el plan {suscripcion.Plan}: no habrá más cobros y vuelves al plan Personal.");
            return new SuscripcionResultadoDto(UsuarioDto.From(usuario), null);
        }

        /// <summary>Procesa un webhook de Kushki ya verificado. Devuelve false si no corresponde a ninguna suscripción.</summary>
        public async Task<bool> ProcesarWebhookAsync(string cuerpo)
        {
            using var documento = JsonDocument.Parse(cuerpo);
            var raiz = documento.RootElement;
            var nombre = Texto(raiz, "name");
            if (!raiz.TryGetProperty("event", out var evento)) return false;
            var kushkiId = Texto(evento, "subscriptionId");
            if (kushkiId is null) return false;

            var suscripcion = await _context.Suscripciones.FirstOrDefaultAsync(s => s.KushkiId == kushkiId);
            if (suscripcion is null)
            {
                _logger.LogWarning("Webhook de Kushki {Evento} para una suscripción desconocida {Id}", nombre, kushkiId);
                return false;
            }
            var usuario = await UsuarioAsync(suscripcion.UsuarioId);
            var ticket = Texto(evento, "ticketNumber");
            var monto = Monto(evento);

            switch (nombre)
            {
                // Kushki escribe "succesfullCharge"; se acepta también la forma correcta por si lo corrigen.
                case "succesfullCharge" or "successfulCharge":
                    if (ticket is not null && await _context.PagosSuscripcion.AnyAsync(p => p.Ticket == ticket)) return true;
                    _context.PagosSuscripcion.Add(new PagoSuscripcion { SuscripcionId = suscripcion.Id, Monto = monto ?? suscripcion.Total, Aprobado = true, Ticket = ticket });
                    suscripcion.Estado = EstadoSuscripcion.Activa;
                    suscripcion.UltimoPagoEn = DateTime.UtcNow;
                    suscripcion.ProximoCobro = (suscripcion.Periodicidad == Periodicidad.Anual
                        ? HoyEcuador().AddYears(1) : HoyEcuador().AddMonths(1));
                    usuario.Plan = suscripcion.Plan;
                    await _context.SaveChangesAsync();
                    await _avisos.NotificarAsync(usuario.Id, TipoAviso.PlanCambiado, "Recibimos tu pago",
                        $"Se cobraron ${(monto ?? suscripcion.Total).ToString("0.00", CultureInfo.InvariantCulture)} por el plan {suscripcion.Plan}. Próximo cobro: {suscripcion.ProximoCobro:dd/MM/yyyy}.");
                    break;

                case "declinedCharge" or "failedRetry":
                    _context.PagosSuscripcion.Add(new PagoSuscripcion { SuscripcionId = suscripcion.Id, Monto = monto ?? suscripcion.Total, Aprobado = false, Ticket = ticket, Detalle = Texto(raiz, "responseText") });
                    suscripcion.Estado = EstadoSuscripcion.PagoPendiente;
                    await _context.SaveChangesAsync();
                    await _avisos.NotificarAsync(usuario.Id, TipoAviso.PlanCambiado, "No pudimos cobrar tu plan",
                        "El cobro con tu tarjeta fue rechazado. Lo intentaremos de nuevo; revisa que tu tarjeta tenga fondos y esté vigente.");
                    break;

                case "lastRetry":
                    await TerminarAsync(suscripcion, usuario, "Tu plan se canceló por falta de pago",
                        $"No pudimos cobrar el plan {suscripcion.Plan} después de varios intentos. Volviste al plan Personal; puedes suscribirte de nuevo cuando quieras.");
                    break;

                case "subscriptionDelete":
                    if (suscripcion.Estado != EstadoSuscripcion.Cancelada)
                        await TerminarAsync(suscripcion, usuario, "Tu plan se canceló", $"La suscripción al plan {suscripcion.Plan} se canceló. Volviste al plan Personal.");
                    break;
            }
            return true;
        }

        private async Task TerminarAsync(Suscripcion suscripcion, Usuario usuario, string titulo, string detalle)
        {
            suscripcion.Estado = EstadoSuscripcion.Cancelada;
            suscripcion.CanceladaEn = DateTime.UtcNow;
            usuario.Plan = PlanSuscripcion.Personal;
            await _context.SaveChangesAsync();
            await _avisos.NotificarAsync(usuario.Id, TipoAviso.PlanCambiado, titulo, detalle);
        }

        private Task<Suscripcion?> VigenteAsync(int usuarioId) =>
            _context.Suscripciones
                .Where(s => s.UsuarioId == usuarioId && s.Estado != EstadoSuscripcion.Cancelada)
                .OrderByDescending(s => s.CreadaEn)
                .FirstOrDefaultAsync();

        private async Task<Usuario> UsuarioAsync(int usuarioId) =>
            await _context.Usuarios.FindAsync(usuarioId) ?? throw BusinessException.NotFound("Usuario no encontrado.");

        private static DateOnly HoyEcuador() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));

        private static string? Texto(JsonElement elemento, string propiedad) =>
            elemento.ValueKind == JsonValueKind.Object && elemento.TryGetProperty(propiedad, out var valor)
                ? valor.ValueKind == JsonValueKind.String ? valor.GetString() : valor.ToString()
                : null;

        private static decimal? Monto(JsonElement evento)
        {
            if (!evento.TryGetProperty("amount", out var amount) || amount.ValueKind != JsonValueKind.Object) return null;
            decimal Parte(string nombre) => amount.TryGetProperty(nombre, out var v) && v.TryGetDecimal(out var d) ? d : 0m;
            return Parte("subtotalIva") + Parte("subtotalIva0") + Parte("iva") + Parte("ice");
        }
    }
}
