namespace Back.Models;

public enum ParentescoCarga { PadreMadre, ConyugePareja, Hijo }
public class CargaFamiliar
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int Anio { get; set; }
    public string Identificacion { get; set; } = "";
    public string Nombre { get; set; } = "";
    public ParentescoCarga Parentesco { get; set; }
    public DateOnly FechaNacimiento { get; set; }
    public decimal IngresosGravados { get; set; }
    public bool Discapacidad { get; set; }
    public bool EnfermedadCatastrofica { get; set; }
    public bool DependenciaConfirmada { get; set; }
}
public enum FrecuenciaObligacion { Ninguna, Mensual, Semestral }
public class PerfilObligaciones
{
    public int UsuarioId { get; set; }
    public FrecuenciaObligacion Iva { get; set; }
    public FrecuenciaObligacion Retenciones { get; set; }
    public FrecuenciaObligacion Ats { get; set; }
    public bool Renta { get; set; } = true;
    public bool GastosPersonales { get; set; }
    public bool Sociedad { get; set; }
    public bool Especial { get; set; }
    public bool Galapagos { get; set; }
    public string Actividades { get; set; } = "";
    public string Feriados { get; set; } = "[]";
    public DateTime? ConfirmadoEn { get; set; }
}
public class PreparacionRenta
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int Anio { get; set; }
    public decimal SueldosGravados { get; set; }
    public decimal IessPersonal { get; set; }
    public decimal RetencionesSueldo { get; set; }
    public decimal OtrosIngresosGeneral { get; set; }
    public decimal OtrosGastosGeneral { get; set; }
    public decimal IngresosRimpeAjuste { get; set; }
    public decimal Exoneraciones { get; set; }
    public decimal AnticiposRenta { get; set; }
    public bool BeneficioEspecial { get; set; }
    public bool Galapagos { get; set; }
    public string Proyeccion { get; set; } = "[]";
    public string SustentoAjustes { get; set; } = "";
}
public class PreparacionIva
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public int Meses { get; set; } = 1;
    public decimal FactorCredito { get; set; } = 1;
    public decimal CreditoAnterior { get; set; }
    public decimal AjusteDebito { get; set; }
    public decimal AjusteCredito { get; set; }
    public string SustentoAjustes { get; set; } = "";
}
public enum TipoPreparacion { Iva, Renta, GastosPersonales, Ats, Retenciones }
public enum TratamientoRentaDocumento { Pendiente, GravadoGeneral, GravadoRimpe, Exento, Deducible, DeducibleRimpe, NoDeducible }
public class RevisionFiscalComprobante
{
    public int ComprobanteId { get; set; }
    public int UsuarioId { get; set; }
    public TratamientoRentaDocumento Renta { get; set; }
    public string CodigoSustento { get; set; } = "";
    public string FormaPago { get; set; } = "";
    public bool ParteRelacionada { get; set; }
    public string Observacion { get; set; } = "";
    public DateTime RevisadoEn { get; set; }
}
public enum EstadoPreparacion { Borrador, Revisado, PresentadoManual }
public class RevisionTributaria
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public TipoPreparacion Tipo { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public int Meses { get; set; }
    public string Huella { get; set; } = "";
    public string Contenido { get; set; } = "";
    public EstadoPreparacion Estado { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime? RevisadoEn { get; set; }
    public DateTime? PresentadoEn { get; set; }
    public string? ReferenciaPresentacion { get; set; }
}
