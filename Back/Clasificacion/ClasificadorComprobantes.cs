using Back.Models;

namespace Back.Clasificacion;

public record SugerenciaClasificacion(CategoriaGasto? Categoria, CategoriaIngreso? CategoriaIngreso,
    string Estado, string Motivo, string VersionReglas);

public static class ClasificadorComprobantes
{
    public static SugerenciaClasificacion Sugerir(Comprobante c)
    {
        SugerenciaClasificacion Resultado(CategoriaGasto? gasto, CategoriaIngreso? ingreso, string estado, string motivo) =>
            new(gasto, ingreso, estado, motivo, ReglasGastosPersonales.Version);
        if (c.Finalidad == FinalidadComprobante.Mixto)
            return Resultado(null, null, c.RevisadoEn.HasValue ? "Confirmada" : "SinEvidencia", "Este comprobante se clasifica por partidas. Revisa sus categorías y finalidades en el detalle.");
        if (c.Direccion == Direccion.Emitido)
        {
            if (c.CategoriaIngreso.HasValue) return Resultado(null, c.CategoriaIngreso, "Confirmada", "Categoría de ingreso elegida por ti.");
            var lineas = c.Detalles.Where(d => d.Subtotal != 0).ToList();
            bool Servicio(string texto) => ReglasGastosPersonales.Contiene(texto, "servicio", "servicios", "consultoria", "asesoria", "honorarios", "desarrollo de software");
            bool Venta(string texto) => ReglasGastosPersonales.Contiene(texto, "venta", "camisa", "camisas", "pantalon", "zapatos", "arroz", "leche", "libro", "libros", "medicamento", "medicamentos");
            var textos = lineas.Select(d => ReglasGastosPersonales.Normalizar(d.Descripcion)).ToList();
            if (textos.Count > 0 && textos.All(t => Servicio(t) && !Venta(t)))
                return Resultado(null, Models.CategoriaIngreso.Servicios, "Pendiente", "Los conceptos describen servicios. Esta es una categoría comercial; confirma su uso.");
            if (textos.Count > 0 && textos.All(t => Venta(t) && !Servicio(t)))
                return Resultado(null, Models.CategoriaIngreso.Ventas, "Pendiente", "Los conceptos describen venta de bienes. Esta es una categoría comercial; confirma su uso.");
            return Resultado(null, null, "SinEvidencia", "El comprobante es un ingreso porque lo emitiste. Elige ventas, servicios u otros ingresos según la operación; no cambia su tratamiento tributario.");
        }
        if (c.Categoria != CategoriaGasto.SinCategoria)
            return Resultado(c.Categoria, null, "Confirmada", "Categoría elegida por ti o aplicada por tu regla de proveedor.");
        if (c.TipoComprobante != TiposComprobante.Factura)
            return Resultado(null, null, "SinEvidencia", "Revisa la categoría del documento que modifica esta nota antes de clasificarla.");
        if (c.FechaEmision.Year != 2026)
            return Resultado(null, null, "SinEvidencia", "Las reglas disponibles corresponden a 2026. Revisa las condiciones del año de emisión.");
        var detalles = c.Detalles.Where(d => d.Subtotal != 0).ToList();
        if (detalles.Count == 0) return Resultado(null, null, "SinEvidencia", "No hay detalle suficiente para proponer una categoría.");
        var coincidencias = detalles.Select(d => ReglasGastosPersonales.Coincidencias(d.Descripcion)).ToList();
        if (coincidencias.Any(x => x.Length != 1) || coincidencias.SelectMany(x => x).Distinct().Count() != 1)
            return Resultado(null, null, "SinEvidencia", "Hay conceptos mezclados, ambiguos o sin coincidencia. Revisa el detalle; no asignamos una categoría a toda la factura.");
        var categoria = coincidencias[0][0];
        var motivo = categoria == CategoriaGasto.Turismo
            ? "Los conceptos corresponden a alojamiento o turismo. Verifica que sea turismo nacional y que el establecimiento esté registrado."
            : "Todos los conceptos con importe coinciden con este rubro de gastos personales. Verifica que sean gastos personales realizados en Ecuador y que el comprobante corresponda a ti o a tus cargas registradas.";
        return Resultado(categoria, null, "Pendiente", motivo);
    }
}
