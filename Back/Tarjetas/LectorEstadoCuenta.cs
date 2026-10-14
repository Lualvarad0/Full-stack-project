using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Back.Helpers;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Exceptions;

namespace Back.Tarjetas;

/// <summary>Lo que se pudo leer de un estado de cuenta; cada dato es null si no se encontró.</summary>
public record EstadoLeido(decimal? SaldoTotal, decimal? PagoMinimo, DateOnly? FechaMaximaPago, DateOnly? FechaCorte, IReadOnlyList<string> Ultimos4)
{
    public bool TieneDatos => SaldoTotal is not null || FechaMaximaPago is not null;
}

/// <summary>
/// Lee el estado de cuenta (PDF) de una tarjeta de crédito sin depender del formato de cada banco: busca las
/// etiquetas habituales en Ecuador ("Total a pagar", "Pago mínimo", "Fecha máxima de pago", "Fecha de corte")
/// y toma el valor o la fecha que las sigue. El usuario confirma lo leído antes de guardarlo.
/// </summary>
public static partial class LectorEstadoCuenta
{
    public const long MaxBytes = 10 * 1024 * 1024;
    private const int MaxPaginas = 8;
    private const int Ventana = 140;

    private static readonly string[] EtiquetasSaldo =
    [
        "valor total a pagar", "total a pagar", "saldo total", "pago total", "total adeudado", "deuda total", "nuevo saldo",
        "saldo actual", "saldo al corte", "saldo a la fecha de corte",
    ];
    private static readonly string[] EtiquetasMinimo = ["valor minimo a pagar", "pago minimo", "minimo a pagar", "cuota minima"];
    private static readonly string[] EtiquetasFechaMaxima =
        ["fecha maxima de pago", "fecha maxima pago", "fecha limite de pago", "fecha limite pago", "pagar hasta", "fecha de pago", "vence el"];
    private static readonly string[] EtiquetasCorte =
        ["fecha de corte", "fecha corte", "fecha de facturacion", "fecha de estado de cuenta", "fecha de emision"];

    private static readonly Dictionary<string, int> Meses = new()
    {
        ["ene"] = 1, ["feb"] = 2, ["mar"] = 3, ["abr"] = 4, ["may"] = 5, ["jun"] = 6, ["jul"] = 7, ["ago"] = 8,
        ["sep"] = 9, ["set"] = 9, ["oct"] = 10, ["nov"] = 11, ["dic"] = 12,
    };

    /// <summary>
    /// Texto del PDF. Prueba sin clave y luego con cada clave (muchos bancos protegen el PDF con la cédula).
    /// Lanza BusinessException si el PDF tiene clave y ninguna sirve, o si no es un PDF legible.
    /// </summary>
    public static string ExtraerTexto(byte[] pdf, IEnumerable<string?> claves)
    {
        if (pdf.Length == 0 || pdf.Length > MaxBytes) throw new BusinessException("El estado de cuenta debe ser un PDF de hasta 10 MB.");
        var protegido = false;
        foreach (var clave in new string?[] { null }.Concat(claves.Where(c => !string.IsNullOrWhiteSpace(c))).Distinct())
        {
            try
            {
                using var documento = PdfDocument.Open(pdf, clave is null ? null : new ParsingOptions { Password = clave });
                var texto = new StringBuilder();
                foreach (var pagina in documento.GetPages().Take(MaxPaginas))
                    texto.AppendLine(ContentOrderTextExtractor.GetText(pagina));
                return texto.ToString();
            }
            catch (PdfDocumentEncryptedException)
            {
                protegido = true;
            }
            catch (Exception e) when (e is PdfDocumentFormatException or InvalidOperationException or ArgumentException)
            {
                throw new BusinessException("No pudimos leer el PDF. Revisa que sea el estado de cuenta original del banco.");
            }
        }

        throw new BusinessException(protegido
            ? "El PDF tiene clave. Escribe la clave del estado de cuenta (suele ser tu cédula) e inténtalo de nuevo."
            : "No pudimos leer el PDF.");
    }

    public static EstadoLeido Analizar(string texto)
    {
        var normal = Normalizar(texto);
        return new EstadoLeido(
            BuscarMonto(normal, EtiquetasSaldo),
            BuscarMonto(normal, EtiquetasMinimo),
            BuscarFecha(normal, EtiquetasFechaMaxima),
            BuscarFecha(normal, EtiquetasCorte),
            UltimosDigitos().Matches(normal).Select(m => m.Groups[1].Value).Distinct().ToList());
    }

    private static decimal? BuscarMonto(string texto, string[] etiquetas)
    {
        foreach (var etiqueta in etiquetas)
            foreach (var tramo in Tramos(texto, etiqueta))
                if (Monto().Match(tramo) is { Success: true } m && LeerMonto(m.Groups[1].Value) is { } valor)
                    return valor;
        return null;
    }

    private static DateOnly? BuscarFecha(string texto, string[] etiquetas)
    {
        foreach (var etiqueta in etiquetas)
            foreach (var tramo in Tramos(texto, etiqueta))
                if (LeerFecha(tramo) is { } fecha)
                    return fecha;
        return null;
    }

    /// <summary>El texto que sigue a cada aparición de la etiqueta (ahí suele estar su valor).</summary>
    private static IEnumerable<string> Tramos(string texto, string etiqueta)
    {
        for (var i = texto.IndexOf(etiqueta, StringComparison.Ordinal); i >= 0; i = texto.IndexOf(etiqueta, i + 1, StringComparison.Ordinal))
        {
            var desde = i + etiqueta.Length;
            yield return texto.Substring(desde, Math.Min(Ventana, texto.Length - desde));
        }
    }

    /// <summary>"1,234.56" o "1.234,56": el último separador seguido de dos dígitos es el decimal.</summary>
    private static decimal? LeerMonto(string crudo)
    {
        var limpio = crudo.Replace(" ", "");
        var decimales = limpio.Length >= 3 && (limpio[^3] == '.' || limpio[^3] == ',');
        var entero = decimales ? limpio[..^3] : limpio;
        var digitos = entero.Replace(".", "").Replace(",", "") + (decimales ? "." + limpio[^2..] : "");
        return decimal.TryParse(digitos, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor) && valor < 10_000_000m
            ? valor : null;
    }

    private static DateOnly? LeerFecha(string tramo)
    {
        if (FechaIso().Match(tramo) is { Success: true } iso
            && Crear(int.Parse(iso.Groups[1].Value), int.Parse(iso.Groups[2].Value), int.Parse(iso.Groups[3].Value)) is { } f1)
            return f1;

        if (Fecha().Match(tramo) is not { Success: true } m) return null;
        var dia = int.Parse(m.Groups[1].Value);
        var textoMes = m.Groups[2].Value;
        var mes = int.TryParse(textoMes, out var n) ? n : Meses.GetValueOrDefault(textoMes[..Math.Min(3, textoMes.Length)]);
        var anio = int.Parse(m.Groups[3].Value);
        return Crear(anio < 100 ? 2000 + anio : anio, mes, dia);
    }

    private static DateOnly? Crear(int anio, int mes, int dia) =>
        anio is >= 2000 and <= 2100 && mes is >= 1 and <= 12 && dia >= 1 && dia <= DateTime.DaysInMonth(anio, mes)
            ? new DateOnly(anio, mes, dia) : null;

    private static string Normalizar(string texto)
    {
        var sinTildes = texto.Normalize(NormalizationForm.FormD)
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark);
        return Espacios().Replace(new string(sinTildes.ToArray()).ToLowerInvariant(), " ");
    }

    // Un valor con decimales, opcionalmente con $: "$ 1,234.56", "1.234,56", "245.10".
    [GeneratedRegex(@"(?<![\d.,])\$?\s?(\d{1,3}(?:[.,]\d{3})+[.,]\d{2}|\d+[.,]\d{2})(?![\d])")]
    private static partial Regex Monto();

    [GeneratedRegex(@"(?<!\d)(\d{4})-(\d{1,2})-(\d{1,2})(?!\d)")]
    private static partial Regex FechaIso();

    // 15/10/2026, 15-10-26, 15 oct 2026, 15-octubre-2026
    [GeneratedRegex(@"(?<!\d)(\d{1,2})[/\-. ]+(\d{1,2}|[a-z]{3,10})[/\-. ]+(\d{4}|\d{2})(?!\d)")]
    private static partial Regex Fecha();

    // Número enmascarado: "xxxx xxxx xxxx 1234", "**** 1234", "4567 89** **** 1234", "terminada en 1234".
    [GeneratedRegex(@"(?:[x*•]{2,}[\s-]*|terminad[ao] en\s+)(\d{4})(?!\d)")]
    private static partial Regex UltimosDigitos();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();
}
