import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '@core/services';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (auth.estaAutenticado()) return true;
  return inject(Router).createUrlTree(['/login'], { queryParams: { volver: state.url } });
};

export const invitadoGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.estaAutenticado() ? inject(Router).createUrlTree(['/resumen']) : true;
};

/** Secciones del plan Profesional: en el plan Personal lleva a la página de pago explicando qué se desbloquea. */
export const planProfesionalGuard: CanActivateFn = (ruta) => {
  if (inject(AuthService).esProfesional()) return true;
  const seccion = ruta.routeConfig?.path?.split('/')[0] ?? '';
  return inject(Router).createUrlTree(['/suscripcion'], { queryParams: { plan: 'Profesional', motivo: seccion } });
};

/** Panel de superadministrador: solo el rol admin. */
export const adminGuard: CanActivateFn = () =>
  inject(AuthService).usuario()?.rol === 'admin' ? true : inject(Router).createUrlTree(['/resumen']);
