using Back.Models;
namespace Back.Contabilidad;

/// <summary>Plantilla que se copia a cada titular. Las cuentas no se comparten entre usuarios.</summary>
public static class PlanCuentasBase
{
    public static readonly (string Codigo, string Nombre, TipoCuentaContable Tipo)[] Cuentas = [
        ("1.1.01", "Caja y bancos", TipoCuentaContable.Activo),
        ("1.1.02", "Cuentas por cobrar", TipoCuentaContable.Activo),
        ("1.1.03", "Crédito tributario IVA", TipoCuentaContable.Activo),
        ("1.1.04", "Retenciones de renta a favor", TipoCuentaContable.Activo),
        ("1.1.05", "Retenciones de IVA a favor", TipoCuentaContable.Activo),
        ("2.1.01", "Cuentas por pagar", TipoCuentaContable.Pasivo),
        ("2.1.02", "IVA por pagar", TipoCuentaContable.Pasivo),
        ("2.1.03", "Retenciones de renta por pagar", TipoCuentaContable.Pasivo),
        ("2.1.04", "Retenciones de IVA por pagar", TipoCuentaContable.Pasivo),
        ("3.1.01", "Capital", TipoCuentaContable.Patrimonio),
        ("3.1.02", "Aportes y retiros del titular", TipoCuentaContable.Patrimonio),
        ("4.1.01", "Ventas", TipoCuentaContable.Ingreso),
        ("4.1.02", "Servicios", TipoCuentaContable.Ingreso),
        ("5.1.01", "Compras y gastos del negocio", TipoCuentaContable.Gasto),
        ("5.1.02", "Impuestos sin crédito tributario", TipoCuentaContable.Gasto)
    ];
}
