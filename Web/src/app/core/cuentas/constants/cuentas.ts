import { CategoriaCuenta } from '../models/cuentas.model';

export const CATEGORIAS_CUENTA: readonly { valor: CategoriaCuenta; etiqueta: string }[] = [
  { valor: 'Vivienda', etiqueta: 'Vivienda y arriendo' },
  { valor: 'ServiciosBasicos', etiqueta: 'Luz, agua, internet y teléfono' },
  { valor: 'Alimentacion', etiqueta: 'Alimentación' },
  { valor: 'Transporte', etiqueta: 'Transporte' },
  { valor: 'Salud', etiqueta: 'Salud' },
  { valor: 'Educacion', etiqueta: 'Educación' },
  { valor: 'Deudas', etiqueta: 'Tarjetas, préstamos y deudas' },
  { valor: 'Entretenimiento', etiqueta: 'Entretenimiento y suscripciones' },
  { valor: 'Otros', etiqueta: 'Otros' },
];

/** A partir de este porcentaje del ingreso comprometido, el mes se considera ajustado. */
export const UMBRAL_AJUSTADO = 0.8;
