using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Back.Dtos;
using Back.Helpers;
namespace Back.Contabilidad;

public static class CsvBancario
{
    public static IEnumerable<string[]> Filas(string texto)
    {
        var separador = texto.Split('\n')[0].Contains(';') ? ';' : ',';
        var fila = new List<string>(); var campo = new StringBuilder(); var comillas = false;
        for (var i = 0; i < texto.Length; i++) {
            var c = texto[i];
            if (c == '"') { if (comillas && i + 1 < texto.Length && texto[i + 1] == '"') { campo.Append('"'); i++; } else comillas = !comillas; }
            else if (!comillas && (c == separador || c == '\n')) {
                fila.Add(campo.ToString().Trim().Trim('\uFEFF')); campo.Clear();
                if (c == '\n') { if (fila.Any(x => x.Length > 0)) yield return fila.ToArray(); fila.Clear(); }
            } else if (c != '\r') campo.Append(c);
        }
        if (comillas) throw new BusinessException("El CSV tiene comillas sin cerrar.");
        if (campo.Length > 0 || fila.Count > 0) { fila.Add(campo.ToString().Trim()); if (fila.Any(x => x.Length > 0)) yield return fila.ToArray(); }
    }
    public static List<MovimientoBancarioRequest> Leer(string texto)
    {
        var filas = Filas(texto).Take(2502).ToList();
        if (filas.Count is < 2 or > 2501 || !filas[0].SequenceEqual(new[] { "id", "fecha", "concepto", "importe", "referencia" }, StringComparer.OrdinalIgnoreCase))
            throw new BusinessException("Usa el CSV con columnas id, fecha, concepto, importe, referencia y hasta 2500 movimientos.");
        var resultado = new List<MovimientoBancarioRequest>();
        foreach (var f in filas.Skip(1)) {
            if (f.Length != 5 || !DateOnly.TryParseExact(f[1], new[] { "yyyy-MM-dd", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
                || !decimal.TryParse(f[3], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var importe))
                throw new BusinessException($"Revisa la fila {resultado.Count + 2}: fecha o importe no válido. Usa punto decimal.");
            var r = new MovimientoBancarioRequest(f[0], fecha, f[2], f[4], importe);
            if (!Validator.TryValidateObject(r, new ValidationContext(r), [], true)) throw new BusinessException($"Revisa los campos de la fila {resultado.Count + 2}.");
            // Los record posicionales usan atributos de parámetro; comprobación explícita para importación CSV.
            if (f[0].Length is < 1 or > 100 || f[2].Length is < 1 or > 300 || f[4].Length > 100 || f.Any(x => x.Contains('<') || x.Contains('>')) || importe is < -999999999 or > 999999999)
                throw new BusinessException($"Revisa la longitud o contenido de la fila {resultado.Count + 2}.");
            resultado.Add(r);
        }
        return resultado;
    }
}
