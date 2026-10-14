using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Back.Helpers;
using Microsoft.Extensions.Options;

namespace Back.Pagos
{
    public class KushkiOptions
    {
        public const string Section = "Kushki";

        /// <summary>Clave pública (la usa el navegador para tokenizar la tarjeta).</summary>
        public string ClavePublica { get; set; } = string.Empty;
        /// <summary>Clave privada (solo backend; nunca se envía al navegador).</summary>
        public string ClavePrivada { get; set; } = string.Empty;
        /// <summary>"Webhook signature" de la consola de Kushki (Desarrolladores → Webhooks).</summary>
        public string FirmaWebhook { get; set; } = string.Empty;
        /// <summary>true: ambiente UAT de Kushki (tarjetas de prueba).</summary>
        public bool Pruebas { get; set; } = true;

        public bool Configurado => ClavePublica.Length > 0 && ClavePrivada.Length > 0;
        public string UrlBase => Pruebas ? "https://api-uat.kushkipagos.com" : "https://api.kushkipagos.com";
    }

    public record ContactoKushki(string FirstName, string LastName, string Email);

    /// <summary>Lo que Kushki dice de un BIN. Tipo: "credit" o "debit".</summary>
    public record InfoBin(string Marca, string? Banco, string Tipo);

    /// <summary>Cliente de la API de suscripciones con tarjeta de Kushki (Ecuador).</summary>
    public class KushkiClient
    {
        private readonly HttpClient _http;
        private readonly KushkiOptions _options;
        private readonly ILogger<KushkiClient> _logger;

        public KushkiClient(HttpClient http, IOptions<KushkiOptions> options, ILogger<KushkiClient> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>Crea la suscripción con el token de la tarjeta; el primer cobro ocurre en <paramref name="primerCobro"/>.</summary>
        public async Task<string> CrearSuscripcionAsync(string token, PrecioPlan precio, ContactoKushki contacto, DateOnly primerCobro,
            Dictionary<string, string> metadata, CancellationToken ct)
        {
            var cuerpo = new
            {
                token,
                planName = $"Nudo {precio.Plan} {precio.Periodicidad}",
                periodicity = precio.Periodicidad == Models.Periodicidad.Anual ? "yearly" : "monthly",
                contactDetails = new { firstName = contacto.FirstName, lastName = contacto.LastName, email = contacto.Email },
                amount = new { subtotalIva = precio.Subtotal, subtotalIva0 = 0m, ice = 0m, iva = precio.Iva, currency = "USD" },
                startDate = primerCobro.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                metadata,
            };

            using var respuesta = await EnviarAsync(HttpMethod.Post, "/subscriptions/v1/card", cuerpo, ct);
            var json = await respuesta.Content.ReadAsStringAsync(ct);
            if (!respuesta.IsSuccessStatusCode)
                throw new BusinessException($"Kushki no aprobó la tarjeta: {MensajeDeError(json)}", StatusCodes.Status402PaymentRequired);

            using var documento = JsonDocument.Parse(json);
            if (documento.RootElement.TryGetProperty("subscriptionId", out var id) && id.GetString() is { Length: > 0 } subscriptionId)
                return subscriptionId;
            _logger.LogError("Respuesta de Kushki sin subscriptionId: {Respuesta}", json);
            throw new BusinessException("Kushki no devolvió el número de la suscripción. Intenta de nuevo.", StatusCodes.Status502BadGateway);
        }

        public async Task CancelarAsync(string subscriptionId, CancellationToken ct)
        {
            using var respuesta = await EnviarAsync(HttpMethod.Delete, $"/subscriptions/v1/card/{Uri.EscapeDataString(subscriptionId)}", null, ct);
            // 404: ya no existe en Kushki (p. ej. se canceló desde su consola): para Nudo también está cancelada.
            if (!respuesta.IsSuccessStatusCode && (int)respuesta.StatusCode != 404)
                throw new BusinessException($"No se pudo cancelar en Kushki: {MensajeDeError(await respuesta.Content.ReadAsStringAsync(ct))}",
                    StatusCodes.Status502BadGateway);
        }

        /// <summary>
        /// Banco emisor, marca y tipo ("credit"/"debit") de una tarjeta por su BIN (6 primeros dígitos), con la clave pública
        /// (la misma consulta que hace el SDK del navegador). null si Kushki no reconoce el BIN.
        /// </summary>
        public async Task<InfoBin?> ConsultarBinAsync(string bin, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_options.ClavePublica))
                throw new BusinessException("La verificación de tarjetas no está configurada en este servidor.", StatusCodes.Status503ServiceUnavailable);
            using var solicitud = new HttpRequestMessage(HttpMethod.Get, $"{_options.UrlBase}/deferred/v2/bin/{Uri.EscapeDataString(bin)}");
            solicitud.Headers.Add("Public-Merchant-Id", _options.ClavePublica);
            using var respuesta = await _http.SendAsync(solicitud, ct);
            if ((int)respuesta.StatusCode is 400 or 404) return null;
            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning("Kushki respondió {Estado} al consultar un BIN", (int)respuesta.StatusCode);
                throw new BusinessException("No pudimos verificar la tarjeta en este momento. Intenta de nuevo.", StatusCodes.Status502BadGateway);
            }

            using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
            string? Texto(string nombre) =>
                documento.RootElement.TryGetProperty(nombre, out var v) && v.ValueKind == JsonValueKind.String && v.GetString() is { Length: > 0 } s ? s.Trim() : null;
            return Texto("brand") is { } marca ? new InfoBin(marca, Texto("bank"), Texto("cardType") ?? "") : null;
        }

        /// <summary>
        /// Verifica X-Kushki-Signature: HMAC-SHA256 (hex) de "{cuerpo}.{X-Kushki-Id}" con la firma de webhooks.
        /// </summary>
        public static bool FirmaValida(string cuerpo, string? kushkiId, string? firma, string secreto)
        {
            if (string.IsNullOrEmpty(secreto) || string.IsNullOrEmpty(kushkiId) || string.IsNullOrEmpty(firma)) return false;
            var esperada = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secreto), Encoding.UTF8.GetBytes($"{cuerpo}.{kushkiId}")));
            return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(esperada.ToLowerInvariant()), Encoding.ASCII.GetBytes(firma.Trim().ToLowerInvariant()));
        }

        private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string ruta, object? cuerpo, CancellationToken ct)
        {
            if (!_options.Configurado)
                throw new BusinessException("Los pagos aún no están configurados en este servidor.", StatusCodes.Status503ServiceUnavailable);
            var solicitud = new HttpRequestMessage(metodo, _options.UrlBase + ruta);
            solicitud.Headers.Add("Private-Merchant-Id", _options.ClavePrivada);
            if (cuerpo is not null) solicitud.Content = JsonContent.Create(cuerpo);
            return await _http.SendAsync(solicitud, ct);
        }

        private static string MensajeDeError(string json)
        {
            try
            {
                using var documento = JsonDocument.Parse(json);
                if (documento.RootElement.TryGetProperty("message", out var mensaje)) return mensaje.GetString() ?? "error desconocido";
            }
            catch (JsonException)
            {
                // La respuesta no es JSON: se informa de forma genérica.
            }
            return "error desconocido";
        }
    }
}
