import { FiltroRango } from '../models/filtro-rango.model';
/** Convierte el filtro en parámetros para la API (el mes se usa solo si no hay rango). */
export function parametrosFiltro(f: FiltroRango, anio: number, mes: number) {
  const usarRango = f.rango && (f.desde || f.hasta);
  return {
    anio: usarRango ? undefined : anio,
    mes: usarRango ? undefined : mes,
    desde: usarRango ? f.desde || undefined : undefined,
    hasta: usarRango ? f.hasta || undefined : undefined,
    min: f.min ?? undefined,
    max: f.max ?? undefined,
  };
}

/** "03/01/2026 – 31/03/2026" si hay un rango activo; null si se usa el mes. */
export function etiquetaRango(f: FiltroRango): string | null {
  if (!f.rango || (!f.desde && !f.hasta)) return null;
  const fecha = (fechaIso: string) => fechaIso.split('-').reverse().join('/');
  if (f.desde && f.hasta) return `${fecha(f.desde)} – ${fecha(f.hasta)}`;
  return f.desde ? `desde el ${fecha(f.desde)}` : `hasta el ${fecha(f.hasta)}`;
}

export function hayFiltroRango(f: FiltroRango): boolean {
  return (f.rango && !!(f.desde || f.hasta)) || f.min !== null || f.max !== null;
}
