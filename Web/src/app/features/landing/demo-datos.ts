import { CategoriaDemo, MovimientoDemo, VistaDemo } from '@features/landing/models/landing.model';
export const sinMovimiento = () =>
  typeof matchMedia !== 'undefined' && matchMedia('(prefers-reduced-motion: reduce)').matches;

export const CATEGORIAS: CategoriaDemo[] = [
  'Vivienda',
  'Salud',
  'Alimentación',
  'Educación',
  'Vestimenta',
];

export const PASOS = ['Correo recibido', 'XML leído', 'Verificado con el SRI', 'Registrado'];

/** Montos del mes anteriores a los movimientos visibles (datos ilustrativos). */
export const BASE = {
  ingresos: 11503,
  gastos: 1890.35,
  deducibles: {
    Vivienda: 1350,
    Salud: 600,
    Alimentación: 520,
    Educación: 300,
    Vestimenta: 90,
  } as Record<CategoriaDemo, number>,
  clientes: {
    'Constructora del Pacífico': 3200,
    'Estudio Andrade & Asoc.': 2100,
    'Comercial Ríos': 1450,
  } as Record<string, number>,
};

export const TOPE = 5752.6;

export const INICIALES: MovimientoDemo[] = [
  {
    id: 1,
    fecha: '09 oct',
    nombre: 'Constructora del Pacífico',
    monto: 977.5,
    ingreso: true,
    categoria: null,
  },
  {
    id: 2,
    fecha: '08 oct',
    nombre: 'De Prati',
    monto: 91.89,
    ingreso: false,
    categoria: 'Vestimenta',
  },
  {
    id: 3,
    fecha: '05 oct',
    nombre: 'Inmobiliaria Los Ceibos',
    monto: 450,
    ingreso: false,
    categoria: 'Vivienda',
  },
  { id: 4, fecha: '03 oct', nombre: 'Fybeca', monto: 43.92, ingreso: false, categoria: 'Salud' },
];

export const POR_LLEGAR: Omit<MovimientoDemo, 'id'>[] = [
  {
    fecha: '10 oct',
    nombre: 'Supermaxi',
    monto: 101.68,
    ingreso: false,
    categoria: 'Alimentación',
  },
  {
    fecha: '10 oct',
    nombre: 'Estudio Andrade & Asoc.',
    monto: 640,
    ingreso: true,
    categoria: null,
  },
  {
    fecha: '10 oct',
    nombre: 'Farmacias Cruz Azul',
    monto: 23.4,
    ingreso: false,
    categoria: 'Salud',
  },
  {
    fecha: '10 oct',
    nombre: 'Unidad Educativa Delta',
    monto: 180,
    ingreso: false,
    categoria: 'Educación',
  },
  { fecha: '10 oct', nombre: 'Comercial Ríos', monto: 1215.3, ingreso: true, categoria: null },
  { fecha: '10 oct', nombre: 'Etafashion', monto: 64.9, ingreso: false, categoria: 'Vestimenta' },
];

export const RECORRIDO: VistaDemo[] = ['buzon', 'resumen', 'gastos', 'ingresos', 'resumen'];
