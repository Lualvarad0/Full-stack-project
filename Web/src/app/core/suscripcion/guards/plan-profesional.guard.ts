import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '@core/auth';
/** Secciones del plan Profesional: en el plan Personal lleva a la página de pago explicando qué se desbloquea. */
export const planProfesionalGuard: CanActivateFn = (ruta) => {
  if (inject(AuthService).esProfesional()) return true;
  const seccion = ruta.routeConfig?.path?.split('/')[0] ?? '';
  return inject(Router).createUrlTree(['/suscripcion'], {
    queryParams: { plan: 'Profesional', motivo: seccion },
  });
};
