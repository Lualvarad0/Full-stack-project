import { Plan, PlanSuscripcion } from '../models/suscripcion.model';
export const DIAS_PRUEBA = 14;

/**
 * Planes de Nudo: los usa la landing, el registro y Configuración. Los montos que se cobran
 * vienen de la API (GET /api/suscripcion/planes); estos textos deben coincidir con ella.
 */
export const PLANES: readonly Plan[] = [
  {
    id: 'Personal',
    nombre: 'Personal',
    precio: 'Gratis',
    periodo: '',
    descripcion: 'Para ordenar tus gastos y tu deducible.',
    destacado: false,
    cta: 'Empezar gratis',
    incluye: [
      'Hasta 50 comprobantes al mes',
      'Historial de los últimos 3 meses',
      'Buzón de facturas propio',
      'Verificación con el SRI',
      'Gastos por categoría y tope deducible',
      'Avisos de seguridad',
    ],
    pronto: [],
  },
  {
    id: 'Profesional',
    nombre: 'Profesional',
    precio: '$14,99',
    periodo: '/mes + IVA',
    descripcion: 'Para quien factura con su RUC.',
    destacado: true,
    cta: `Probar ${DIAS_PRUEBA} días gratis`,
    incluye: [
      'Todo lo de Personal, sin límites',
      'Comprobantes ilimitados y todo tu historial',
      'Facturas electrónicas (hasta 300 al mes)',
      'Tu catálogo de servicios para facturar',
      'Ingresos y clientes',
      'IVA del mes: cobrado vs pagado',
      'Filtros por fecha y monto',
    ],
    pronto: ['Declaración de IVA (formulario 104)'],
  },
  {
    id: 'Contadores',
    nombre: 'Contadores',
    precio: '$49',
    periodo: '/mes + IVA',
    descripcion: 'Para estudios contables: hasta 10 RUC.',
    destacado: false,
    cta: `Probar ${DIAS_PRUEBA} días gratis`,
    incluye: [
      'Todo lo de Profesional',
      'Varios RUC en una cuenta',
      'Acompañamiento en la puesta en marcha',
    ],
    pronto: ['Panel por cliente'],
  },
];

/** Mensaje común de los planes de pago (la tarjeta se cobra al terminar la prueba). */
export const AVISO_LANZAMIENTO = `Los planes de pago incluyen ${DIAS_PRUEBA} días gratis: no se cobra nada hasta que terminen y puedes cancelar cuando quieras. Pagando el año te regalamos 2 meses.`;

export function esPlan(valor: string | null | undefined): valor is PlanSuscripcion {
  return PLANES.some((p) => p.id === valor);
}

export function esDePago(plan: PlanSuscripcion): boolean {
  return plan !== 'Personal';
}
