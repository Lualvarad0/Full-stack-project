import { CANASTA_ENERO, PORCENTAJE_REBAJA } from '@core/constants';
import { TopeGastos } from '@core/models';
import { redondear } from './formato';

export function canastasPorCargas(cargas: number, enfermedadCatastrofica: boolean): number {
  if (enfermedadCatastrofica) return 20;
  if (cargas <= 0) return 7;
  if (cargas === 1) return 9;
  if (cargas === 2) return 11;
  if (cargas === 3) return 14;
  if (cargas === 4) return 17;
  return 20;
}

/** Devuelve null si aún no hay canasta registrada para ese año. */
export function calcularTope(anio: number, cargas: number, enfermedadCatastrofica: boolean): TopeGastos | null {
  const canasta = CANASTA_ENERO[anio];
  if (canasta === undefined) return null;
  const canastas = canastasPorCargas(cargas, enfermedadCatastrofica);
  const tope = redondear(canasta * canastas);
  return { anio, canasta, canastas, tope, rebajaMaxima: redondear(tope * PORCENTAJE_REBAJA) };
}

export function rebajaEstimada(gastoDeducible: number, tope: TopeGastos): number {
  return redondear(Math.min(Math.max(gastoDeducible, 0), tope.tope) * PORCENTAJE_REBAJA);
}
