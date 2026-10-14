import { CategoriaGasto } from '@core/comprobantes';
import { CategoriaCuenta } from '@core/cuentas';
// Tipos de las animaciones de la landing (demo del panel y flujo de facturas).
// Las vistas son las mismas pantallas de la app (Resumen, Mis cuentas, Traer facturas, Ingresos, Gastos).
export type VistaDemo = 'resumen' | 'cuentas' | 'importar' | 'ingresos' | 'gastos';

/** Movimiento de ejemplo con los mismos datos que muestra la tabla de comprobantes. */
export interface MovimientoDemo {
  id: number;
  fecha: string;
  nombre: string;
  /** RUC del proveedor o identificación del cliente. */
  identificacion: string;
  numero: string;
  monto: number;
  ingreso: boolean;
  /** Categoría de gasto (mismos valores que la app); null en ingresos. */
  categoria: CategoriaGasto | null;
}

/** Cuenta de Mis cuentas con las mismas categorías que la app. */
export interface CuentaDemo {
  id: number;
  nombre: string;
  categoria: CategoriaCuenta;
  monto: number;
  diaVencimiento: number;
  /** Día del mes en que se pagó; null si está pendiente. */
  pagadaEl: number | null;
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

export type IconoFuncion =
  | 'cuentas'
  | 'factura'
  | 'ingresos'
  | 'categorias'
  | 'tope'
  | 'iva'
  | 'conciliacion'
  | 'bancos'
  | 'cartera';

/** Una función de la sección "Funciones" y el plan desde el que está incluida. */
export interface FuncionLanding {
  icono: IconoFuncion;
  titulo: string;
  texto: string;
  plan: 'Personal' | 'Profesional' | 'Contadores';
}
