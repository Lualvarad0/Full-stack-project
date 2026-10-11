using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Back.Dtos;
using Back.Models;
using Xunit;

namespace Back.Tests
{
    public partial class ImportacionSriTests
    {
        [Fact]
        public async Task Registro_PideNombreApellidoEdadYPlan()
        {
            var http = _factory.CreateClient();

            var menor = await http.PostAsJsonAsync("/api/auth/register", new
            {
                nombre = "Ana", apellido = "Pérez", fechaNacimiento = DateTime.UtcNow.AddYears(-15).ToString("yyyy-MM-dd"),
                plan = "Personal", email = "menor@test.ec", password = "Secreta123",
            });
            Assert.Equal(HttpStatusCode.BadRequest, menor.StatusCode);
            Assert.Contains("mayor de edad", await menor.Content.ReadAsStringAsync());

            var sinApellido = await http.PostAsJsonAsync("/api/auth/register", new
            {
                nombre = "Ana", fechaNacimiento = "1992-03-01", plan = "Personal", email = "sinapellido@test.ec", password = "Secreta123",
            });
            Assert.Equal(HttpStatusCode.BadRequest, sinApellido.StatusCode);

            var registro = await LeerAsync<AuthResponse>(await http.PostAsJsonAsync("/api/auth/register", new
            {
                nombre = "Ana María", apellido = "Pérez León", fechaNacimiento = "1992-03-01",
                plan = "Profesional", email = "ana@test.ec", password = "Secreta123",
            }));
            Assert.Equal("Ana María Pérez León", registro.Usuario.Nombre);
            // Elegir un plan de pago no lo activa: se activa al pagar (ver Suscripcion_CobraConKushkiYSigueLosWebhooks).
            Assert.Equal(PlanSuscripcion.Personal, registro.Usuario.Plan);
        }
    }
}
