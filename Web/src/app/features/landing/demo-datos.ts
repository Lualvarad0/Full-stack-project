import { CuentaDemo, MovimientoDemo, VistaDemo } from '@features/landing/models/landing.model';
export const sinMovimiento = () =>
  typeof matchMedia !== 'undefined' && matchMedia('(prefers-reduced-motion: reduce)').matches;

/** Mes de la demo y el "hoy" con el que se marcan pagos y vencimientos (datos ilustrativos). */
export const MES = { nombre: 'octubre', anio: 2026, hoy: 10 };

/** Mismos pasos que sigue cada correo del buzón en la app. */
export const PASOS = ['Correo recibido', 'XML leído', 'Verificado con el SRI', 'Registrado'];

/** Ingresos y gastos de los meses anteriores (mayo a septiembre) para el gráfico de evolución. */
export const EVOLUCION = [
  { mes: 'May', ingresos: 7200, gastos: 2310 },
  { mes: 'Jun', ingresos: 6400, gastos: 2050 },
  { mes: 'Jul', ingresos: 8100, gastos: 2600 },
  { mes: 'Ago', ingresos: 7600, gastos: 2240 },
  { mes: 'Sep', ingresos: 8900, gastos: 2480 },
];

/** Tope de gastos personales 2026 sin cargas (7 canastas de $821,80) y lo deducido hasta septiembre. */
export const TOPE = { anio: 2026, tope: 5752.6, deducidoAntes: 3410.25 };

export const INICIALES: MovimientoDemo[] = [
  { id: 1, fecha: '09 oct', nombre: 'Constructora del Pacífico', identificacion: '0992345678001', numero: '001-001-000000231', monto: 977.5, ingreso: true, categoria: null },
  { id: 2, fecha: '08 oct', nombre: 'De Prati', identificacion: 'RUC 0990017514001', numero: '012-104-000548877', monto: 91.89, ingreso: false, categoria: 'Vestimenta' },
  { id: 3, fecha: '05 oct', nombre: 'Inmobiliaria Los Ceibos', identificacion: 'RUC 0992877410001', numero: '001-001-000001290', monto: 450, ingreso: false, categoria: 'Vivienda' },
  { id: 4, fecha: '03 oct', nombre: 'Fybeca', identificacion: 'RUC 0990858322001', numero: '091-012-000873421', monto: 43.92, ingreso: false, categoria: 'Salud' },
  { id: 5, fecha: '02 oct', nombre: 'Estudio Andrade & Asoc.', identificacion: '1792233445001', numero: '001-001-000000230', monto: 640, ingreso: true, categoria: null },
  { id: 6, fecha: '02 oct', nombre: 'Megamaxi', identificacion: 'RUC 1790016919001', numero: '043-011-001190342', monto: 148.75, ingreso: false, categoria: 'Alimentacion' },
  { id: 7, fecha: '01 oct', nombre: 'Hidalgo & Hidalgo S.A.', identificacion: '0990778899001', numero: '001-001-000000229', monto: 2350, ingreso: true, categoria: null },
  { id: 8, fecha: '01 oct', nombre: 'Clínica Kennedy', identificacion: 'RUC 0990186045001', numero: '002-010-000412218', monto: 85, ingreso: false, categoria: 'Salud' },
  { id: 9, fecha: '01 oct', nombre: 'Librería Mr. Books', identificacion: 'RUC 0992461098001', numero: '001-004-000067120', monto: 36.5, ingreso: false, categoria: 'Educacion' },
];

/** Facturas que van llegando al buzón durante la demo. */
export const POR_LLEGAR: Omit<MovimientoDemo, 'id'>[] = [
  { fecha: '10 oct', nombre: 'Supermaxi', identificacion: 'RUC 1790016919001', numero: '043-020-001284456', monto: 101.68, ingreso: false, categoria: 'Alimentacion' },
  { fecha: '10 oct', nombre: 'Comercial Ríos', identificacion: '0991122334001', numero: '001-001-000000232', monto: 1215.3, ingreso: true, categoria: null },
  { fecha: '10 oct', nombre: 'Farmacias Cruz Azul', identificacion: 'RUC 1791984722001', numero: '128-001-000341209', monto: 23.4, ingreso: false, categoria: 'Salud' },
  { fecha: '10 oct', nombre: 'Unidad Educativa Delta', identificacion: 'RUC 0991520486001', numero: '001-002-000009812', monto: 180, ingreso: false, categoria: 'Educacion' },
  { fecha: '10 oct', nombre: 'Etafashion', identificacion: 'RUC 0990011419001', numero: '025-003-000221097', monto: 64.9, ingreso: false, categoria: 'Vestimenta' },
];

/** Mis cuentas: lo que gana y sus cuentas del mes, con las categorías de la app. */
export const INGRESO_MENSUAL = 2400;
export const CUENTAS: CuentaDemo[] = [
  { id: 1, nombre: 'Arriendo', categoria: 'Vivienda', monto: 450, diaVencimiento: 5, pagadaEl: 5 },
  { id: 2, nombre: 'Cuota del préstamo', categoria: 'Deudas', monto: 185.4, diaVencimiento: 8, pagadaEl: null },
  { id: 3, nombre: 'Luz eléctrica', categoria: 'ServiciosBasicos', monto: 32.1, diaVencimiento: 15, pagadaEl: null },
  { id: 4, nombre: 'Internet y celular', categoria: 'ServiciosBasicos', monto: 48.5, diaVencimiento: 20, pagadaEl: null },
  { id: 5, nombre: 'Supermercado', categoria: 'Alimentacion', monto: 320, diaVencimiento: 28, pagadaEl: null },
  { id: 6, nombre: 'Streaming', categoria: 'Entretenimiento', monto: 10.99, diaVencimiento: 2, pagadaEl: 2 },
];

export const RECORRIDO: VistaDemo[] = ['importar', 'resumen', 'cuentas', 'gastos', 'ingresos', 'resumen'];
