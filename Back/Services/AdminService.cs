using Back.Avisos;
using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Back.Pagos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Back.Services
{
    /// <summary>Panel de superadministrador: métricas del negocio, usuarios, pagos y estado del sistema.</summary>
    public class AdminService
    {
        private readonly AppDbContext _context;
        private readonly SuscripcionService _suscripciones;
        private readonly AvisoService _avisos;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _entorno;
        private readonly KushkiOptions _kushki;
        private readonly SmtpOptions _smtp;

        public AdminService(AppDbContext context, SuscripcionService suscripciones, AvisoService avisos, IConfiguration configuration,
            IWebHostEnvironment entorno, IOptions<KushkiOptions> kushki, IOptions<SmtpOptions> smtp)
        {
            _context = context;
            _suscripciones = suscripciones;
            _avisos = avisos;
            _configuration = configuration;
            _entorno = entorno;
            _kushki = kushki.Value;
            _smtp = smtp.Value;
        }

        public async Task<MetricasAdminDto> MetricasAsync()
        {
            var ahora = DateTime.UtcNow;
            var hace30 = ahora.AddDays(-30);
            var inicioMes = AccesoPlan.InicioMesUtc();
            var desde6Meses = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);

            var porPlan = await _context.Usuarios.GroupBy(u => u.Plan).Select(g => new { g.Key, Total = g.Count() }).ToListAsync();
            var vigentes = await _context.Suscripciones.Where(s => s.Estado != EstadoSuscripcion.Cancelada).ToListAsync();
            var registros = await _context.Usuarios.Where(u => u.FechaCreacion >= desde6Meses)
                .GroupBy(u => new { u.FechaCreacion.Year, u.FechaCreacion.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Count() })
                .ToListAsync();

            return new MetricasAdminDto(
                await _context.Usuarios.CountAsync(),
                await _context.Usuarios.CountAsync(u => !u.Activo),
                await _context.Usuarios.CountAsync(u => u.FechaCreacion >= hace30),
                await _context.Usuarios.CountAsync(u => u.UltimoIngresoEn >= hace30),
                Enum.GetValues<PlanSuscripcion>().ToDictionary(p => p, p => porPlan.FirstOrDefault(x => x.Key == p)?.Total ?? 0),
                vigentes.Count(s => s.Estado == EstadoSuscripcion.EnPrueba),
                vigentes.Count(s => s.Estado == EstadoSuscripcion.Activa),
                vigentes.Count(s => s.Estado == EstadoSuscripcion.PagoPendiente),
                vigentes.Where(s => s.Estado is EstadoSuscripcion.Activa or EstadoSuscripcion.PagoPendiente)
                    .Sum(s => s.Periodicidad == Periodicidad.Anual ? Math.Round(s.Subtotal / 12m, 2) : s.Subtotal),
                await _context.PagosSuscripcion.Where(p => p.Aprobado && p.Fecha >= hace30).SumAsync(p => (decimal?)p.Monto) ?? 0m,
                await _context.FacturasEmitidas.CountAsync(),
                await _context.FacturasEmitidas.CountAsync(f => f.Estado == EstadoFactura.Autorizada),
                await _context.Comprobantes.CountAsync(),
                await _context.Comprobantes.CountAsync(c => c.FechaImportacion >= inicioMes),
                Enumerable.Range(0, 6).Select(i => desde6Meses.AddMonths(i))
                    .Select(m => new RegistrosMesDto(m.Year, m.Month, registros.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month)?.Total ?? 0))
                    .ToList());
        }

        public async Task<List<UsuarioAdminDto>> UsuariosAsync(string? busqueda, PlanSuscripcion? plan)
        {
            var consulta = _context.Usuarios.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var patron = $"%{busqueda.Trim()}%";
                consulta = consulta.Where(u => EF.Functions.ILike(u.Nombre + " " + u.Apellido, patron) || EF.Functions.ILike(u.Correo, patron) || (u.Ruc != null && u.Ruc.Contains(busqueda.Trim())));
            }
            if (plan is not null) consulta = consulta.Where(u => u.Plan == plan);

            return await consulta
                .OrderByDescending(u => u.FechaCreacion)
                .Take(500)
                .Select(u => new UsuarioAdminDto(
                    u.Id,
                    (u.Nombre + " " + u.Apellido).Trim(),
                    u.Correo,
                    u.Rol,
                    u.Plan,
                    u.Activo,
                    u.FechaCreacion,
                    u.UltimoIngresoEn,
                    u.Ruc,
                    _context.Suscripciones.Where(s => s.UsuarioId == u.Id && s.Estado != EstadoSuscripcion.Cancelada)
                        .OrderByDescending(s => s.CreadaEn).Select(s => (EstadoSuscripcion?)s.Estado).FirstOrDefault(),
                    _context.Comprobantes.Count(c => c.UsuarioId == u.Id),
                    _context.FacturasEmitidas.Count(f => f.UsuarioId == u.Id)))
                .ToListAsync();
        }

        /// <summary>Plan de cortesía: cambia el plan sin cobrar (no toca suscripciones pagadas).</summary>
        public async Task CambiarPlanAsync(int adminId, int usuarioId, PlanSuscripcion plan)
        {
            var usuario = await UsuarioAsync(usuarioId);
            if (await _context.Suscripciones.AnyAsync(s => s.UsuarioId == usuarioId && s.Estado != EstadoSuscripcion.Cancelada))
                throw BusinessException.Conflict("Este usuario tiene una suscripción pagada con Kushki. Cancélala antes de cambiarle el plan.");
            if (usuario.Plan == plan) return;
            var anterior = usuario.Plan;
            usuario.Plan = plan;
            await _context.SaveChangesAsync();
            await _avisos.NotificarAsync(usuarioId, TipoAviso.PlanCambiado, "Tu plan cambió", $"El equipo de Nudo cambió tu plan de {anterior} a {plan}.");
        }

        public async Task CambiarEstadoAsync(int adminId, int usuarioId, bool activo)
        {
            if (usuarioId == adminId) throw new BusinessException("No puedes suspender tu propia cuenta.");
            var usuario = await UsuarioAsync(usuarioId);
            usuario.Activo = activo;
            await _context.SaveChangesAsync();
        }

        public async Task CancelarSuscripcionAsync(int usuarioId, CancellationToken ct) => await _suscripciones.CancelarAsync(usuarioId, ct);

        /// <summary>Elimina la cuenta y todos sus datos (cancela antes su suscripción en Kushki para que no se le cobre).</summary>
        public async Task EliminarUsuarioAsync(int adminId, int usuarioId, CancellationToken ct)
        {
            if (usuarioId == adminId) throw new BusinessException("No puedes eliminar tu propia cuenta desde el panel.");
            var usuario = await UsuarioAsync(usuarioId);
            if (await _context.Set<AuditoriaContable>().AnyAsync(a => a.UsuarioId == usuarioId, ct)
                || await _context.Set<PeriodoContable>().AnyAsync(p => p.UsuarioId == usuarioId, ct))
                throw BusinessException.Conflict("La cuenta tiene historial contable que debe conservarse. Suspéndela en lugar de eliminarla.");
            if (await _context.Facturas.AnyAsync(f => f.UsuarioId == usuarioId || f.Cliente.UsuarioId == usuarioId ||
                f.Detalles.Any(d => d.Producto.UsuarioId == usuarioId), ct))
                throw BusinessException.Conflict("La cuenta tiene facturas del catálogo antiguo vinculadas. Revisa esas referencias antes de eliminarla.");
            if (await _context.Suscripciones.AnyAsync(s => s.UsuarioId == usuarioId && s.Estado != EstadoSuscripcion.Cancelada, ct))
                await _suscripciones.CancelarAsync(usuarioId, ct);
            await using var transaccion = await _context.Database.BeginTransactionAsync(ct);
            await _context.Productos.Where(p => p.UsuarioId == usuarioId).ExecuteDeleteAsync(ct);
            await _context.Clientes.Where(c => c.UsuarioId == usuarioId).ExecuteDeleteAsync(ct);
            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
        }

        public Task<List<AvisoCuentaDto>> ActividadAsync(int usuarioId) => _avisos.ObtenerRecientesAsync(usuarioId);

        public async Task<List<PagoAdminDto>> PagosAsync() =>
            await (from p in _context.PagosSuscripcion
                   join s in _context.Suscripciones on p.SuscripcionId equals s.Id
                   join u in _context.Usuarios on s.UsuarioId equals u.Id
                   orderby p.Fecha descending
                   select new PagoAdminDto(p.Id, p.Fecha, (u.Nombre + " " + u.Apellido).Trim(), u.Correo, s.Plan, p.Monto, p.Aprobado, p.Ticket, p.Detalle))
                .Take(200)
                .ToListAsync();

        public async Task<EstadoSistemaDto> SistemaAsync() => new(
            _entorno.EnvironmentName,
            _kushki.Configurado,
            _kushki.Pruebas,
            _kushki.FirmaWebhook.Length > 0,
            _smtp.Configurado,
            _configuration["Correo:DominioEntrante"] is { Length: > 0 } dominio ? dominio : null,
            _configuration["DataProtection:RutaClaves"] is { Length: > 0 },
            (await _context.Database.GetPendingMigrationsAsync()).Count(),
            DateTime.UtcNow);

        private async Task<Usuario> UsuarioAsync(int usuarioId) =>
            await _context.Usuarios.FindAsync(usuarioId) ?? throw BusinessException.NotFound("Usuario no encontrado.");
    }
}
