namespace Back.Models;

public enum TipoCuentaContable { Activo, Pasivo, Patrimonio, Ingreso, Gasto }
public enum EstadoAsiento { Borrador, Contabilizado, Anulado }
public class CuentaContable
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public TipoCuentaContable Tipo { get; set; }
    public bool Activa { get; set; } = true;
}
public class AsientoContable
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public DateOnly Fecha { get; set; }
    public string Concepto { get; set; } = "";
    public EstadoAsiento Estado { get; set; }
    public int? ComprobanteId { get; set; }
    public int? PagoComprobanteId { get; set; }
    public DateTime? ContabilizadoEn { get; set; }
    public string? MotivoAnulacion { get; set; }
    public List<LineaAsiento> Lineas { get; set; } = [];
}
public class LineaAsiento
{
    public int Id { get; set; }
    public int AsientoContableId { get; set; }
    public int CuentaContableId { get; set; }
    public CuentaContable Cuenta { get; set; } = null!;
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
}
