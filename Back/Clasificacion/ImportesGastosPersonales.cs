using Back.Models;
namespace Back.Clasificacion;

public static class ImportesGastosPersonales
{
    public static bool EsRubro(CategoriaGasto? categoria) => categoria is CategoriaGasto.Vivienda or CategoriaGasto.Educacion or CategoriaGasto.Salud or CategoriaGasto.Alimentacion or CategoriaGasto.Vestimenta or CategoriaGasto.Turismo;
    /// <summary>Base neta del XML, sin IVA/ICE. No certifica elegibilidad ni aplica límites de rebaja.</summary>
    public static decimal BasePersonal(Comprobante c)
    {
        if (c.Direccion != Direccion.Recibido) return 0;
        if (c.Finalidad == FinalidadComprobante.Mixto)
            return c.Detalles.Where(d => d.RevisadoEn.HasValue && d.Finalidad == FinalidadComprobante.Personal && EsRubro(d.Categoria)).Sum(d => d.Subtotal) * c.Signo;
        return c.Finalidad == FinalidadComprobante.Personal && c.RevisadoEn.HasValue && EsRubro(c.Categoria) ? c.Subtotal * c.Signo : 0;
    }
}
