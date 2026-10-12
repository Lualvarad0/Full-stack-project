export interface FiltroRango {
  /** false: usa el mes de la barra superior; true: usa desde/hasta. */
  rango: boolean;
  desde: string;
  hasta: string;
  min: number | null;
  max: number | null;
}
