using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Back.Data;
using Back.Dtos;
using Back.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Back.Tests
{
    public partial class ImportacionSriTests
    {
        private async Task CambiarRolYPlanAsync(string correo, string rol, PlanSuscripcion plan)
        {
            using var scope = _factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Usuarios.Where(u => u.Correo == correo)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Rol, rol).SetProperty(u => u.Plan, plan));
        }

        /// <summary>Superadministrador: se registra, se le da el rol en la base y vuelve a ingresar (el rol va en el token).</summary>
        private async Task<HttpClient> ClienteAdminAsync(string correo)
        {
            await ClienteAutenticadoAsync(correo);
            await CambiarRolYPlanAsync(correo, Roles.Admin, PlanSuscripcion.Personal);
            var http = _factory.CreateClient();
            var login = await LeerAsync<AuthResponse>(await http.PostAsJsonAsync("/api/auth/login", new { email = correo, password = "Secreta123" }));
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
            return http;
        }

        [Fact]
        public async Task PlanPersonal_LimitaComprobantesHistorialYFiltros()
        {
            var http = await ClienteAutenticadoAsync("limite@test.ec");
            await CambiarRolYPlanAsync("limite@test.ec", Roles.User, PlanSuscripcion.Personal);

            // 52 facturas nuevas: entran 50 (el límite del mes) y 2 quedan pendientes por el plan.
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
            var claves = Enumerable.Range(1, 52).Select(i =>
            {
                var clave = SriFixtures.Clave(hoy, "01", SriFixtures.RucSupermercado, 7000 + i);
                return Autorizar(clave, SriFixtures.FacturaXml(clave, SriFixtures.RucSupermercado, "SUPERMERCADO", hoy));
            }).ToList();
            var importacion = await LeerAsync<ImportacionResponse>(await http.PostAsJsonAsync("/api/comprobantes/importar/claves", new { texto = string.Join(" ", claves) }));
            Assert.Equal(50, importacion.Importados);
            Assert.Equal(2, importacion.Claves.Count(c => c.Resultado == ResultadoImportacion.LimiteDelPlan));

            var uso = await LeerAsync<UsoPlanDto>(await http.GetAsync("/api/suscripcion/uso"));
            Assert.Equal((50, 50, 3), (uso.LimiteComprobantes!.Value, uso.ComprobantesEsteMes, uso.MesesHistorial!.Value));

            var antiguo = hoy.AddMonths(-6);
            Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync($"/api/comprobantes?anio={antiguo.Year}&mes={antiguo.Month}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/api/comprobantes?min=10")).StatusCode);

            // Con Profesional no hay límite.
            await ActivarPlanAsync("limite@test.ec");
            var resto = await LeerAsync<ImportacionResponse>(await http.PostAsJsonAsync("/api/comprobantes/importar/claves", new { texto = string.Join(" ", claves) }));
            Assert.Equal(2, resto.Importados);
            Assert.Null((await LeerAsync<UsoPlanDto>(await http.GetAsync("/api/suscripcion/uso"))).LimiteComprobantes);
        }

        [Fact]
        public async Task Admin_VeMetricasYGestionaUsuarios()
        {
            var admin = await ClienteAdminAsync("dueno@test.ec");
            var cliente = await ClienteAutenticadoAsync("cliente.admin@test.ec");
            await CambiarRolYPlanAsync("cliente.admin@test.ec", Roles.User, PlanSuscripcion.Personal);

            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/admin/metricas")).StatusCode);

            var metricas = await LeerAsync<MetricasAdminDto>(await admin.GetAsync("/api/admin/metricas"));
            Assert.True(metricas.UsuariosTotales >= 2);
            Assert.Equal(6, metricas.RegistrosPorMes.Count);
            Assert.True(metricas.NuevosUltimos30Dias >= 2);

            var usuarios = await LeerAsync<List<UsuarioAdminDto>>(await admin.GetAsync("/api/admin/usuarios?q=cliente.admin"));
            var usuario = Assert.Single(usuarios);
            Assert.Equal(PlanSuscripcion.Personal, usuario.Plan);

            // Plan de cortesía: sin cobro y con aviso al usuario.
            Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/admin/usuarios/{usuario.Id}/plan", new { plan = "Profesional" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/comprobantes?direccion=Emitido")).StatusCode);
            Assert.Contains(await LeerAsync<List<AvisoCuentaDto>>(await admin.GetAsync($"/api/admin/usuarios/{usuario.Id}/actividad")),
                a => a.Titulo == "Tu plan cambió");

            // Suspender: el token vigente deja de servir y no puede volver a ingresar.
            Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/admin/usuarios/{usuario.Id}/estado", new { activo = false })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/perfil")).StatusCode);
            var login = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = "cliente.admin@test.ec", password = "Secreta123" });
            Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
            Assert.Contains("suspendida", await login.Content.ReadAsStringAsync());

            await admin.PutAsJsonAsync($"/api/admin/usuarios/{usuario.Id}/estado", new { activo = true });
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/perfil")).StatusCode);

            var yo = Assert.Single(await LeerAsync<List<UsuarioAdminDto>>(await admin.GetAsync("/api/admin/usuarios?q=dueno@test.ec")));
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"/api/admin/usuarios/{yo.Id}")).StatusCode);

            Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/usuarios/{usuario.Id}")).StatusCode);
            Assert.Empty(await LeerAsync<List<UsuarioAdminDto>>(await admin.GetAsync("/api/admin/usuarios?q=cliente.admin")));

            var sistema = await LeerAsync<EstadoSistemaDto>(await admin.GetAsync("/api/admin/sistema"));
            Assert.True(sistema.KushkiConfigurado);
            Assert.Equal(0, sistema.MigracionesPendientes);
            Assert.NotNull(await LeerAsync<List<PagoAdminDto>>(await admin.GetAsync("/api/admin/pagos")));
        }
    }
}
