import { Routes } from '@angular/router';
import { adminGuard, authGuard, invitadoGuard, planProfesionalGuard } from '@core/guards';

export const routes: Routes = [
  {
    // /login y /registro son la misma ruta para que el panel de acceso se deslice sin recrear la página.
    matcher: (url) =>
      url.length === 1 && (url[0].path === 'login' || url[0].path === 'registro') ? { consumed: url, posParams: { modo: url[0] } } : null,
    title: (ruta) => (ruta.params['modo'] === 'registro' ? 'Crear cuenta · Nudo' : 'Iniciar sesión · Nudo'),
    canActivate: [invitadoGuard],
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
    loadComponent: () => import('./layout/shell').then((m) => m.Shell),
    canActivate: [authGuard],
    children: [
      { path: 'resumen', title: 'Resumen · Nudo', loadComponent: () => import('./pages/resumen/resumen').then((m) => m.Resumen) },
      { path: 'gastos', title: 'Gastos · Nudo', loadComponent: () => import('./pages/gastos/gastos').then((m) => m.Gastos) },
      { path: 'gastos/:id', redirectTo: 'comprobantes/:id' },
      { path: 'ingresos', title: 'Ingresos · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./pages/ingresos/ingresos').then((m) => m.Ingresos) },
      { path: 'clientes', title: 'Clientes · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./pages/clientes/clientes').then((m) => m.Clientes) },
      { path: 'comprobantes/:id', title: 'Comprobante · Nudo', loadComponent: () => import('./pages/comprobante/comprobante').then((m) => m.ComprobantePagina) },
      { path: 'importar', title: 'Traer facturas · Nudo', loadComponent: () => import('./pages/importar/importar').then((m) => m.Importar) },
      { path: 'proveedores', title: 'Proveedores · Nudo', loadComponent: () => import('./pages/proveedores/proveedores').then((m) => m.Proveedores) },
      { path: 'admin', title: 'Administración · Nudo', canActivate: [adminGuard], loadComponent: () => import('./pages/admin/admin').then((m) => m.Admin) },
      { path: 'facturacion/servicios', title: 'Mis servicios · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./pages/facturacion/servicios').then((m) => m.Servicios) },
      { path: 'suscripcion', title: 'Tu plan · Nudo', loadComponent: () => import('./pages/suscripcion/suscripcion').then((m) => m.SuscripcionPagina) },
      { path: 'configuracion', title: 'Configuración · Nudo', loadComponent: () => import('./pages/configuracion/configuracion').then((m) => m.Configuracion) },
      { path: 'facturacion', title: 'Facturas · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./pages/facturacion/facturas').then((m) => m.Facturas) },
      { path: 'facturacion/nueva', title: 'Nueva factura · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./pages/facturacion/nueva-factura').then((m) => m.NuevaFactura) },
      { path: 'facturacion/:id', title: 'Factura · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./pages/facturacion/factura-detalle').then((m) => m.FacturaDetalle) },
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
