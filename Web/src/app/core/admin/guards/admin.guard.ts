import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '@core/auth';
/** Panel de superadministrador: solo el rol admin. */
export const adminGuard: CanActivateFn = () =>
  inject(AuthService).usuario()?.rol === 'admin'
    ? true
    : inject(Router).createUrlTree(['/resumen']);
