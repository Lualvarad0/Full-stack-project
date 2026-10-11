// Tipos de la interfaz: cargas, avisos, tema, controles y gráficos.
export interface Carga<T> {
  cargando: boolean;
  datos: T | null;
  error: string | null;
}

export type Tema = 'light' | 'dark';

export interface Aviso {
  id: number;
  tipo: 'exito' | 'error';
  texto: string;
}

export interface TopeGastos {
  anio: number;
  canasta: number;
  canastas: number;
  tope: number;
  rebajaMaxima: number;
}

export interface OpcionSegmento<T extends string | boolean> {
  valor: T;
  etiqueta: string;
}

/** Filtros de fecha y monto que se suman a la búsqueda y la categoría de cada lista. */
export interface FiltroRango {
  /** false: usa el mes de la barra superior; true: usa desde/hasta. */
  rango: boolean;
  desde: string;
  hasta: string;
  min: number | null;
  max: number | null;
}

export interface Barra {
  etiqueta: string;
  valor: number;
  color: string;
  detalle?: string;
}

export interface PuntoComparado {
  mes: number;
  etiqueta: string;
  a: number;
  b: number;
}

export interface PuntoMes {
  mes: number;
  etiqueta: string;
  valor: number;
  detalle?: string;
}
