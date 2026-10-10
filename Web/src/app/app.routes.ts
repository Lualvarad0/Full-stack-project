import { Routes } from '@angular/router';
import { authGuard, invitadoGuard } from './core/auth';
import { Shell } from './layout/shell';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Iniciar sesión · Nudo',
    canActivate: [invitadoGuard],
    data: { modo: 'login' },
    loadComponent: () => import('./pages/auth/acceso').then((m) => m.Acceso),
  },
  {
    path: 'registro',
    title: 'Crear cuenta · Nudo',
    canActivate: [invitadoGuard],
    data: { modo: 'registro' },
    loadComponent: () => import('./pages/auth/acceso').then((m) => m.Acceso),
  },
  {
    // Landing pública; si ya hay sesión, va directo al resumen.
    path: '',
    pathMatch: 'full',
    title: 'Nudo · Facturas, gastos y SRI en orden',
    canActivate: [invitadoGuard],
    loadComponent: () => import('./pages/landing/landing').then((m) => m.Landing),
  },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: 'resumen', title: 'Resumen · Nudo', loadComponent: () => import('./pages/resumen/resumen').then((m) => m.Resumen) },
      { path: 'gastos', title: 'Gastos · Nudo', loadComponent: () => import('./pages/gastos/gastos').then((m) => m.Gastos) },
      { path: 'gastos/:id', redirectTo: 'comprobantes/:id' },
      { path: 'ingresos', title: 'Ingresos · Nudo', loadComponent: () => import('./pages/ingresos/ingresos').then((m) => m.Ingresos) },
      { path: 'clientes', title: 'Clientes · Nudo', loadComponent: () => import('./pages/clientes/clientes').then((m) => m.Clientes) },
      { path: 'comprobantes/:id', title: 'Comprobante · Nudo', loadComponent: () => import('./pages/comprobante/comprobante').then((m) => m.ComprobantePagina) },
      { path: 'importar', title: 'Importar del SRI · Nudo', loadComponent: () => import('./pages/importar/importar').then((m) => m.Importar) },
      { path: 'proveedores', title: 'Proveedores · Nudo', loadComponent: () => import('./pages/proveedores/proveedores').then((m) => m.Proveedores) },
      { path: 'configuracion', title: 'Configuración · Nudo', loadComponent: () => import('./pages/configuracion/configuracion').then((m) => m.Configuracion) },
      proximamente('facturacion', 'Emitir facturas electrónicas', 'Emite facturas firmadas y autorizadas por el SRI directamente desde Nudo.', [
        'Firma electrónica con tu certificado (.p12) guardado de forma segura',
        'Envío al SRI, autorización y RIDE en PDF enviado a tu cliente por correo',
        'Notas de crédito y anulaciones',
      ]),
      proximamente('declaraciones', 'Declaración de IVA', 'Prepara tu declaración mensual (formulario 104) con los datos de tus facturas.', [
        'Ventas y compras del mes agrupadas por tarifa de IVA',
        'Crédito tributario y valor a pagar estimados',
        'Recordatorio según el noveno dígito de tu RUC',
      ]),
      proximamente('mi-ruc', 'Mi RUC', 'Tus datos tributarios y las actividades económicas registradas en tu RUC.', [
        'Actividades económicas y establecimientos',
        'Régimen y obligaciones tributarias',
      ]),
    ],
  },
  { path: '**', redirectTo: '' },
];

function proximamente(path: string, titulo: string, descripcion: string, puntos: string[]) {
  return {
    path,
    title: `${titulo} · Nudo`,
    data: { titulo, descripcion, puntos },
    loadComponent: () => import('./pages/proximamente/proximamente').then((m) => m.Proximamente),
  };
}
