using Back.Helpers;
namespace Back.Tributacion;

/// <summary>Parámetros 2026 verificados en las fuentes oficiales que constan en docs/preparacion-tributaria.md.</summary>
public static class ReglasTributarias
{
    public const string Version = "EC-2026.1";
    public const decimal Canasta = 821.80m;
    public const decimal Sbu = 482m;
    public static void Anio(int anio) { if (anio != 2026) throw new BusinessException("Esta versión prepara el ejercicio 2026. Para otro año se necesita cargar y verificar sus reglas tributarias."); }
    public static decimal Dinero(decimal v) => decimal.Round(v, 2, MidpointRounding.AwayFromZero);
    public static int Canastas(int cargas, bool especial) => especial ? 100 : cargas switch { <= 0 => 7, 1 => 9, 2 => 11, 3 => 14, 4 => 17, _ => 20 };
    private static readonly (decimal Desde, decimal Base, decimal Porcentaje)[] Renta = [
        (0, 0, 0), (12208, 0, .05m), (15549, 167, .10m), (20188, 631, .12m), (26700, 1412, .15m),
        (35136, 2678, .20m), (46575, 4965, .25m), (62005, 8823, .30m), (82679, 15025, .35m), (109956, 24572, .37m)
    ];
    public static decimal ImpuestoGeneral(decimal baseImponible) { var t = Renta.Last(t => baseImponible >= t.Desde); return Dinero(t.Base + (baseImponible - t.Desde) * t.Porcentaje); }
    public static decimal ImpuestoRimpe(decimal ingresos)
    {
        if (ingresos < 0 || ingresos > 300000) throw new BusinessException("Los ingresos están fuera de los rangos RIMPE soportados. Revisa el régimen antes de preparar la declaración.");
        if (ingresos <= 2500) return 0; if (ingresos <= 5000) return 5; if (ingresos <= 10000) return 15; if (ingresos <= 15000) return 35; if (ingresos <= 20000) return 60;
        var tramos = new (decimal Desde, decimal Base, decimal Porcentaje)[] { (20000, 60, .01m), (50000, 360, .0125m), (75000, 672.50m, .015m), (100000, 1047.50m, .0175m), (200000, 2797.52m, .02m) };
        var t = tramos.Last(t => ingresos >= t.Desde); return Dinero(t.Base + (ingresos - t.Desde) * t.Porcentaje);
    }
}
