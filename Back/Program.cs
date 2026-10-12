using Back.Avisos;
using Back.Correo;
using Back.Data;
using Back.Facturacion;
using Back.Helpers;
using Back.Pagos;
using Back.Interfaces;
using Back.Repositories;
using Back.Services;
using Back.Sri;
using Back.Seguridad;
using Back.Models;
using Back.Contabilidad;
using Back.Clasificacion;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1024 * 1024);
builder.Services.AddLimitesApi(builder.Configuration);

// Base de datos (PostgreSQL). La cadena se toma de ConnectionStrings:Default
// (user-secrets en desarrollo, variable de entorno ConnectionStrings__Default en producción).
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:Default'.");
builder.Services.AddDbContext<AppDbContext>(options => options
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention());

// Repositorios y servicios
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IFacturaRepository, FacturaRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SeguridadCuentaService>();
builder.Services.AddScoped<CarteraService>();
builder.Services.AddScoped<ControlPeriodoService>();
builder.Services.AddScoped<PeriodosService>();
builder.Services.AddScoped<ClasificacionPartidasService>();
builder.Services.AddScoped<RetencionesService>();
builder.Services.AddScoped<DocumentosComplementariosService>();
builder.Services.AddScoped<CobrosPagosService>();
builder.Services.AddScoped<BancosService>();
builder.Services.AddScoped<LibroContableService>();
builder.Services.AddScoped<Back.Tributacion.PerfilTributarioService>();
builder.Services.AddScoped<Back.Tributacion.PreparacionTributariaService>();
builder.Services.AddScoped<Back.Tributacion.RevisionTributariaService>();
builder.Services.AddScoped<ExportacionesService>();
builder.Services.AddScoped<ProcesadorCorreoAcceso>();
builder.Services.AddScoped<ITransporteCorreoAcceso, TransporteCorreoAcceso>();
builder.Services.AddHostedService<CorreoAccesoWorker>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<ProductoService>();
builder.Services.AddScoped<FacturaService>();
builder.Services.AddScoped<IComprobanteRepository, ComprobanteRepository>();
builder.Services.AddScoped<ComprobanteService>();
builder.Services.AddScoped<PerfilService>();

// Servicio web de autorización del SRI
builder.Services.Configure<SriOptions>(builder.Configuration.GetSection(SriOptions.Section));
builder.Services.AddHttpClient<SriAutorizacionClient>(http =>
    http.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Sri:TimeoutSegundos", 30)));

// Buzón de reenvío: correo entrante por webhook, procesado en segundo plano.
builder.Services.AddScoped<IBuzonRepository, BuzonRepository>();
builder.Services.AddScoped<BuzonService>();
builder.Services.AddSingleton<SenalCorreos>();
builder.Services.AddHostedService<ProcesadorCorreosWorker>();

// Avisos de seguridad (ingresos y cambios de configuración), también por correo si hay SMTP.
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.Section));
builder.Services.AddSingleton<ColaCorreoSaliente>();
builder.Services.AddHostedService<EnvioCorreosWorker>();
builder.Services.AddScoped<AvisoService>();

// Facturación electrónica. La firma (.p12) y su contraseña se cifran con Data Protection; en producción define
// DataProtection:RutaClaves (carpeta persistente y privada) para que las claves sobrevivan a los reinicios.
var proteccion = builder.Services.AddDataProtection().SetApplicationName("nudo");
if (builder.Configuration["DataProtection:RutaClaves"] is { Length: > 0 } rutaClaves)
    proteccion.PersistKeysToFileSystem(new DirectoryInfo(rutaClaves));
builder.Services.AddScoped<FacturacionService>();

// Planes de pago con Kushki (claves en Kushki:ClavePublica, Kushki:ClavePrivada y Kushki:FirmaWebhook).
builder.Services.Configure<KushkiOptions>(builder.Configuration.GetSection(KushkiOptions.Section));
builder.Services.AddHttpClient<KushkiClient>(http => http.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddScoped<SuscripcionService>();
builder.Services.AddScoped<AdminService>();

// Autenticación JWT
var jwtOptions = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (jwtOptions.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Una cuenta suspendida por el administrador deja de funcionar aunque su token siga vigente.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async contexto =>
            {
                var id = JwtHelper.GetUsuarioId(contexto.Principal!);
                var db = contexto.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var rol = contexto.Principal!.FindFirstValue(ClaimTypes.Role);
                var sesion = contexto.Principal!.FindFirstValue("jti");
                if (!int.TryParse(contexto.Principal!.FindFirstValue("sv"), out var version)
                    || !await db.Usuarios.AnyAsync(u => u.Id == id && u.Activo && u.Rol == rol && u.VersionSeguridad == version)
                    || !await db.Set<SesionCuenta>().AnyAsync(s => s.Id == sesion && s.UsuarioId == id && s.RevocadaEn == null && s.ExpiraEn > DateTime.UtcNow))
                    contexto.Fail("Cuenta suspendida o eliminada.");
            },
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtOptions.SigningKey(),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();

// CORS: orígenes permitidos del frontend (Cors:Origins)
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services
    .AddControllers(options => options.Filters.Add<BusinessExceptionFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

// Swagger con soporte para Bearer token
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nudo API", Version = "v1" });
    var esquema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    options.AddSecurityDefinition("Bearer", esquema);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [esquema] = Array.Empty<string>() });
});

var app = builder.Build();

// Errores no controlados -> 500 genérico, sin exponer detalles internos.
// (Los errores de negocio los resuelve BusinessExceptionFilter.)
app.UseExceptionHandler(errorApp => errorApp.Run(context =>
    Results.Problem(detail: "Ocurrió un error inesperado.", statusCode: StatusCodes.Status500InternalServerError)
        .ExecuteAsync(context)));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Aplica migraciones pendientes al iniciar (Database:MigrateOnStartup), útil al desplegar.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    }
    await next();
});
app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<ContextoContribuyenteMiddleware>();

app.MapControllers().RequireRateLimiting(LimitesApi.Operaciones);
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();

// Expone Program a WebApplicationFactory en las pruebas de integración.
public partial class Program { }
