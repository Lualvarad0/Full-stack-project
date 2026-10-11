using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Back.Data;
using Back.Dtos;
using Back.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Back.Tests
{
    /// <summary>Kushki simulado: crea suscripciones (o rechaza el token "tok-rechazado") y acepta cancelaciones.</summary>
    public class FakeKushkiHandler : HttpMessageHandler
    {
        private int _siguiente = 1000;
        public List<(HttpMethod Metodo, string Ruta, string? Cuerpo, string? ClavePrivada)> Llamadas { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var cuerpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            lock (Llamadas)
                Llamadas.Add((request.Method, request.RequestUri!.AbsolutePath, cuerpo,
                    request.Headers.TryGetValues("Private-Merchant-Id", out var v) ? v.First() : null));

            if (request.Method == HttpMethod.Post && cuerpo!.Contains("\"tok-rechazado\""))
                return Json(HttpStatusCode.BadRequest, """{"code":"K006","message":"Tarjeta rechazada por el emisor"}""");
            if (request.Method == HttpMethod.Post)
                return Json(HttpStatusCode.OK, $$"""{"subscriptionId":"sub-{{Interlocked.Increment(ref _siguiente)}}"}""");
            return Json(HttpStatusCode.OK, "{}");
        }

        private static HttpResponseMessage Json(HttpStatusCode estado, string json) =>
            new(estado) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        protected override void Dispose(bool disposing) { }
    }

    public partial class ImportacionSriTests
    {
        private static HttpRequestMessage WebhookKushki(string json, string? firma = null)
        {
            var id = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            firma ??= Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(ApiFactory.FirmaWebhookKushki), Encoding.UTF8.GetBytes($"{json}.{id}"))).ToLowerInvariant();
            var solicitud = new HttpRequestMessage(HttpMethod.Post, "/api/pagos/kushki/webhook") { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            solicitud.Headers.Add("X-Kushki-Id", id);
            solicitud.Headers.Add("X-Kushki-Signature", firma);
            return solicitud;
        }

        private static string EventoKushki(string nombre, string subscriptionId, string ticket) => JsonSerializer.Serialize(new
        {
            name = nombre,
            @event = new { subscriptionId, ticketNumber = ticket, amount = new { subtotalIva = 14.99m, subtotalIva0 = 0m, iva = 2.25m, ice = 0m, currency = "USD" } },
        });

        /// <summary>Activa un plan de pago directamente en la base (para pruebas que no tratan sobre pagos).</summary>
        private async Task ActivarPlanAsync(string correo, PlanSuscripcion plan = PlanSuscripcion.Profesional)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Usuarios.Where(u => u.Correo == correo).ExecuteUpdateAsync(s => s.SetProperty(u => u.Plan, plan));
        }

        [Fact]
        public async Task Suscripcion_CobraConKushkiYSigueLosWebhooks()
        {
            var anonimo = _factory.CreateClient();
            var catalogo = await LeerAsync<CatalogoPagosDto>(await anonimo.GetAsync("/api/suscripcion/planes"));
            Assert.True(catalogo.PagosDisponibles);
            Assert.Equal("publica-pruebas", catalogo.ClavePublicaKushki);
            var mensual = catalogo.Precios.Single(p => p.Plan == PlanSuscripcion.Profesional && p.Periodicidad == Periodicidad.Mensual);
            Assert.Equal((14.99m, 2.25m, 17.24m), (mensual.Subtotal, mensual.Iva, mensual.Total));
            Assert.Equal(49.00m, catalogo.Precios.Single(p => p.Plan == PlanSuscripcion.Contadores && p.Periodicidad == Periodicidad.Mensual).Subtotal);

            var http = await ClienteAutenticadoAsync("suscriptor@test.ec");
            Assert.Equal(HttpStatusCode.NoContent, (await http.GetAsync("/api/suscripcion")).StatusCode);

            // Tarjeta rechazada: no se crea nada y el plan sigue siendo Personal.
            var rechazada = await http.PostAsJsonAsync("/api/suscripcion", new { plan = "Profesional", periodicidad = "Mensual", token = "tok-rechazado" });
            Assert.Equal(HttpStatusCode.PaymentRequired, rechazada.StatusCode);
            Assert.Contains("Tarjeta rechazada", await rechazada.Content.ReadAsStringAsync());

            var resultado = await LeerAsync<SuscripcionResultadoDto>(await http.PostAsJsonAsync("/api/suscripcion",
                new { plan = "Profesional", periodicidad = "Mensual", token = "tok-aprobado-123", marcaTarjeta = "Visa", ultimosDigitos = "5480" }));
            Assert.Equal(PlanSuscripcion.Profesional, resultado.Usuario.Plan);
            Assert.Equal(EstadoSuscripcion.EnPrueba, resultado.Suscripcion!.Estado);
            Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5)).AddDays(14), resultado.Suscripcion.ProximoCobro);

            // Lo que recibió Kushki: clave privada, monto con IVA, periodicidad y primer cobro al terminar la prueba.
            var alta = _factory.Kushki.Llamadas.Last(l => l.Metodo == HttpMethod.Post);
            Assert.Equal("/subscriptions/v1/card", alta.Ruta);
            Assert.Equal("privada-pruebas", alta.ClavePrivada);
            using (var cuerpo = JsonDocument.Parse(alta.Cuerpo!))
            {
                var raiz = cuerpo.RootElement;
                Assert.Equal("tok-aprobado-123", raiz.GetProperty("token").GetString());
                Assert.Equal("monthly", raiz.GetProperty("periodicity").GetString());
                Assert.Equal(14.99m, raiz.GetProperty("amount").GetProperty("subtotalIva").GetDecimal());
                Assert.Equal(2.25m, raiz.GetProperty("amount").GetProperty("iva").GetDecimal());
                Assert.Equal(resultado.Suscripcion.ProximoCobro.ToString("yyyy-MM-dd"), raiz.GetProperty("startDate").GetString());
            }

            var segunda = await http.PostAsJsonAsync("/api/suscripcion", new { plan = "Contadores", periodicidad = "Anual", token = "tok-aprobado-456" });
            Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);

            var kushkiId = await KushkiIdAsync("suscriptor@test.ec");

            // Webhooks: firma inválida → 401; cobro aprobado → Activa (y un webhook repetido no duplica el pago).
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.SendAsync(WebhookKushki(EventoKushki("succesfullCharge", kushkiId, "T-1"), "firma-falsa"))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await anonimo.SendAsync(WebhookKushki(EventoKushki("succesfullCharge", kushkiId, "T-1")))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await anonimo.SendAsync(WebhookKushki(EventoKushki("succesfullCharge", kushkiId, "T-1")))).StatusCode);
            Assert.Equal(EstadoSuscripcion.Activa, (await LeerAsync<SuscripcionDto>(await http.GetAsync("/api/suscripcion"))).Estado);
            using (var scope = _factory.Services.CreateScope())
                Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<AppDbContext>().PagosSuscripcion.CountAsync(p => p.Ticket == "T-1"));

            // Cobro rechazado → pago pendiente; reintentos agotados → vuelve al plan Personal.
            await anonimo.SendAsync(WebhookKushki(EventoKushki("failedRetry", kushkiId, "T-2")));
            Assert.Equal(EstadoSuscripcion.PagoPendiente, (await LeerAsync<SuscripcionDto>(await http.GetAsync("/api/suscripcion"))).Estado);
            await anonimo.SendAsync(WebhookKushki(EventoKushki("lastRetry", kushkiId, "T-3")));
            Assert.Equal(HttpStatusCode.NoContent, (await http.GetAsync("/api/suscripcion")).StatusCode);

            // Suscribirse de nuevo y cancelar: se cancela en Kushki y vuelve a Personal.
            await LeerAsync<SuscripcionResultadoDto>(await http.PostAsJsonAsync("/api/suscripcion", new { plan = "Contadores", periodicidad = "Anual", token = "tok-aprobado-789" }));
            var cancelada = await LeerAsync<SuscripcionResultadoDto>(await http.DeleteAsync("/api/suscripcion"));
            Assert.Equal(PlanSuscripcion.Personal, cancelada.Usuario.Plan);
            Assert.Contains(_factory.Kushki.Llamadas, l => l.Metodo == HttpMethod.Delete && l.Ruta.StartsWith("/subscriptions/v1/card/sub-"));

            var avisos = await LeerAsync<List<AvisoCuentaDto>>(await http.GetAsync("/api/avisos"));
            Assert.Contains(avisos, a => a.Titulo == "Recibimos tu pago");
            Assert.Contains(avisos, a => a.Titulo == "Tu plan se canceló por falta de pago");
        }

        [Fact]
        public async Task PlanPersonal_NoVeIngresosNiClientes()
        {
            var http = await ClienteAutenticadoAsync("personal@test.ec");
            using (var scope = _factory.Services.CreateScope())
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Usuarios.Where(u => u.Correo == "personal@test.ec")
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.Rol, Roles.User).SetProperty(u => u.Plan, PlanSuscripcion.Personal));

            var ingresos = await http.GetAsync("/api/comprobantes?direccion=Emitido");
            Assert.Equal(HttpStatusCode.Forbidden, ingresos.StatusCode);
            Assert.Contains("plan Profesional", await ingresos.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/api/comprobantes/clientes")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/comprobantes?direccion=Recibido")).StatusCode);

            var datos = await LeerAsync<DatosEmisorDto>(await http.GetAsync("/api/facturacion/datos"));
            Assert.Contains("el plan Profesional", datos.Pendientes);

            await ActivarPlanAsync("personal@test.ec");
            Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/comprobantes?direccion=Emitido")).StatusCode);
        }

        private async Task<string> KushkiIdAsync(string correo)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var usuario = await db.Usuarios.SingleAsync(u => u.Correo == correo);
            return (await db.Suscripciones.Where(s => s.UsuarioId == usuario.Id).OrderByDescending(s => s.CreadaEn).FirstAsync()).KushkiId;
        }
    }
}
