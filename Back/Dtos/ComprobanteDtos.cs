using System.ComponentModel.DataAnnotations;
using Back.Models;
using Back.Clasificacion;

namespace Back.Dtos
{
    public enum ResultadoImportacion { Importado, YaExistia, NoAutorizado, NoEncontrado, NoSoportado, Error, LimiteDelPlan }

    public record ClaveImportada(string ClaveAcceso, ResultadoImportacion Resultado, string? Detalle = null, Direccion? Direccion = null);

    public record ImportacionResponse(
        int ClavesEncontradas,
        int Importados,
        int YaExistian,
        int ConProblemas,
        int Ingresos,
        bool PerfilCompleto,
        List<ClaveImportada> Claves);

    public record ImportarClavesRequest([Required, StringLength(1024 * 1024)] string Texto);

    /// <param name="ANombreDelUsuario">
    /// Solo para gastos: si la factura está a nombre de la cédula/RUC del perfil. Null si el perfil no tiene identificación.
    /// </param>
    public record ComprobanteListaDto(
        int Id,
        string ClaveAcceso,
        Direccion Direccion,
        string TipoComprobante,
        string NumeroComprobante,
        DateOnly FechaEmision,
        string RucEmisor,
        string RazonSocialEmisor,
        string? NombreComercial,
        string IdentificacionComprador,
        string RazonSocialComprador,
        decimal Subtotal,
        decimal Iva,
        decimal Total,
        CategoriaGasto Categoria,
        bool? ANombreDelUsuario)
    {
        public CategoriaIngreso? CategoriaIngreso { get; init; }
        public static ComprobanteListaDto From(Comprobante c, Usuario perfil) => new(
            c.Id, c.ClaveAcceso, c.Direccion, TiposComprobante.Nombre(c.TipoComprobante), c.NumeroComprobante, c.FechaEmision,
            c.RucEmisor, c.RazonSocialEmisor, c.NombreComercial, c.IdentificacionComprador, c.RazonSocialComprador,
            c.Subtotal, c.Iva, c.Total, c.Categoria, ANombreDe(c, perfil)) { CategoriaIngreso = c.CategoriaIngreso };

        internal static bool? ANombreDe(Comprobante c, Usuario perfil)
        {
            if (c.Direccion == Direccion.Emitido) return null;
            var identificaciones = perfil.Identificaciones().ToList();
            return identificaciones.Count == 0 ? null : identificaciones.Contains(c.IdentificacionComprador);
        }
    }

    public record ComprobanteDetalleDto(string? Codigo, string Descripcion, decimal Cantidad, decimal PrecioUnitario, decimal Descuento, decimal Subtotal)
    {
        public int Id { get; init; }
        public CategoriaGasto? Categoria { get; init; }
        public CategoriaIngreso? CategoriaIngreso { get; init; }
        public FinalidadComprobante Finalidad { get; init; }
        public TratamientoIva TratamientoIva { get; init; }
        public decimal? Iva { get; init; }
        public decimal? TarifaIva { get; init; }
        public DateTime? RevisadoEn { get; init; }
        public SugerenciaClasificacion? Sugerencia { get; init; }
    }

    public record ComprobanteDto(
        int Id,
        string ClaveAcceso,
        Direccion Direccion,
        string TipoComprobante,
        string NumeroComprobante,
        string NumeroAutorizacion,
        DateTime? FechaAutorizacion,
        DateOnly FechaEmision,
        string RucEmisor,
        string RazonSocialEmisor,
        string? NombreComercial,
        string IdentificacionComprador,
        string RazonSocialComprador,
        decimal Subtotal,
        decimal BaseIva0,
        decimal BaseIvaGravada,
        decimal Iva,
        decimal OtrosImpuestos,
        decimal Descuento,
        decimal Propina,
        decimal Total,
        string? DocumentoModificado,
        CategoriaGasto Categoria,
        bool? ANombreDelUsuario,
        List<ComprobanteDetalleDto> Detalles)
    {
        public CategoriaIngreso? CategoriaIngreso { get; init; }
        public SugerenciaClasificacion? Sugerencia { get; init; }
        public FinalidadComprobante Finalidad { get; init; }
        public TratamientoIva TratamientoIva { get; init; }
        public DateTime? RevisadoEn { get; init; }
        public static ComprobanteDto From(Comprobante c, Usuario perfil) => new(
            c.Id, c.ClaveAcceso, c.Direccion, TiposComprobante.Nombre(c.TipoComprobante), c.NumeroComprobante, c.NumeroAutorizacion,
            c.FechaAutorizacion, c.FechaEmision, c.RucEmisor, c.RazonSocialEmisor, c.NombreComercial,
            c.IdentificacionComprador, c.RazonSocialComprador, c.Subtotal, c.BaseIva0, c.BaseIvaGravada, c.Iva,
            c.OtrosImpuestos, c.Descuento, c.Propina, c.Total, c.DocumentoModificado, c.Categoria,
            ComprobanteListaDto.ANombreDe(c, perfil),
            c.Detalles.Select(d => new ComprobanteDetalleDto(d.Codigo, d.Descripcion, d.Cantidad, d.PrecioUnitario, d.Descuento, d.Subtotal)
            { Id = d.Id, Categoria = d.Categoria, CategoriaIngreso = d.CategoriaIngreso, Finalidad = d.Finalidad, TratamientoIva = d.TratamientoIva,
              Iva = d.Iva, TarifaIva = d.TarifaIva, RevisadoEn = d.RevisadoEn,
              Sugerencia = ClasificadorComprobantes.Sugerir(new Comprobante { Direccion = c.Direccion, FechaEmision = c.FechaEmision, TipoComprobante = c.TipoComprobante,
                  Categoria = d.Categoria ?? CategoriaGasto.SinCategoria, CategoriaIngreso = d.CategoriaIngreso, Detalles = [d] }) }).ToList())
            { CategoriaIngreso = c.CategoriaIngreso, Sugerencia = ClasificadorComprobantes.Sugerir(c), Finalidad = c.Finalidad, TratamientoIva = c.TratamientoIva, RevisadoEn = c.RevisadoEn };
    }

    public record CambiarCategoriaRequest(
        [EnumDataType(typeof(CategoriaGasto))] CategoriaGasto Categoria,
        bool AplicarAlProveedor = true);

    public record CambiarCategoriaIngresoRequest([EnumDataType(typeof(CategoriaIngreso))] CategoriaIngreso Categoria);

    public record ProveedorDto(string Ruc, string RazonSocial, CategoriaGasto? Categoria, int Comprobantes, decimal Total);

    public record ClienteFacturadoDto(string Identificacion, string RazonSocial, int Comprobantes, decimal Total, decimal Iva, DateOnly UltimaFactura);

    public record TotalCategoriaDto(CategoriaGasto Categoria, int Comprobantes, decimal Total);

    public record TotalTerceroDto(string Identificacion, string RazonSocial, int Comprobantes, decimal Total);

    public record TotalMesDto(int Mes, int Comprobantes, decimal Total, decimal Iva, decimal Ingresos, decimal IvaCobrado);

    /// <summary>
    /// Totales netos (las notas de crédito restan) de gastos (comprobantes recibidos) e ingresos (emitidos).
    /// Si se pide un año sin mes, incluye el desglose mensual.
    /// </summary>
    public record ResumenDto(
        int Anio,
        int? Mes,
        int Comprobantes,
        decimal Total,
        decimal Iva,
        decimal TotalDeducible,
        int SinCategoria,
        int ANombreDeOtros,
        int ComprobantesEmitidos,
        decimal Ingresos,
        decimal IvaCobrado,
        List<TotalCategoriaDto> PorCategoria,
        List<TotalTerceroDto> PrincipalesProveedores,
        List<TotalTerceroDto> PrincipalesClientes,
        List<TotalMesDto>? PorMes);
}
