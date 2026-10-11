// Tipos de las animaciones de la landing (demo del panel y flujo de facturas).
export type VistaDemo = 'resumen' | 'gastos' | 'ingresos' | 'buzon';

export type CategoriaDemo = 'Vivienda' | 'Salud' | 'Alimentación' | 'Educación' | 'Vestimenta';

export interface MovimientoDemo {
  id: number;
  fecha: string;
  nombre: string;
  monto: number;
  ingreso: boolean;
  categoria: CategoriaDemo | null;
}

export interface LlegadaDemo {
  id: number;
  movimiento: MovimientoDemo;
  /** 0 correo recibido · 1 XML leído · 2 verificado con el SRI · 3 registrado */
  paso: number;
}

export type TonoFlujo = 'gasto' | 'ingreso' | 'sri';

export type IconoFlujo = 'correo' | 'factura' | 'sri' | 'gastos' | 'ingresos' | 'tope' | 'iva';

export interface NodoFlujo {
  icono: IconoFlujo;
  titulo: string;
  detalle: string;
  tono: TonoFlujo;
  x: number;
  y: number;
}

export interface CableFlujo {
  d: string;
  tono: TonoFlujo;
  /** Segundo del ciclo en el que el punto de luz sale por este cable. */
  inicio: number;
}

export interface DiagramaFlujo {
  ancho: number;
  alto: number;
  centro: { x: number; y: number };
  fuentes: NodoFlujo[];
  salidas: NodoFlujo[];
  cables: CableFlujo[];
}

export type IconoFuncion = 'ingresos' | 'categorias' | 'tope' | 'iva' | 'conciliacion' | 'filtros';
