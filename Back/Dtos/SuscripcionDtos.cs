using Back.Seguridad;
using System.ComponentModel.DataAnnotations;
using Back.Models;
using Back.Pagos;

namespace Back.Dtos
{
    public record PrecioPlanDto(PlanSuscripcion Plan, Periodicidad Periodicidad, decimal Subtotal, decimal Iva, decimal Total)
    {
        public static PrecioPlanDto From(PrecioPlan p) => new(p.Plan, p.Periodicidad, p.Subtotal, p.Iva, p.Total);
    }

    /// <summary>Lo que necesita la pantalla de pago: precios, días de prueba y la clave pública de Kushki.</summary>
    public record CatalogoPagosDto(List<PrecioPlanDto> Precios, int DiasDePrueba, bool PagosDisponibles, string? ClavePublicaKushki, bool KushkiPruebas);

    public record SuscripcionDto(
        PlanSuscripcion Plan,
        Periodicidad Periodicidad,
        EstadoSuscripcion Estado,
        decimal Total,
        DateOnly ProximoCobro,
        DateTime CreadaEn,
        string? MarcaTarjeta,
        string? UltimosDigitos)
    {
        public static SuscripcionDto From(Suscripcion s) =>
            new(s.Plan, s.Periodicidad, s.Estado, s.Total, s.ProximoCobro, s.CreadaEn, s.MarcaTarjeta, s.UltimosDigitos);
    }

    public record SuscribirRequest(
        [Required, EnumDataType(typeof(PlanSuscripcion))] PlanSuscripcion Plan,
        [Required, EnumDataType(typeof(Periodicidad))] Periodicidad Periodicidad,
        [Required, StringLength(200, MinimumLength = 10)] string Token,
        [TextoPlano, StringLength(20)] string? MarcaTarjeta,
        [RegularExpression(@"^[0-9]{4}$")] string? UltimosDigitos);

    /// <summary>Uso del plan: en Personal, cuántos comprobantes lleva este mes frente al límite.</summary>
    public record UsoPlanDto(PlanSuscripcion Plan, bool Profesional, int? LimiteComprobantes, int ComprobantesEsteMes, int? MesesHistorial);

    /// <summary>Estado de la cuenta tras suscribirse o cancelar (incluye el usuario con su plan actualizado).</summary>
    public record SuscripcionResultadoDto(UsuarioDto Usuario, SuscripcionDto? Suscripcion);
}
