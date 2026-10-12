import { EstadoFactura, TarifaIva } from '../models/facturacion.model';
/** Tabla 6 de la ficha técnica del SRI. */
export const TIPOS_IDENTIFICACION = [
  { valor: '05', etiqueta: 'Cédula' },
  { valor: '04', etiqueta: 'RUC' },
  { valor: '06', etiqueta: 'Pasaporte' },
  { valor: '07', etiqueta: 'Consumidor final' },
] as const;

/** Tabla 24: formas de pago (las más usadas primero). */
export const FORMAS_PAGO = [
  { valor: '20', etiqueta: 'Transferencia u otros con sistema financiero' },
  { valor: '01', etiqueta: 'Efectivo (sin sistema financiero)' },
  { valor: '19', etiqueta: 'Tarjeta de crédito' },
  { valor: '16', etiqueta: 'Tarjeta de débito' },
  { valor: '17', etiqueta: 'Dinero electrónico' },
  { valor: '18', etiqueta: 'Tarjeta prepago' },
  { valor: '15', etiqueta: 'Compensación de deudas' },
  { valor: '21', etiqueta: 'Endoso de títulos' },
] as const;

export const TARIFAS_IVA: readonly { valor: TarifaIva; etiqueta: string; porcentaje: number }[] = [
  { valor: 'Iva15', etiqueta: 'IVA 15 %', porcentaje: 15 },
  { valor: 'Iva5', etiqueta: 'IVA 5 %', porcentaje: 5 },
  { valor: 'Iva0', etiqueta: 'IVA 0 %', porcentaje: 0 },
  { valor: 'NoObjeto', etiqueta: 'No objeto de IVA', porcentaje: 0 },
  { valor: 'Exento', etiqueta: 'Exento de IVA', porcentaje: 0 },
];

export const ESTADOS_FACTURA: Record<
  EstadoFactura,
  { texto: string; tono: 'ok' | 'espera' | 'error' }
> = {
  Autorizada: { texto: 'Autorizada', tono: 'ok' },
  Recibida: { texto: 'Recibida por el SRI', tono: 'espera' },
  EnProceso: { texto: 'En proceso', tono: 'espera' },
  Firmada: { texto: 'Sin enviar', tono: 'espera' },
  Devuelta: { texto: 'Devuelta', tono: 'error' },
  NoAutorizada: { texto: 'No autorizada', tono: 'error' },
  Anulada: { texto: 'Anulada', tono: 'error' },
};
