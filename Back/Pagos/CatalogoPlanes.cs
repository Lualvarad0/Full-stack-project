using Back.Models;

namespace Back.Pagos
{
    /// <summary>Precio de un plan de pago antes de IVA, con su IVA (15 %) y total.</summary>
    public record PrecioPlan(PlanSuscripcion Plan, Periodicidad Periodicidad, decimal Subtotal, decimal Iva, decimal Total);

    /// <summary>
    /// Precios de Nudo (fuente única: el frontend los pide a la API). El plan anual cuesta 10 meses.
    /// </summary>
    public static class CatalogoPlanes
    {
        public const decimal TarifaIva = 0.15m;
        public const int DiasDePrueba = 14;

        private static readonly Dictionary<(PlanSuscripcion, Periodicidad), decimal> Subtotales = new()
        {
            [(PlanSuscripcion.Profesional, Periodicidad.Mensual)] = 14.99m,
            [(PlanSuscripcion.Profesional, Periodicidad.Anual)] = 149.00m,
            [(PlanSuscripcion.Contadores, Periodicidad.Mensual)] = 49.00m,
            [(PlanSuscripcion.Contadores, Periodicidad.Anual)] = 490.00m,
        };

        public static IEnumerable<PrecioPlan> Todos() => Subtotales.Keys.Select(k => Precio(k.Item1, k.Item2));

        public static PrecioPlan Precio(PlanSuscripcion plan, Periodicidad periodicidad)
        {
            if (!Subtotales.TryGetValue((plan, periodicidad), out var subtotal))
                throw new ArgumentException($"El plan {plan} no es de pago.", nameof(plan));
            var iva = Math.Round(subtotal * TarifaIva, 2, MidpointRounding.AwayFromZero);
            return new PrecioPlan(plan, periodicidad, subtotal, iva, subtotal + iva);
        }

        public static bool EsDePago(PlanSuscripcion plan) => plan != PlanSuscripcion.Personal;
    }
}
