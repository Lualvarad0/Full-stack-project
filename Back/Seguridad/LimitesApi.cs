using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Back.Seguridad;

/// <summary>Presupuestos compartidos entre rutas: cambiar un ID o endpoint no evade el límite.</summary>
public static class LimitesApi
{
    public const string Operaciones = "operaciones";

    public static IServiceCollection AddLimitesApi(this IServiceCollection services, IConfiguration config)
    {
        var ip = Limite(config, "PorIp", 300);
        var lectura = Limite(config, "Lecturas", 180);
        var escritura = Limite(config, "Escrituras", 30);
        var acceso = Limite(config, "Acceso", 8);
        var costosas = Limite(config, "Costosas", 6);
        var webhooks = Limite(config, "Webhooks", 120);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                var espera = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry) ? retry : TimeSpan.FromMinutes(1);
                context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(espera.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                await Results.Problem(statusCode: 429, detail: "Demasiadas solicitudes. Espera antes de intentarlo de nuevo.")
                    .ExecuteAsync(context.HttpContext);
            };
            // Presupuesto por IP adicional al del usuario; también cubre solicitudes sin sesión válida.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                Ventana("ip:" + context.Connection.RemoteIpAddress, ip));
            options.AddPolicy(Operaciones, context =>
            {
                var ruta = context.Request.Path;
                var grupo = ruta.StartsWithSegments("/api/auth") ? "acceso" :
                    ruta.StartsWithSegments("/api/correo/entrante") || ruta.StartsWithSegments("/api/pagos") ? "webhooks" :
                    ruta.StartsWithSegments("/api/contabilidad/exportaciones") || ruta.Value?.EndsWith("/importar", StringComparison.OrdinalIgnoreCase) == true ? "costosas" :
                    HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) ? "lectura" :
                    ruta.StartsWithSegments("/api/comprobantes/importar") || ruta.StartsWithSegments("/api/facturacion/facturas") || ruta.StartsWithSegments("/api/facturacion/complementarios") || ruta.StartsWithSegments("/api/contabilidad/retenciones/importar") ||
                    ruta.StartsWithSegments("/api/facturacion/firma") || ruta == "/api/suscripcion" ? "costosas" : "escritura";
                var maximo = grupo switch { "acceso" => acceso, "webhooks" => webhooks, "lectura" => lectura, "costosas" => costosas, _ => escritura };
                // El sujeto proviene exclusivamente del JWT validado, nunca de un header del cliente.
                var sujeto = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "ip:" + context.Connection.RemoteIpAddress;
                return Ventana(grupo + ":" + sujeto, maximo);
            });
        });
        return services;
    }

    private static RateLimitPartition<string> Ventana(string clave, int maximo) =>
        RateLimitPartition.GetFixedWindowLimiter(clave, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = maximo, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true,
        });

    private static int Limite(IConfiguration config, string nombre, int defecto)
    {
        var valor = config.GetValue($"RateLimiting:{nombre}", defecto);
        return valor > 0 ? valor : throw new InvalidOperationException($"RateLimiting:{nombre} debe ser positivo.");
    }
}
