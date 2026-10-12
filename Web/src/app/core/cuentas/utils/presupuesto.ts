import { UMBRAL_AJUSTADO } from '../constants/cuentas';
import { CuentaPersonal, EstadoPresupuesto, GastoPorCategoria } from '../models/cuentas.model';

/** Cuánto del ingreso se va en cuentas (0–1+); null sin ingreso. */
export function proporcionDelIngreso(total: number, ingreso: number | null): number | null {
  if (ingreso === null || ingreso <= 0) return null;
  return total / ingreso;
}

export function estadoPresupuesto(total: number, ingreso: number | null): EstadoPresupuesto {
  const p = proporcionDelIngreso(total, ingreso);
  if (p === null) return 'sinIngreso';
  if (p > 1) return 'excedido';
  return p >= UMBRAL_AJUSTADO ? 'ajustado' : 'holgado';
}

/** Total por categoría, de mayor a menor, con su peso sobre el ingreso (o sobre el total si no hay ingreso). */
export function gastosPorCategoria(
  cuentas: readonly CuentaPersonal[],
  ingreso: number | null,
): GastoPorCategoria[] {
  const totales = new Map<CuentaPersonal['categoria'], number>();
  for (const c of cuentas) totales.set(c.categoria, (totales.get(c.categoria) ?? 0) + c.monto);
  const total = cuentas.reduce((s, c) => s + c.monto, 0);
  const base = ingreso && ingreso > 0 ? ingreso : total;
  return [...totales]
    .map(([categoria, monto]) => ({
      categoria,
      total: monto,
      porcentaje: base > 0 ? (monto / base) * 100 : 0,
    }))
    .sort((a, b) => b.total - a.total);
}

/** Pendiente y con vencimiento ya pasado dentro del mes mostrado. */
export function estaVencida(
  cuenta: CuentaPersonal,
  anio: number,
  mes: number,
  hoy = new Date(),
): boolean {
  if (cuenta.pagadaEl || !cuenta.diaVencimiento) return false;
  const ultimoDia = new Date(anio, mes, 0).getDate();
  const vence = new Date(anio, mes - 1, Math.min(cuenta.diaVencimiento, ultimoDia));
  return vence < new Date(hoy.getFullYear(), hoy.getMonth(), hoy.getDate());
}
