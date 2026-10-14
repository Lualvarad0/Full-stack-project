using Back.Correo;
using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Back.Pagos;
using Back.Services;
using Microsoft.EntityFrameworkCore;

namespace Back.Tarjetas;

/// <summary>
/// Tarjetas de crédito (planes de pago): se registran con el BIN verificado en Kushki (banco emisor real y tarjeta
/// de crédito) y su deuda viene del estado de cuenta mensual (PDF subido o reenviado al buzón). Lo leído queda
/// por confirmar; al confirmarlo se convierte en la deuda del mes en Mis cuentas.
/// </summary>
public class TarjetasService(AppDbContext db, KushkiClient kushki, ILogger<TarjetasService> logger)
{
    public const int MaxTarjetas = 10;
    private const string Seccion = "Las tarjetas de crédito y sus estados de cuenta";

    public async Task<TarjetasDto> ListarAsync(int usuarioId, CancellationToken ct)
    {
        await ExigirPlanAsync(usuarioId, ct);
        var tarjetas = await db.TarjetasCredito.AsNoTracking().Where(t => t.UsuarioId == usuarioId).OrderBy(t => t.Alias).ToListAsync(ct);
        var estados = await db.EstadosCuentaTarjeta.AsNoTracking()
            .Where(e => e.UsuarioId == usuarioId && e.Estado != EstadoRevision.Descartado)
            .OrderByDescending(e => e.FechaMaximaPago).ThenByDescending(e => e.CreadoEn)
            .ToListAsync(ct);
        return new TarjetasDto(
            tarjetas.Select(t => new TarjetaDto(t.Id, t.Alias, t.Marca, t.Banco, t.Ultimos4, t.Verificada,
                estados.Where(e => e.TarjetaCreditoId == t.Id && e.Estado == EstadoRevision.Confirmado).Select(EstadoCuentaDto.From).FirstOrDefault())).ToList(),
            estados.Where(e => e.Estado == EstadoRevision.PorConfirmar).Select(EstadoCuentaDto.From).ToList());
    }

    public async Task<TarjetaDto> CrearAsync(int usuarioId, TarjetaRequest r, CancellationToken ct)
    {
        await ExigirPlanAsync(usuarioId, ct);
        if (await db.TarjetasCredito.CountAsync(t => t.UsuarioId == usuarioId, ct) >= MaxTarjetas)
            throw new BusinessException($"Puedes registrar hasta {MaxTarjetas} tarjetas.");
        if (await db.TarjetasCredito.AnyAsync(t => t.UsuarioId == usuarioId && t.Bin == r.Bin && t.Ultimos4 == r.Ultimos4, ct))
            throw BusinessException.Conflict("Esa tarjeta ya está registrada.");

        // El BIN dice quién emitió la tarjeta: si ningún banco lo tiene, el número no es de una tarjeta real.
        var info = await kushki.ConsultarBinAsync(r.Bin, ct)
            ?? throw new BusinessException("No encontramos un banco emisor para esa tarjeta. Revisa el número.");
        if (!info.Tipo.Equals("credit", StringComparison.OrdinalIgnoreCase))
            throw new BusinessException(info.Tipo.Equals("debit", StringComparison.OrdinalIgnoreCase)
                ? "Es una tarjeta de débito. Aquí registras tus tarjetas de crédito."
                : "No pudimos confirmar que sea una tarjeta de crédito.");

        var tarjeta = new TarjetaCredito
        {
            UsuarioId = usuarioId, Alias = r.Alias.Trim(), Bin = r.Bin, Ultimos4 = r.Ultimos4,
            Marca = Recortar(info.Marca, 40), Banco = info.Banco is null ? null : Recortar(info.Banco, 120), Verificada = true,
        };
        db.TarjetasCredito.Add(tarjeta);
        await db.SaveChangesAsync(ct);
        return new TarjetaDto(tarjeta.Id, tarjeta.Alias, tarjeta.Marca, tarjeta.Banco, tarjeta.Ultimos4, tarjeta.Verificada, null);
    }

    /// <summary>Quita la tarjeta y sus estados de cuenta; las deudas ya anotadas en Mis cuentas se conservan.</summary>
    public async Task EliminarAsync(int usuarioId, int id, CancellationToken ct)
    {
        db.TarjetasCredito.Remove(await BuscarTarjetaAsync(usuarioId, id, ct));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Lee el estado de cuenta subido; queda por confirmar. La clave del PDF no se guarda.</summary>
    public async Task<EstadoCuentaDto> SubirEstadoCuentaAsync(int usuarioId, int tarjetaId, byte[] pdf, string? clave, CancellationToken ct)
    {
        var usuario = await ExigirPlanAsync(usuarioId, ct);
        var tarjeta = await BuscarTarjetaAsync(usuarioId, tarjetaId, ct);
        var leido = LectorEstadoCuenta.Analizar(LectorEstadoCuenta.ExtraerTexto(pdf, [clave, .. usuario.Identificaciones()]));
        if (leido.Ultimos4.Count > 0 && !leido.Ultimos4.Contains(tarjeta.Ultimos4))
            throw new BusinessException($"Este estado de cuenta es de la tarjeta terminada en {leido.Ultimos4[0]}, no de la terminada en {tarjeta.Ultimos4}.");
        if (!leido.TieneDatos)
            throw new BusinessException("No encontramos el saldo ni la fecha de pago en el PDF. Ingresa los datos a mano.");

        var estado = Nuevo(usuarioId, tarjeta.Id, leido, OrigenEstadoCuenta.Subido);
        db.EstadosCuentaTarjeta.Add(estado);
        await db.SaveChangesAsync(ct);
        return EstadoCuentaDto.From(estado);
    }

    /// <summary>
    /// PDF adjuntos de un correo reenviado al buzón: los que son estado de cuenta de una tarjeta registrada
    /// (por sus 4 últimos dígitos) quedan por confirmar. Devuelve cuántos se leyeron.
    /// </summary>
    public async Task<int> RegistrarDesdeCorreoAsync(int usuarioId, IReadOnlyList<AdjuntoPdf> pdfs, CancellationToken ct)
    {
        var usuario = await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == usuarioId, ct);
        if (!AccesoPlan.TieneProfesional(usuario)) return 0;
        var tarjetas = await db.TarjetasCredito.AsNoTracking().Where(t => t.UsuarioId == usuarioId).ToListAsync(ct);
        if (tarjetas.Count == 0) return 0;

        var leidos = 0;
        foreach (var pdf in pdfs)
        {
            EstadoLeido leido;
            try
            {
                leido = LectorEstadoCuenta.Analizar(LectorEstadoCuenta.ExtraerTexto(pdf.Contenido, usuario.Identificaciones()));
            }
            catch (BusinessException e)
            {
                logger.LogInformation("PDF del buzón no leído ({Archivo}): {Motivo}", pdf.Nombre, e.Message);
                continue;
            }

            var tarjeta = tarjetas.FirstOrDefault(t => leido.Ultimos4.Contains(t.Ultimos4));
            if (tarjeta is null || !leido.TieneDatos) continue;
            db.EstadosCuentaTarjeta.Add(Nuevo(usuarioId, tarjeta.Id, leido, OrigenEstadoCuenta.Correo));
            leidos++;
        }

        if (leidos > 0) await db.SaveChangesAsync(ct);
        return leidos;
    }

    /// <summary>Estado de cuenta escrito a mano (por ejemplo si el banco no envía PDF): se confirma de una vez.</summary>
    public async Task<EstadoCuentaDto> RegistrarManualAsync(int usuarioId, int tarjetaId, EstadoCuentaRequest r, CancellationToken ct)
    {
        await ExigirPlanAsync(usuarioId, ct);
        var tarjeta = await BuscarTarjetaAsync(usuarioId, tarjetaId, ct);
        var estado = new EstadoCuentaTarjeta { UsuarioId = usuarioId, TarjetaCreditoId = tarjeta.Id, Origen = OrigenEstadoCuenta.Manual };
        db.EstadosCuentaTarjeta.Add(estado);
        await AplicarAsync(usuarioId, tarjeta, estado, r, ct);
        return EstadoCuentaDto.From(estado);
    }

    /// <summary>Confirma (corrigiendo si hace falta) lo leído del estado de cuenta y lo anota como deuda del mes.</summary>
    public async Task<EstadoCuentaDto> ConfirmarAsync(int usuarioId, int estadoId, EstadoCuentaRequest r, CancellationToken ct)
    {
        await ExigirPlanAsync(usuarioId, ct);
        var estado = await db.EstadosCuentaTarjeta.SingleOrDefaultAsync(e => e.Id == estadoId && e.UsuarioId == usuarioId, ct)
            ?? throw BusinessException.NotFound("Estado de cuenta no encontrado.");
        if (estado.Estado != EstadoRevision.PorConfirmar) throw new BusinessException("Ese estado de cuenta ya fue revisado.");
        var tarjeta = await BuscarTarjetaAsync(usuarioId, estado.TarjetaCreditoId, ct);
        await AplicarAsync(usuarioId, tarjeta, estado, r, ct);
        return EstadoCuentaDto.From(estado);
    }

    public async Task DescartarAsync(int usuarioId, int estadoId, CancellationToken ct)
    {
        var estado = await db.EstadosCuentaTarjeta.SingleOrDefaultAsync(e => e.Id == estadoId && e.UsuarioId == usuarioId, ct)
            ?? throw BusinessException.NotFound("Estado de cuenta no encontrado.");
        if (estado.Estado != EstadoRevision.PorConfirmar) throw new BusinessException("Ese estado de cuenta ya fue revisado.");
        estado.Estado = EstadoRevision.Descartado;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Guarda los datos y crea (o actualiza) la deuda de la tarjeta en el mes de su fecha máxima de pago.</summary>
    private async Task AplicarAsync(int usuarioId, TarjetaCredito tarjeta, EstadoCuentaTarjeta estado, EstadoCuentaRequest r, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
        if (r.FechaMaximaPago < hoy.AddYears(-1) || r.FechaMaximaPago > hoy.AddMonths(3))
            throw new BusinessException("Revisa la fecha máxima de pago.");
        if (r.FechaCorte is { } corte && corte > r.FechaMaximaPago) throw new BusinessException("La fecha de corte va antes de la fecha máxima de pago.");
        if (r.PagoMinimo > r.SaldoTotal) throw new BusinessException("El pago mínimo no puede ser mayor que el saldo total.");
        var monto = r.PagarMinimo ? r.PagoMinimo ?? throw new BusinessException("Indica el pago mínimo.") : r.SaldoTotal;
        if (monto <= 0) throw new BusinessException("El valor a pagar debe ser mayor que cero.");

        (estado.SaldoTotal, estado.PagoMinimo, estado.FechaMaximaPago, estado.FechaCorte) = (r.SaldoTotal, r.PagoMinimo, r.FechaMaximaPago, r.FechaCorte);
        estado.Estado = EstadoRevision.Confirmado;

        var (anio, mes) = (r.FechaMaximaPago.Year, r.FechaMaximaPago.Month);
        var deuda = await db.CuentasPersonales.SingleOrDefaultAsync(c => c.UsuarioId == usuarioId && c.TarjetaCreditoId == tarjeta.Id
            && c.Anio == anio && c.Mes == mes && c.TrasladadaDesdeId == null && c.PagadaEl == null && c.TrasladadaACuentaId == null, ct);
        if (deuda is null)
        {
            if (await db.CuentasPersonales.CountAsync(c => c.UsuarioId == usuarioId && c.Anio == anio && c.Mes == mes, ct) >= CuentasPersonalesService.MaxCuentasPorMes)
                throw new BusinessException($"Ese mes ya tiene {CuentasPersonalesService.MaxCuentasPorMes} cuentas.");
            deuda = new CuentaPersonal
            {
                UsuarioId = usuarioId, Anio = anio, Mes = mes, Categoria = CategoriaCuenta.Deudas, TarjetaCreditoId = tarjeta.Id,
                Nombre = Recortar($"Tarjeta {tarjeta.Banco ?? tarjeta.Marca} •••• {tarjeta.Ultimos4}", 120),
            };
            db.CuentasPersonales.Add(deuda);
        }
        deuda.Monto = Math.Round(monto, 2, MidpointRounding.AwayFromZero);
        deuda.DiaVencimiento = r.FechaMaximaPago.Day;

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.SaveChangesAsync(ct);
        estado.CuentaPersonalId = deuda.Id;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static EstadoCuentaTarjeta Nuevo(int usuarioId, int tarjetaId, EstadoLeido leido, OrigenEstadoCuenta origen) => new()
    {
        UsuarioId = usuarioId, TarjetaCreditoId = tarjetaId, Origen = origen, SaldoTotal = leido.SaldoTotal, PagoMinimo = leido.PagoMinimo,
        FechaMaximaPago = leido.FechaMaximaPago, FechaCorte = leido.FechaCorte,
    };

    private async Task<Usuario> ExigirPlanAsync(int usuarioId, CancellationToken ct)
    {
        var usuario = await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == usuarioId, ct);
        AccesoPlan.ExigirProfesional(usuario, Seccion);
        return usuario;
    }

    private async Task<TarjetaCredito> BuscarTarjetaAsync(int usuarioId, int id, CancellationToken ct) =>
        await db.TarjetasCredito.SingleOrDefaultAsync(t => t.Id == id && t.UsuarioId == usuarioId, ct)
        ?? throw BusinessException.NotFound("Tarjeta no encontrada.");

    private static string Recortar(string texto, int largo) => texto.Length <= largo ? texto : texto[..largo];
}
