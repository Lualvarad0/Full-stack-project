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
  /** Qué respalda el día de pago: factura, correo o lo que dijo el usuario. */
  respaldo?: RespaldoPago | null;
  /** Quedó pendiente al cierre y pasó al mes siguiente. */
  trasladada?: boolean;
  /** Deuda que viene del mes anterior. */
  delMesAnterior?: boolean;
  /** Deuda de una tarjeta de crédito (según su estado de cuenta). */
  tarjetaCreditoId?: number | null;
}

export type RespaldoPago = 'Manual' | 'Factura' | 'Correo';

/** Factura o correo que parece respaldar el pago de una cuenta pendiente al cierre. */
export interface SugerenciaPago {
  tipo: RespaldoPago;
  fecha: string;
  descripcion: string;
  monto: number | null;
  comprobanteId: number | null;
  correoId: number | null;
}

/** Cuentas que quedaron pendientes en el mes que terminó (planes de pago). */
export interface CierreMes {
  anio: number;
  mes: number;
  pendientes: { cuenta: CuentaPersonal; sugerencia: SugerenciaPago | null }[];
}

export interface DecisionCierre {
  cuentaId: number;
  accion: 'Pagada' | 'Trasladar';
  fecha?: string | null;
  comprobanteId?: number | null;
  correoId?: number | null;
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
  /** Lo que quedó pendiente y pasó al mes siguiente (no cuenta como pendiente). */
  trasladado?: number;
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
