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
