import { Routes } from '@angular/router';
import { adminGuard } from '@core/admin';
import { authGuard, idValidoGuard, invitadoGuard } from '@core/auth';
import { inicioPorPlanGuard, planProfesionalGuard } from '@core/suscripcion';
export const routes: Routes = [
  { path: 'recuperar-clave', title: 'Recuperar contraseña · Nudo', loadComponent: () => import('./features/auth/recuperar-clave').then(m => m.RecuperarClave) },
  { path: 'restablecer-clave', title: 'Nueva contraseña · Nudo', loadComponent: () => import('./features/auth/restablecer-clave').then(m => m.RestablecerClave) },
  { path: 'verificar-correo', title: 'Verificar correo · Nudo', loadComponent: () => import('./features/auth/verificar-correo').then(m => m.VerificarCorreo) },
  {
    // /login y /registro son la misma ruta para que el panel de acceso se deslice sin recrear la página.
    matcher: (url) =>
      url.length === 1 && (url[0].path === 'login' || url[0].path === 'registro')
        ? { consumed: url, posParams: { modo: url[0] } }
        : null,
    title: (ruta) =>
      ruta.params['modo'] === 'registro' ? 'Crear cuenta · Nudo' : 'Iniciar sesión · Nudo',
    canActivate: [invitadoGuard],
    loadComponent: () => import('./features/auth/acceso').then((m) => m.Acceso),
  },
  {
    // Landing pública; si ya hay sesión, va directo al resumen.
    path: '',
    pathMatch: 'full',
    title: 'Nudo · Facturas, gastos y SRI en orden',
    canActivate: [invitadoGuard],
    loadComponent: () => import('./features/landing/landing').then((m) => m.Landing),
  },
  {
    path: '',
    loadComponent: () => import('./layout/shell').then((m) => m.Shell),
    canActivate: [authGuard],
    children: [
      { path: 'declaraciones', title: 'Preparación tributaria · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./features/tributacion/declaraciones').then(m => m.DeclaracionesPagina) },
      { path: 'mi-ruc', title: 'Mi RUC y obligaciones · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./features/tributacion/mi-ruc').then(m => m.MiRucPagina) },
      { path: 'gastos-personales', title: 'Gastos personales · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./features/tributacion/gastos-personales').then(m => m.GastosPersonalesPagina) },
      { path: 'exportaciones', title: 'Exportar y entregar · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./features/tributacion/exportaciones').then(m => m.ExportacionesPagina) },
      { path: 'libros', title: 'Libros contables · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./features/contabilidad/libros').then(m => m.LibrosPagina) },
      { path: 'cartera', title: 'Cartera y accesos · Nudo', loadComponent: () => import('./features/cartera/cartera').then(m => m.CarteraPagina) },
      { path: 'cierre', title: 'Cierre mensual · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./features/contabilidad/cierre-periodo').then(m => m.CierrePeriodoPagina) },
      { path: 'retenciones', title: 'Retenciones · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./features/contabilidad/retenciones').then(m => m.RetencionesPagina) },
      { path: 'cobros-pagos', title: 'Cobros y pagos · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./features/tesoreria/cobros-pagos').then(m => m.CobrosPagosPagina) },
      { path: 'bancos', title: 'Bancos · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./features/tesoreria/bancos').then(m => m.BancosPagina) },
      { path: 'facturacion/nota-credito/:facturaId', title: 'Nota de crédito · Nudo', canActivate: [planProfesionalGuard, idValidoGuard('facturaId')], loadComponent: () => import('./features/facturacion/nueva-nota-credito').then(m => m.NuevaNotaCredito) },
      { path: 'facturacion/retencion/nueva', title: 'Emitir retención · Nudo', canActivate: [planProfesionalGuard], loadComponent: () => import('./features/facturacion/nueva-retencion').then(m => m.NuevaRetencion) },
      {
        path: 'resumen',
        title: 'Resumen · Nudo',
        canActivate: [inicioPorPlanGuard],
        loadComponent: () => import('./features/resumen/resumen').then((m) => m.Resumen),
      },
      {
        path: 'cuentas',
        title: 'Mis cuentas · Nudo',
        loadComponent: () => import('./features/cuentas/cuentas').then((m) => m.Cuentas),
      },
      {
        path: 'tarjetas',
        title: 'Mis tarjetas · Nudo',
        canActivate: [planProfesionalGuard],
        loadComponent: () => import('./features/tarjetas/tarjetas').then((m) => m.TarjetasPagina),
      },
      {
        path: 'gastos',
        title: 'Gastos · Nudo',
        canActivate: [planProfesionalGuard],
        loadComponent: () => import('./features/gastos/gastos').then((m) => m.Gastos),
      },
      { path: 'gastos/:id', redirectTo: 'comprobantes/:id' },
      {
        path: 'ingresos',
        title: 'Ingresos · Nudo',
        canActivate: [planProfesionalGuard],
        loadComponent: () => import('./features/ingresos/ingresos').then((m) => m.Ingresos),
      },
      {
        path: 'clientes',
        title: 'Clientes · Nudo',
        canActivate: [planProfesionalGuard],
        loadComponent: () => import('./features/clientes/clientes').then((m) => m.Clientes),
      },
      {
        path: 'comprobantes/:id',
        title: 'Comprobante · Nudo',
        canActivate: [planProfesionalGuard, idValidoGuard()],
        loadComponent: () =>
          import('./features/comprobante/comprobante').then((m) => m.ComprobantePagina),
      },
      {
        path: 'importar',
        title: 'Traer facturas · Nudo',
        canActivate: [planProfesionalGuard],
        loadComponent: () => import('./features/importar/importar').then((m) => m.Importar),
      },
      {
        path: 'proveedores',
        title: 'Proveedores · Nudo',
        canActivate: [planProfesionalGuard],
        loadComponent: () =>
          import('./features/proveedores/proveedores').then((m) => m.Proveedores),
      },
      {
        path: 'admin',
        title: 'Administración · Nudo',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/admin').then((m) => m.Admin),
      },
      {
        path: 'facturacion/servicios',
        title: 'Mis servicios · Nudo',
        canActivate: [planProfesionalGuard],
        loadComponent: () => import('./features/facturacion/servicios').then((m) => m.Servicios),
      },
      {
        path: 'suscripcion',
        title: 'Tu plan · Nudo',
        loadComponent: () =>
          import('./features/suscripcion/suscripcion').then((m) => m.SuscripcionPagina),
      },
      {
        path: 'configuracion',
        title: 'Configuración · Nudo',
        loadComponent: () =>
          import('./features/configuracion/configuracion').then((m) => m.Configuracion),
      },
      {
        path: 'facturacion',
        title: 'Facturas · Nudo',
        canActivate: [planProfesionalGuard],
        loadComponent: () => import('./features/facturacion/facturas').then((m) => m.Facturas),
      },
      {
        path: 'facturacion/nueva',
        title: 'Nueva factura · Nudo',
        canActivate: [planProfesionalGuard],
        loadComponent: () =>
          import('./features/facturacion/nueva-factura').then((m) => m.NuevaFactura),
      },
      {
        path: 'facturacion/:id',
        title: 'Factura · Nudo',
        canActivate: [planProfesionalGuard, idValidoGuard()],
        loadComponent: () =>
          import('./features/facturacion/factura-detalle').then((m) => m.FacturaDetalle),
      },
    ],
  },
  {
    // Cualquier dirección que no existe: página 404 (la URL se conserva para que se vea qué se escribió).
    path: '**',
    title: 'Página no encontrada · Nudo',
    loadComponent: () => import('./features/no-encontrado/no-encontrado').then((m) => m.NoEncontrado),
  },
];
