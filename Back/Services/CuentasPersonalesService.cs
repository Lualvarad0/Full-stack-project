using System.Globalization;
using System.Text;
using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Back.Pagos;
using Microsoft.EntityFrameworkCore;

namespace Back.Services;

/// <summary>
/// Registro manual de cuentas del mes (todos los planes): el usuario dice cuánto gana, anota sus cuentas
/// y marca cada una con el día en que la pagó. En los planes de pago, al terminar el mes pregunta por las
/// cuentas que quedaron pendientes (pagada o pasa al mes siguiente) y busca la factura o el correo que respalde el pago.
/// </summary>
public class CuentasPersonalesService(AppDbContext db)
{
    public const int MaxCuentasPorMes = CuentasLimites.MaxCuentasPorMes;

    private static readonly string[] NombresMes =
        ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    public async Task<MesCuentasDto> MesAsync(int usuarioId, int anio, int mes, CancellationToken ct)
    {
        ValidarMes(anio, mes);
        var cuentas = await db.CuentasPersonales.AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId && c.Anio == anio && c.Mes == mes)
            .OrderBy(c => c.PagadaEl != null || c.TrasladadaACuentaId != null).ThenBy(c => c.DiaVencimiento ?? 32).ThenBy(c => c.Nombre)
            .ToListAsync(ct);
        var ingreso = await IngresoDelMesAsync(usuarioId, anio, mes, ct);
        var total = cuentas.Sum(c => c.Monto);
        var pagado = cuentas.Where(c => c.PagadaEl != null).Sum(c => c.Monto);
        var trasladado = cuentas.Where(c => c.PagadaEl == null && c.TrasladadaACuentaId != null).Sum(c => c.Monto);
        return new MesCuentasDto(anio, mes, ingreso, total, pagado, total - pagado - trasladado, ingreso - total,
            cuentas.Select(CuentaPersonalDto.From).ToList(), trasladado);
    }

    public async Task<CuentaPersonalDto> CrearAsync(int usuarioId, CuentaPersonalRequest r, CancellationToken ct)
    {
        ValidarMes(r.Anio, r.Mes);
        if (await db.CuentasPersonales.CountAsync(c => c.UsuarioId == usuarioId && c.Anio == r.Anio && c.Mes == r.Mes, ct) >= MaxCuentasPorMes)
            throw new BusinessException($"Puedes registrar hasta {MaxCuentasPorMes} cuentas por mes.");
        var cuenta = new CuentaPersonal { UsuarioId = usuarioId, Anio = r.Anio, Mes = r.Mes };
        Aplicar(cuenta, r);
        db.CuentasPersonales.Add(cuenta);
        await db.SaveChangesAsync(ct);
        return CuentaPersonalDto.From(cuenta);
    }

    public async Task<CuentaPersonalDto> EditarAsync(int usuarioId, int id, CuentaPersonalRequest r, CancellationToken ct)
    {
        var cuenta = await BuscarAsync(usuarioId, id, ct);
        if (cuenta.Anio != r.Anio || cuenta.Mes != r.Mes) throw new BusinessException("La cuenta pertenece a otro mes.");
        Aplicar(cuenta, r);
        await db.SaveChangesAsync(ct);
        return CuentaPersonalDto.From(cuenta);
    }

    public async Task EliminarAsync(int usuarioId, int id, CancellationToken ct)
    {
        db.CuentasPersonales.Remove(await BuscarAsync(usuarioId, id, ct));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Marca la cuenta como pagada el día indicado (no puede ser futuro).</summary>
    public async Task<CuentaPersonalDto> PagarAsync(int usuarioId, int id, DateOnly fecha, CancellationToken ct)
    {
        var cuenta = await BuscarAsync(usuarioId, id, ct);
        if (cuenta.TrasladadaACuentaId is not null) throw new BusinessException("Esta cuenta pasó al mes siguiente: márcala pagada en ese mes.");
        ValidarFechaPago(cuenta, fecha);
        cuenta.PagadaEl = fecha;
        cuenta.Respaldo = RespaldoPago.Manual;
        cuenta.ComprobanteId = null;
        await db.SaveChangesAsync(ct);
        return CuentaPersonalDto.From(cuenta);
    }

    public async Task<CuentaPersonalDto> QuitarPagoAsync(int usuarioId, int id, CancellationToken ct)
    {
        var cuenta = await BuscarAsync(usuarioId, id, ct);
        cuenta.PagadaEl = null;
        cuenta.Respaldo = null;
        cuenta.ComprobanteId = null;
        await db.SaveChangesAsync(ct);
        return CuentaPersonalDto.From(cuenta);
    }

    public async Task GuardarIngresoAsync(int usuarioId, IngresoMensualRequest r, CancellationToken ct)
    {
        ValidarMes(r.Anio, r.Mes);
        var ingreso = await db.IngresosMensuales.SingleOrDefaultAsync(i => i.UsuarioId == usuarioId && i.Anio == r.Anio && i.Mes == r.Mes, ct);
        if (ingreso is null) db.IngresosMensuales.Add(ingreso = new IngresoMensual { UsuarioId = usuarioId, Anio = r.Anio, Mes = r.Mes });
        ingreso.Monto = Math.Round(r.Monto, 2, MidpointRounding.AwayFromZero);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Trae al mes las cuentas del mes anterior que todavía no estén (por nombre), sin marcar como pagadas.</summary>
    public async Task<int> CopiarMesAnteriorAsync(int usuarioId, int anio, int mes, CancellationToken ct)
    {
        ValidarMes(anio, mes);
        var anterior = new DateOnly(anio, mes, 1).AddMonths(-1);
        // Las deudas que pasaron de un mes a otro no se repiten: ya están en este mes como trasladadas.
        var origen = await db.CuentasPersonales.AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId && c.Anio == anterior.Year && c.Mes == anterior.Month && c.TrasladadaDesdeId == null)
            .ToListAsync(ct);
        var actuales = await db.CuentasPersonales
            .Where(c => c.UsuarioId == usuarioId && c.Anio == anio && c.Mes == mes).Select(c => c.Nombre.ToLower()).ToListAsync(ct);
        var nuevas = origen.Where(c => !actuales.Contains(c.Nombre.ToLower()))
            .Take(MaxCuentasPorMes - actuales.Count)
            .Select(c => new CuentaPersonal
            {
                UsuarioId = usuarioId, Anio = anio, Mes = mes, Nombre = c.Nombre, Categoria = c.Categoria, Monto = c.Monto,
                DiaVencimiento = c.DiaVencimiento,
            }).ToList();
        db.CuentasPersonales.AddRange(nuevas);
        await db.SaveChangesAsync(ct);
        return nuevas.Count;
    }

    /// <summary>
    /// Cierre del mes anterior: las cuentas que quedaron pendientes, cada una con la factura o el correo que parece
    /// respaldar su pago. null en el plan gratuito (el cierre es de los planes de pago).
    /// </summary>
    public async Task<CierreMesDto?> CierrePendienteAsync(int usuarioId, CancellationToken ct)
    {
        var usuario = await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == usuarioId, ct);
        if (!AccesoPlan.TieneProfesional(usuario)) return null;
        var hoy = HoyEcuador();
        var anterior = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(-1);
        return await CierreAsync(usuarioId, anterior.Year, anterior.Month, ct);
    }

    private async Task<CierreMesDto> CierreAsync(int usuarioId, int anio, int mes, CancellationToken ct)
    {
        var pendientes = await db.CuentasPersonales.AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId && c.Anio == anio && c.Mes == mes && c.PagadaEl == null && c.TrasladadaACuentaId == null)
            .OrderBy(c => c.DiaVencimiento ?? 32).ThenBy(c => c.Nombre)
            .ToListAsync(ct);
        if (pendientes.Count == 0) return new CierreMesDto(anio, mes, []);

        // Respaldo posible: facturas de gasto y correos desde el inicio del mes hasta hoy (a veces se paga con atraso).
        var inicio = new DateOnly(anio, mes, 1);
        var usadas = db.CuentasPersonales.Where(c => c.UsuarioId == usuarioId && c.ComprobanteId != null).Select(c => c.ComprobanteId!.Value);
        var facturas = await db.Comprobantes.AsNoTracking()
            .Where(f => f.UsuarioId == usuarioId && f.Direccion == Direccion.Recibido && f.FechaEmision >= inicio && !usadas.Contains(f.Id))
            .OrderBy(f => f.FechaEmision).Take(500)
            .ToListAsync(ct);
        var desdeUtc = DateTime.SpecifyKind(inicio.ToDateTime(TimeOnly.MinValue).AddHours(5), DateTimeKind.Utc);
        var correos = await db.CorreosRecibidos.AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId && c.RecibidoEn >= desdeUtc)
            .OrderBy(c => c.RecibidoEn).Take(500)
            .ToListAsync(ct);

        return new CierreMesDto(anio, mes,
            pendientes.Select(c => new CuentaCierreDto(CuentaPersonalDto.From(c), SugerirRespaldo(c, facturas, correos))).ToList());
    }

    /// <summary>Aplica lo que el usuario decidió para cada cuenta pendiente del mes que terminó.</summary>
    public async Task<CierreMesResultadoDto> CerrarMesAsync(int usuarioId, CierreMesRequest r, CancellationToken ct)
    {
        var usuario = await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == usuarioId, ct);
        AccesoPlan.ExigirProfesional(usuario, "El cierre de tus cuentas del mes y el traslado de deudas");
        ValidarMes(r.Anio, r.Mes);
        if (new DateOnly(r.Anio, r.Mes, 1).AddMonths(1) > HoyEcuador()) throw new BusinessException("Ese mes todavía no termina.");
        if (r.Decisiones.Select(d => d.CuentaId).Distinct().Count() != r.Decisiones.Count)
            throw new BusinessException("Cada cuenta va una sola vez en el cierre.");

        var ids = r.Decisiones.Select(d => d.CuentaId).ToList();
        var cuentas = await db.CuentasPersonales
            .Where(c => c.UsuarioId == usuarioId && ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        var siguiente = new DateOnly(r.Anio, r.Mes, 1).AddMonths(1);
        var enSiguiente = await db.CuentasPersonales.CountAsync(c => c.UsuarioId == usuarioId && c.Anio == siguiente.Year && c.Mes == siguiente.Month, ct);
        var trasladadas = new List<(CuentaPersonal Origen, CuentaPersonal Copia)>();
        var pagadas = 0;

        foreach (var d in r.Decisiones)
        {
            if (!cuentas.TryGetValue(d.CuentaId, out var cuenta)) throw BusinessException.NotFound("Cuenta no encontrada.");
            if (cuenta.Anio != r.Anio || cuenta.Mes != r.Mes) throw new BusinessException($"\"{cuenta.Nombre}\" pertenece a otro mes.");
            if (!cuenta.Pendiente) throw new BusinessException($"\"{cuenta.Nombre}\" ya no está pendiente.");

            if (d.Accion == AccionCierre.Pagada)
            {
                await MarcarPagadaAlCierreAsync(usuarioId, cuenta, d, ct);
                pagadas++;
                continue;
            }

            if (enSiguiente + trasladadas.Count >= MaxCuentasPorMes)
                throw new BusinessException($"El mes siguiente ya tiene {MaxCuentasPorMes} cuentas: no caben más deudas.");
            var etiqueta = $" (de {NombresMes[r.Mes - 1]})";
            var copia = new CuentaPersonal
            {
                UsuarioId = usuarioId, Anio = siguiente.Year, Mes = siguiente.Month,
                Nombre = cuenta.Nombre.Length + etiqueta.Length <= 120 ? cuenta.Nombre + etiqueta : cuenta.Nombre,
                Categoria = cuenta.Categoria, Monto = cuenta.Monto, DiaVencimiento = cuenta.DiaVencimiento,
                TarjetaCreditoId = cuenta.TarjetaCreditoId, TrasladadaDesdeId = cuenta.Id,
            };
            db.CuentasPersonales.Add(copia);
            trasladadas.Add((cuenta, copia));
        }

        // Dos pasos: la cuenta nueva necesita su Id para enlazarla con la original.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.SaveChangesAsync(ct);
        foreach (var (origen, copia) in trasladadas) origen.TrasladadaACuentaId = copia.Id;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new CierreMesResultadoDto(pagadas, trasladadas.Count);
    }

    private async Task MarcarPagadaAlCierreAsync(int usuarioId, CuentaPersonal cuenta, DecisionCierreRequest d, CancellationToken ct)
    {
        if (d.ComprobanteId is { } comprobanteId)
        {
            var factura = await db.Comprobantes.AsNoTracking()
                .SingleOrDefaultAsync(f => f.Id == comprobanteId && f.UsuarioId == usuarioId && f.Direccion == Direccion.Recibido, ct)
                ?? throw BusinessException.NotFound("Factura no encontrada.");
            ValidarFechaPago(cuenta, factura.FechaEmision);
            (cuenta.PagadaEl, cuenta.Respaldo, cuenta.ComprobanteId) = (factura.FechaEmision, RespaldoPago.Factura, factura.Id);
            return;
        }

        if (d.CorreoId is { } correoId)
        {
            var correo = await db.CorreosRecibidos.AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == correoId && c.UsuarioId == usuarioId, ct)
                ?? throw BusinessException.NotFound("Correo no encontrado.");
            var fechaCorreo = DateOnly.FromDateTime(correo.RecibidoEn.AddHours(-5));
            ValidarFechaPago(cuenta, fechaCorreo);
            (cuenta.PagadaEl, cuenta.Respaldo, cuenta.ComprobanteId) = (fechaCorreo, RespaldoPago.Correo, null);
            return;
        }

        // Sin factura ni correo que lo respalde, el usuario dice qué día pagó.
        var fecha = d.Fecha ?? throw new BusinessException($"Indica el día en que pagaste \"{cuenta.Nombre}\".");
        ValidarFechaPago(cuenta, fecha);
        (cuenta.PagadaEl, cuenta.Respaldo, cuenta.ComprobanteId) = (fecha, RespaldoPago.Manual, null);
    }

    /// <summary>
    /// La factura o el correo que mejor parece respaldar el pago: una factura del proveedor (por nombre) con un valor
    /// parecido, o con el valor exacto; si no hay factura, un correo cuyo asunto o remitente nombre la cuenta.
    /// </summary>
    public static SugerenciaPagoDto? SugerirRespaldo(CuentaPersonal cuenta, IReadOnlyList<Comprobante> facturas, IReadOnlyList<CorreoRecibido> correos)
    {
        var palabras = PalabrasClave(cuenta.Nombre);
        var factura = facturas
            .Select(f => new
            {
                Factura = f,
                Nombre = Menciona(palabras, $"{f.RazonSocialEmisor} {f.NombreComercial}"),
                Exacto = Math.Abs(f.Total - cuenta.Monto) <= 0.01m,
                Cercano = Math.Abs(f.Total - cuenta.Monto) <= cuenta.Monto * 0.2m,
            })
            .Where(x => (x.Nombre && x.Cercano) || x.Exacto)
            .OrderByDescending(x => x.Nombre && x.Exacto).ThenByDescending(x => x.Nombre).ThenBy(x => x.Factura.FechaEmision)
            .Select(x => x.Factura)
            .FirstOrDefault();
        if (factura is not null)
            return new SugerenciaPagoDto(RespaldoPago.Factura, factura.FechaEmision,
                $"Factura {factura.NumeroComprobante} de {factura.NombreComercial ?? factura.RazonSocialEmisor}", factura.Total, factura.Id, null);

        var correo = correos.FirstOrDefault(c => Menciona(palabras, $"{c.Asunto} {c.Remitente}"));
        return correo is null ? null
            : new SugerenciaPagoDto(RespaldoPago.Correo, DateOnly.FromDateTime(correo.RecibidoEn.AddHours(-5)),
                $"Correo \"{correo.Asunto}\" de {correo.Remitente}", null, null, correo.Id);
    }

    // Palabras que aparecen en cualquier cuenta o correo y no identifican al proveedor.
    private static readonly HashSet<string> PalabrasComunes =
    [
        "pago", "pagos", "cuenta", "cuentas", "mensual", "servicio", "servicios", "tarjeta", "banco", "factura", "credito",
        .. NombresMes,
    ];

    private static List<string> PalabrasClave(string texto) =>
        Normalizar(texto).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length >= 4 && !PalabrasComunes.Contains(p)).Distinct().ToList();

    private static bool Menciona(List<string> palabras, string texto)
    {
        var normalizado = $" {Normalizar(texto)} ";
        return palabras.Any(p => normalizado.Contains($" {p} ", StringComparison.Ordinal));
    }

    private static string Normalizar(string texto)
    {
        var sinTildes = texto.Normalize(NormalizationForm.FormD)
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark);
        return new string(sinTildes.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ').ToArray());
    }

    /// <summary>Ingreso del mes; si no lo registró, el último que declaró antes (el sueldo suele repetirse).</summary>
    private async Task<decimal?> IngresoDelMesAsync(int usuarioId, int anio, int mes, CancellationToken ct)
    {
        var clave = anio * 12 + mes;
        return await db.IngresosMensuales.AsNoTracking()
            .Where(i => i.UsuarioId == usuarioId && i.Anio * 12 + i.Mes <= clave)
            .OrderByDescending(i => i.Anio).ThenByDescending(i => i.Mes)
            .Select(i => (decimal?)i.Monto).FirstOrDefaultAsync(ct);
    }

    private async Task<CuentaPersonal> BuscarAsync(int usuarioId, int id, CancellationToken ct) =>
        await db.CuentasPersonales.SingleOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId, ct)
        ?? throw BusinessException.NotFound("Cuenta no encontrada.");

    private static void Aplicar(CuentaPersonal cuenta, CuentaPersonalRequest r)
    {
        cuenta.Nombre = r.Nombre.Trim();
        cuenta.Categoria = r.Categoria;
        cuenta.Monto = Math.Round(r.Monto, 2, MidpointRounding.AwayFromZero);
        cuenta.DiaVencimiento = r.DiaVencimiento;
    }

    private static void ValidarMes(int anio, int mes)
    {
        if (anio is < 2000 or > 2100 || mes is < 1 or > 12) throw new BusinessException("Mes no válido.");
    }

    private static void ValidarFechaPago(CuentaPersonal cuenta, DateOnly fecha)
    {
        if (fecha > HoyEcuador()) throw new BusinessException("La fecha de pago no puede ser posterior a hoy.");
        if (fecha < new DateOnly(cuenta.Anio, cuenta.Mes, 1).AddMonths(-12)) throw new BusinessException("Revisa la fecha de pago.");
    }

    private static DateOnly HoyEcuador() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
}
