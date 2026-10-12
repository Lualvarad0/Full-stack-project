using System.Security.Claims;
using Back.Data;
using Back.Helpers;
using Back.Models;
using Microsoft.EntityFrameworkCore;

namespace Back.Contabilidad;

public class ContextoContribuyenteMiddleware(RequestDelegate siguiente)
{
    private static readonly string[] Areas = ["/api/comprobantes", "/api/clientes", "/api/productos", "/api/facturas", "/api/facturacion", "/api/perfil", "/api/contabilidad"];
    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (context.User.Identity is ClaimsIdentity identidad)
            foreach (var claim in identidad.FindAll("contribuyente_id").ToArray()) identidad.RemoveClaim(claim);
        var header = context.Request.Headers["X-Contribuyente"].ToString();
        if (header.Length == 0 || !Areas.Any(a => context.Request.Path.StartsWithSegments(a))) { await siguiente(context); return; }
        if (context.User.Identity?.IsAuthenticated != true || !int.TryParse(header, out var propietarioId) || propietarioId <= 0)
        { await Results.Problem(statusCode: 403, detail: "No tienes acceso a este contribuyente.").ExecuteAsync(context); return; }
        var actor = JwtHelper.GetUsuarioId(context.User);
        if (actor == propietarioId) { await siguiente(context); return; }
        var contador = await db.Usuarios.SingleAsync(u => u.Id == actor);
        var permiso = await db.Set<VinculoContador>().Where(v => v.PropietarioId == propietarioId && v.ContadorId == actor && v.Estado == EstadoVinculo.Activo)
            .Select(v => (PermisoContador?)v.Permiso).SingleOrDefaultAsync();
        var lectura = HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method);
        // Firma privada, configuración de emisor y perfil siguen siendo responsabilidad del titular.
        var sensible = !lectura && (context.Request.Path.StartsWithSegments("/api/facturacion/firma") || context.Request.Path.StartsWithSegments("/api/facturacion/datos")
            || context.Request.Path.StartsWithSegments("/api/perfil"));
        var propietario = permiso.HasValue ? await db.Usuarios.SingleOrDefaultAsync(u => u.Id == propietarioId && u.Activo) : null;
        if (propietario is null || !contador.CorreoVerificado || (contador.Plan != PlanSuscripcion.Contadores && contador.Rol != Roles.Admin)
            || (!lectura && permiso != PermisoContador.Edicion) || sensible)
        { await Results.Problem(statusCode: 403, detail: "No tienes permiso para esta operación del contribuyente.").ExecuteAsync(context); return; }
        propietario.AccesoContador = true;
        ((ClaimsIdentity)context.User.Identity!).AddClaim(new("contribuyente_id", propietarioId.ToString()));
        await siguiente(context);
    }
}
