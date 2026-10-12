import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (auth.estaAutenticado()) return true;
  return inject(Router).createUrlTree(['/login'], { queryParams: { volver: state.url } });
};

export const invitadoGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.estaAutenticado() ? inject(Router).createUrlTree([auth.rutaInicio()]) : true;
};
