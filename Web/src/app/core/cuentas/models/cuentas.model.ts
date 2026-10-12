/** Mis cuentas: registro manual de cuentas del mes con el ingreso declarado (todos los planes). */

export type CategoriaCuenta =
  | 'Vivienda'
  | 'ServiciosBasicos'
  | 'Alimentacion'
  | 'Transporte'
  | 'Salud'
  | 'Educacion'
  | 'Deudas'
  | 'Entretenimiento'
  | 'Otros';

export interface CuentaPersonal {
  id: number;
  nombre: string;
  categoria: CategoriaCuenta;
  monto: number;
  diaVencimiento: number | null;
  /** Día en que se pagó (yyyy-MM-dd); null si está pendiente. */
  pagadaEl: string | null;
}

export interface MesCuentas {
  anio: number;
  mes: number;
  /** Lo que el usuario dijo que gana (del mes o el último que declaró); null si nunca lo dijo. */
  ingreso: number | null;
  total: number;
  pagado: number;
  pendiente: number;
  /** Ingreso menos el total de cuentas; null sin ingreso. */
  disponible: number | null;
  cuentas: CuentaPersonal[];
}

export interface CuentaPersonalRequest {
  anio: number;
  mes: number;
  nombre: string;
  categoria: CategoriaCuenta;
  monto: number;
  diaVencimiento: number | null;
}

/** Borrador del formulario de cuenta (sin id si es nueva). */
export interface CuentaBorrador {
  id?: number;
  nombre: string;
  categoria: CategoriaCuenta;
  monto: number;
  diaVencimiento: number | null;
}

export interface GastoPorCategoria {
  categoria: CategoriaCuenta;
  total: number;
  /** Porcentaje del ingreso (o del total si no hay ingreso). */
  porcentaje: number;
}

export type EstadoPresupuesto = 'sinIngreso' | 'holgado' | 'ajustado' | 'excedido';
