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
    descripcion: 'Para llevar tus cuentas del mes frente a lo que ganas.',
    destacado: false,
    cta: 'Empezar gratis',
    incluye: [
      'Registra tus cuentas del mes a mano',
      'Dinos cuánto ganas y te decimos cuánto te queda',
      'Marca el día en que pagas cada cuenta',
      'Cuentas vencidas resaltadas',
      'Historial de los últimos 3 meses',
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
      'Todo lo de Personal, con todo tu historial',
      'Sube tus facturas o tráelas del SRI, sin límite',
      'Buzón propio: reenvía tus facturas por correo',
      'Gastos por categoría, proveedores y tope deducible',
      'Facturas electrónicas (hasta 300 al mes), notas de crédito y retenciones',
      'Ingresos, clientes y tu catálogo de servicios',
      'IVA del mes, vencimientos de tu RUC y preparación de declaraciones',
      'Cobros, pagos y conciliación bancaria',
      'Libros contables, cierre mensual y exportaciones',
      'Filtros por fecha y monto',
    ],
    pronto: ['Presentar la declaración de IVA (formulario 104)'],
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
      'Cartera: hasta 10 RUC desde una sola cuenta',
      'Cada cliente te autoriza con permiso de consulta o edición',
      'Cambia de contribuyente sin cerrar sesión',
      'Cada RUC con sus libros, declaraciones y exportaciones',
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
