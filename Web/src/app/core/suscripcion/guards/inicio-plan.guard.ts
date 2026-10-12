import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '@core/auth';
/** El resumen se arma con facturas (plan Profesional); en el plan gratuito el inicio es Mis cuentas. */
export const inicioPorPlanGuard: CanActivateFn = () =>
  inject(AuthService).esProfesional() || inject(Router).createUrlTree(['/cuentas']);
