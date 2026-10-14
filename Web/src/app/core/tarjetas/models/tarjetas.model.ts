/** Tarjetas de crédito y estados de cuenta (planes de pago); contratos de /api/tarjetas. */

export type OrigenEstadoCuenta = 'Subido' | 'Correo' | 'Manual';
export type EstadoRevision = 'PorConfirmar' | 'Confirmado' | 'Descartado';

export interface EstadoCuenta {
  id: number;
  tarjetaId: number;
  fechaCorte: string | null;
  fechaMaximaPago: string | null;
  saldoTotal: number | null;
  pagoMinimo: number | null;
  origen: OrigenEstadoCuenta;
  estado: EstadoRevision;
  /** Deuda creada en Mis cuentas al confirmar. */
  cuentaPersonalId: number | null;
  creadoEn: string;
}

export interface Tarjeta {
  id: number;
  alias: string;
  marca: string;
  /** Banco emisor según el BIN; null si no se pudo consultar. */
  banco: string | null;
  ultimos4: string;
  verificada: boolean;
  /** Último estado de cuenta confirmado: la deuda que se conoce. */
  ultimoEstado: EstadoCuenta | null;
}

export interface Tarjetas {
  tarjetas: Tarjeta[];
  /** Estados de cuenta leídos (subidos o del buzón) que esperan confirmación. */
  porConfirmar: EstadoCuenta[];
}

/** Solo el BIN (6 primeros dígitos) y los 4 últimos: el número completo no sale del navegador. */
export interface TarjetaRequest {
  alias: string;
  bin: string;
  ultimos4: string;
}

export interface EstadoCuentaRequest {
  saldoTotal: number;
  pagoMinimo: number | null;
  fechaMaximaPago: string;
  fechaCorte: string | null;
  /** true: la deuda del mes es el pago mínimo; false: el saldo total. */
  pagarMinimo: boolean;
}
