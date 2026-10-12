// Tipos de la interfaz: cargas, avisos, tema, controles y gráficos.

export type Tema = 'light' | 'dark';
/** Lo que elige el usuario: claro, oscuro o el del sistema operativo. */
export type ModoTema = Tema | 'sistema';
export type TamanoTexto = 'normal' | 'grande';

export interface Aviso {
  id: number;
  tipo: 'exito' | 'error';
  texto: string;
}

export interface OpcionSegmento<T extends string | boolean> {
  valor: T;
  etiqueta: string;
}

/** Filtros de fecha y monto que se suman a la búsqueda y la categoría de cada lista. */

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
