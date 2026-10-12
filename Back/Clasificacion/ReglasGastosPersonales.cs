using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Back.Models;

namespace Back.Clasificacion;

/// <summary>Rubros personales del SRI; la coincidencia textual es una sugerencia, no validación tributaria.</summary>
public static class ReglasGastosPersonales
{
    public const string Version = "GP-2026.1";
    private static readonly ReglaCategoria[] Reglas = [
        new ReglaTerminos(CategoriaGasto.Vivienda, "arriendo de vivienda", "arriendo vivienda", "alquiler de vivienda", "intereses hipotecarios", "agua potable", "energia electrica", "servicio electrico"),
        new ReglaTerminos(CategoriaGasto.Educacion, "matricula escolar", "matricula universitaria", "pension escolar", "utiles escolares", "transporte escolar", "libro", "libros", "curso", "cursos", "entrada al museo", "entrada al teatro"),
        new ReglaTerminos(CategoriaGasto.Salud, "consulta medica", "consulta odontologica", "medicamento", "medicamentos", "examen medico", "examen de laboratorio", "terapia fisica", "seguro de salud", "seguro medico", "consulta veterinaria"),
        new ReglaTerminos(CategoriaGasto.Alimentacion, "arroz", "leche", "pan", "huevos", "frutas", "verduras", "pollo", "carne", "pescado", "almuerzo", "desayuno", "cena", "servicio de alimentacion"),
        new ReglaTerminos(CategoriaGasto.Vestimenta, "camisa", "camisas", "pantalon", "pantalones", "zapatos", "camiseta", "camisetas", "vestido", "vestidos", "ropa interior"),
        new ReglaTurismo(),
    ];

    public static CategoriaGasto[] Coincidencias(string texto)
    {
        var normal = Normalizar(texto);
        // Evitar inferir gastos personales de compras con usos o condiciones ambiguos.
        if (Contiene(normal, "mascota", "mascotas", "alcohol", "cerveza", "cervezas", "vino", "cigarrillos", "combustible", "internacional", "exportacion", "jabon", "detergente", "juguete", "repuesto", "limpieza")) return [];
        return Reglas.Where(r => r.Coincide(normal)).Select(r => r.Categoria).Distinct().ToArray();
    }

    internal static string Normalizar(string texto) => new string(texto.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray())
        .ToLowerInvariant().Normalize(NormalizationForm.FormC);

    internal static bool Contiene(string texto, params string[] terminos) => terminos.Any(t =>
        Regex.IsMatch(texto, @"(?<![a-z0-9])" + Regex.Escape(t) + @"(?![a-z0-9])", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)));

    private abstract class ReglaCategoria(CategoriaGasto categoria)
    {
        public CategoriaGasto Categoria { get; } = categoria;
        public abstract bool Coincide(string texto);
    }
    private sealed class ReglaTerminos(CategoriaGasto categoria, params string[] terminos) : ReglaCategoria(categoria)
    {
        public override bool Coincide(string texto) => Contiene(texto, terminos);
    }
    private sealed class ReglaTurismo() : ReglaCategoria(CategoriaGasto.Turismo)
    {
        // País, registro del establecimiento y finalidad turística requieren revisión humana.
        public override bool Coincide(string texto) => Contiene(texto, "alojamiento", "hospedaje", "paquete turistico", "tour nacional");
    }
}
