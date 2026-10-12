using System.ComponentModel.DataAnnotations;
using Back.Models;
using Back.Seguridad;
namespace Back.Dtos;

public record CargaFamiliarRequest([Range(2026, 2026)] int Anio, [Required, RegularExpression("^[A-Za-z0-9]{5,20}$")] string Identificacion,
    [Required, TextoPlano, StringLength(200)] string Nombre, [EnumDataType(typeof(ParentescoCarga))] ParentescoCarga Parentesco, DateOnly FechaNacimiento,
    [Range(0, 999999999)] decimal IngresosGravados, bool Discapacidad, bool EnfermedadCatastrofica, bool DependenciaConfirmada);
public record PerfilObligacionesRequest([EnumDataType(typeof(FrecuenciaObligacion))] FrecuenciaObligacion Iva,
    [EnumDataType(typeof(FrecuenciaObligacion))] FrecuenciaObligacion Retenciones, [EnumDataType(typeof(FrecuenciaObligacion))] FrecuenciaObligacion Ats,
    bool Renta, bool GastosPersonales, bool Sociedad, bool Especial, bool Galapagos, [TextoPlano, StringLength(2000)] string Actividades,
    [Required, MaxLength(100)] List<DateOnly> Feriados, bool Confirmado);
public record RubroProyeccion([EnumDataType(typeof(CategoriaGasto))] CategoriaGasto Categoria, [Range(0, 999999999)] decimal Importe);
public record RentaRequest([Range(0, 999999999)] decimal SueldosGravados, [Range(0, 999999999)] decimal IessPersonal,
    [Range(0, 999999999)] decimal RetencionesSueldo, [Range(0, 999999999)] decimal OtrosIngresosGeneral,
    [Range(0, 999999999)] decimal OtrosGastosGeneral, [Range(-999999999, 999999999)] decimal IngresosRimpeAjuste,
    [Range(0, 999999999)] decimal Exoneraciones, [Range(0, 999999999)] decimal AnticiposRenta,
    bool BeneficioEspecial, bool Galapagos, [Required, MaxLength(6)] List<RubroProyeccion> Proyeccion,
    [TextoPlano, StringLength(2000)] string SustentoAjustes);
public record IvaRequest([Range(0, 1)] decimal FactorCredito, [Range(0, 999999999)] decimal CreditoAnterior,
    [Range(-999999999, 999999999)] decimal AjusteDebito, [Range(-999999999, 999999999)] decimal AjusteCredito,
    [TextoPlano, StringLength(2000)] string SustentoAjustes);
public record VencimientoTributarioDto(string Obligacion, string Periodo, DateOnly Fecha, string Estado, int? RevisionId);
public record RubroGastoDto(CategoriaGasto Categoria, decimal Real, decimal Proyectado);
public record GastosPersonalesDto(int Anio, string Version, int Cargas, decimal Canasta, int Canastas, decimal Tope,
    decimal Real, decimal Proyectado, decimal RebajaPotencial, decimal RebajaProyectada, List<RubroGastoDto> Rubros, List<string> Pendientes);
public record RentaPreparadaDto(int Anio, string Version, RegimenTributario? Regimen, decimal IngresosFacturados,
    decimal GastosNegocio, decimal IngresosGeneral, decimal BaseGeneral, decimal IngresosRimpe,
    decimal ImpuestoGeneral, decimal ImpuestoRimpe, decimal Rebaja, decimal Retenciones, decimal Anticipos, decimal Pagar,
    decimal SaldoFavor, GastosPersonalesDto GastosPersonales, RentaRequest Parametros, List<string> Pendientes);
public record TarifaIvaDto(string Tarifa, decimal VentasBase, decimal VentasIva, decimal ComprasBase, decimal ComprasIva);
public record IvaPreparadoDto(int Anio, int Mes, int Meses, string Version, List<TarifaIvaDto> Tarifas,
    decimal Debito, decimal CreditoElegible, decimal CreditoAplicado, decimal RetencionesRecibidas,
    decimal RetencionesEmitidasIva, decimal RetencionesEmitidasRenta, decimal Pagar, decimal SaldoFavor,
    IvaRequest Parametros, List<string> Pendientes);
public record RevisarTributoRequest([EnumDataType(typeof(TipoPreparacion))] TipoPreparacion Tipo, [Range(2026, 2026)] int Anio,
    [Range(1, 12)] int Mes, [Range(1, 12)] int Meses, bool Confirmado);
public record PresentacionManualRequest([Required, TextoPlano, StringLength(150, MinimumLength = 5)] string Referencia, DateOnly Fecha);
public record RevisionFiscalRequest([EnumDataType(typeof(TratamientoRentaDocumento))] TratamientoRentaDocumento Renta,
    [RegularExpression("^[0-9]{2}$")] string? CodigoSustento, [RegularExpression("^[0-9]{2}$")] string? FormaPago,
    bool ParteRelacionada, [TextoPlano, StringLength(1000)] string Observacion);
public record DocumentoFiscalDto(int Id, DateOnly Fecha, string Numero, string Tercero, Direccion Direccion,
    FinalidadComprobante Finalidad, decimal Subtotal, decimal Iva, TratamientoRentaDocumento Renta, string CodigoSustento,
    string FormaPago, bool ParteRelacionada, string Observacion);
