/**
 * Rebaja del Impuesto a la Renta por gastos personales (Ecuador, vigente desde 2023):
 * rebaja = 18 % del menor valor entre los gastos personales y el tope.
 * Tope = canasta familiar básica de enero del año × número de canastas según cargas familiares
 * (20 canastas si el contribuyente o una carga tiene enfermedad catastrófica, rara o huérfana).
 */

/** Canasta familiar básica de enero según el INEC (USD). Agregar cada año al publicarse. */
export const CANASTA_ENERO: Readonly<Record<number, number>> = {
  2026: 821.8,
};

export const PORCENTAJE_REBAJA = 0.18;

export function canastasPorCargas(cargas: number, enfermedadCatastrofica: boolean): number {
  if (enfermedadCatastrofica) return 20;
  if (cargas <= 0) return 7;
  if (cargas === 1) return 9;
  if (cargas === 2) return 11;
  if (cargas === 3) return 14;
  if (cargas === 4) return 17;
  return 20;
}

export interface TopeGastos {
  anio: number;
  canasta: number;
  canastas: number;
  tope: number;
  rebajaMaxima: number;
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

function redondear(valor: number): number {
  return Math.round(valor * 100) / 100;
}
