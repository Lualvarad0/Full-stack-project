using Back.Avisos;
using Back.Data;
using Back.Dtos;
using Back.Helpers;
using Back.Models;
using Microsoft.EntityFrameworkCore;

namespace Back.Contabilidad;

public class CarteraService(AppDbContext db, AvisoService avisos)
{
    public async Task SolicitarAsync(int contadorId, SolicitarVinculoRequest request, CancellationToken ct)
    {
        var reloj = System.Diagnostics.Stopwatch.StartNew();
        try { await CrearSolicitudAsync(contadorId, request, ct); }
        finally { var resto = 700 - (int)reloj.ElapsedMilliseconds; if (resto > 0) await Task.Delay(resto, ct); }
    }
    private async Task CrearSolicitudAsync(int contadorId, SolicitarVinculoRequest request, CancellationToken ct)
    {
        var contador = await db.Usuarios.SingleAsync(u => u.Id == contadorId, ct);
        if (contador.Plan != PlanSuscripcion.Contadores && contador.Rol != Roles.Admin) throw new BusinessException("La cartera de contribuyentes requiere el plan Contadores.", 403);
        if (!contador.CorreoVerificado) throw BusinessException.Conflict("Verifica tu correo antes de solicitar acceso a otro contribuyente.");
        // Límite del plan: se valida antes de buscar el correo, para no revelar si la cuenta existe.
        if (contador.Rol != Roles.Admin && await OcupadosAsync(contadorId, ct) >= Pagos.AccesoPlan.LimiteContribuyentesCartera)
            throw BusinessException.Conflict($"Tu cartera llegó al máximo de {Pagos.AccesoPlan.LimiteContribuyentesCartera} contribuyentes del plan Contadores (incluye solicitudes pendientes). Revoca un acceso o espera que venza una solicitud.");
        var propietario = await db.Usuarios.SingleOrDefaultAsync(u => u.Correo == request.Correo.Trim().ToLowerInvariant() && u.Activo, ct);
        // No revelar cuentas registradas; el solicitante ve su cartera solo tras autorización.
        if (propietario is null || propietario.Id == contadorId) return;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Usuarios.FromSqlInterpolated($"SELECT * FROM usuarios WHERE id = {contadorId} FOR UPDATE").ToListAsync(ct);
        if (await db.Set<VinculoContador>().CountAsync(v => v.ContadorId == contadorId && v.SolicitadoEn > DateTime.UtcNow.AddHours(-1), ct) >= 10) return;
        var v = await db.Set<VinculoContador>().SingleOrDefaultAsync(v => v.PropietarioId == propietario.Id && v.ContadorId == contadorId, ct);
        if (v?.Estado == EstadoVinculo.Activo || (v?.Estado == EstadoVinculo.Pendiente && v.ExpiraEn > DateTime.UtcNow)
            || v?.SolicitadoEn > DateTime.UtcNow.AddDays(-1)) return;
        if (v is null) { v = new() { PropietarioId = propietario.Id, ContadorId = contadorId }; db.Add(v); }
        v.SolicitadoPorId = contadorId; v.Estado = EstadoVinculo.Pendiente; v.Permiso = request.Permiso;
        v.SolicitadoEn = DateTime.UtcNow; v.ExpiraEn = DateTime.UtcNow.AddDays(7); v.RespondidoEn = null;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        await avisos.NotificarAsync(propietario.Id, TipoAviso.AccesoContador, "Solicitud de acceso de un contador", $"{contador.Nombre} {contador.Apellido} ({contador.Correo}) solicita acceso de {request.Permiso} a tu información. Revisa Cartera y accesos para aceptarlo o rechazarlo.");
    }
    /// <summary>Espacios usados de la cartera frente al límite del plan Contadores (sin límite para el administrador).</summary>
    public async Task<CupoPlanDto> CupoAsync(int contadorId, CancellationToken ct)
    {
        var contador = await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == contadorId, ct);
        if (contador.Plan != PlanSuscripcion.Contadores && contador.Rol != Roles.Admin) throw new BusinessException("La cartera de contribuyentes requiere el plan Contadores.", 403);
        return new CupoPlanDto(await OcupadosAsync(contadorId, ct), contador.Rol == Roles.Admin ? null : Pagos.AccesoPlan.LimiteContribuyentesCartera);
    }
    /// <summary>Espacios usados de la cartera: accesos activos más solicitudes pendientes que no han vencido.</summary>
    private Task<int> OcupadosAsync(int contadorId, CancellationToken ct) =>
        db.Set<VinculoContador>().CountAsync(v => v.ContadorId == contadorId
            && (v.Estado == EstadoVinculo.Activo || (v.Estado == EstadoVinculo.Pendiente && v.ExpiraEn > DateTime.UtcNow)), ct);
    public Task<List<VinculoContadorDto>> SolicitudesAsync(int usuarioId, CancellationToken ct) =>
        (from v in db.Set<VinculoContador>().AsNoTracking()
         join p in db.Usuarios on v.PropietarioId equals p.Id join c in db.Usuarios on v.ContadorId equals c.Id
         // No incluir cuentas objetivo para el solicitante antes de la aceptación.
         where v.PropietarioId == usuarioId || (v.ContadorId == usuarioId && v.Estado == EstadoVinculo.Activo)
         orderby v.SolicitadoEn descending
         select new VinculoContadorDto(v.Id, v.PropietarioId, v.ContadorId, p.RazonSocial ?? p.Nombre + " " + p.Apellido, p.Ruc,
             c.Nombre + " " + c.Apellido, c.Correo, v.Permiso, v.Estado, v.SolicitadoEn, v.ExpiraEn,
             v.PropietarioId == usuarioId && v.Estado == EstadoVinculo.Pendiente && v.ExpiraEn > DateTime.UtcNow)).ToListAsync(ct);
    public async Task ResponderAsync(int usuarioId, int id, bool aceptar, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var v = (await db.Set<VinculoContador>().FromSqlInterpolated($"SELECT * FROM vinculos_contador WHERE id = {id} AND propietario_id = {usuarioId} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()
            ?? throw BusinessException.NotFound("Solicitud no encontrada.");
        if (v.Estado != EstadoVinculo.Pendiente || v.ExpiraEn <= DateTime.UtcNow) throw BusinessException.Conflict("La solicitud ya fue respondida o venció.");
        if (aceptar && !await db.Usuarios.AnyAsync(u => u.Id == usuarioId && u.CorreoVerificado && u.Activo, ct)) throw BusinessException.Conflict("Verifica tu correo antes de autorizar acceso.");
        if (aceptar && !await db.Usuarios.AnyAsync(u => u.Id == v.ContadorId && u.CorreoVerificado && u.Activo && (u.Plan == PlanSuscripcion.Contadores || u.Rol == Roles.Admin), ct))
            throw BusinessException.Conflict("El contador debe tener su cuenta verificada y el plan activo.");
        if (aceptar)
        {
            // Bloquea al contador: dos aceptaciones a la vez no pueden pasar del límite de su cartera.
            var contador = (await db.Usuarios.FromSqlInterpolated($"SELECT * FROM usuarios WHERE id = {v.ContadorId} FOR UPDATE").ToListAsync(ct)).Single();
            var activos = await db.Set<VinculoContador>().CountAsync(x => x.ContadorId == v.ContadorId && x.Estado == EstadoVinculo.Activo, ct);
            if (contador.Rol != Roles.Admin && activos >= Pagos.AccesoPlan.LimiteContribuyentesCartera)
                throw BusinessException.Conflict($"Tu contador ya atiende el máximo de {Pagos.AccesoPlan.LimiteContribuyentesCartera} contribuyentes de su plan. Pídele que libere un espacio y vuelve a intentarlo.");
        }
        v.Estado = aceptar ? EstadoVinculo.Activo : EstadoVinculo.Rechazado; v.RespondidoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        await avisos.NotificarAsync(usuarioId, TipoAviso.AccesoContador, aceptar ? "Autorizaste a tu contador" : "Rechazaste una solicitud", $"Permiso solicitado: {v.Permiso}. Puedes revocar el acceso desde Cartera y accesos.");
    }
    public async Task RevocarAsync(int usuarioId, int id, CancellationToken ct)
    {
        if (await db.Set<VinculoContador>().Where(v => v.Id == id && (v.PropietarioId == usuarioId || v.ContadorId == usuarioId) && v.Estado == EstadoVinculo.Activo)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.Estado, EstadoVinculo.Revocado).SetProperty(v => v.RespondidoEn, DateTime.UtcNow), ct) == 0)
            throw BusinessException.NotFound("Acceso no encontrado.");
        await avisos.NotificarAsync(usuarioId, TipoAviso.AccesoContador, "Revocaste un acceso contable", "El acceso revocado deja de funcionar en las siguientes peticiones.");
    }
    public async Task<List<ContribuyenteAccesibleDto>> ContribuyentesAsync(int usuarioId, CancellationToken ct)
    {
        var usuario = await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == usuarioId, ct);
        var lista = new List<ContribuyenteAccesibleDto> { new(usuario.Id, usuario.RazonSocial ?? usuario.Nombre + " " + usuario.Apellido, usuario.Ruc, PermisoContador.Edicion, true) };
        if (usuario.Plan != PlanSuscripcion.Contadores && usuario.Rol != Roles.Admin) return lista;
        lista.AddRange(await (from v in db.Set<VinculoContador>() join p in db.Usuarios on v.PropietarioId equals p.Id
            where v.ContadorId == usuarioId && v.Estado == EstadoVinculo.Activo && p.Activo
            select new ContribuyenteAccesibleDto(p.Id, p.RazonSocial ?? p.Nombre + " " + p.Apellido, p.Ruc, v.Permiso, false)).ToListAsync(ct));
        return lista;
    }
}
