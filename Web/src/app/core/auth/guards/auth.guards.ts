import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (auth.estaAutenticado()) return true;
  return inject(Router).createUrlTree(['/login'], { queryParams: { volver: state.url } });
};

/** Solo rutas internas de la app tras iniciar sesión (nada de "//otro-sitio" ni direcciones completas). */
export function rutaInterna(ruta: string | null | undefined): ruta is string {
  return !!ruta && ruta.startsWith('/') && !ruta.startsWith('//') && !ruta.includes('\\') && !ruta.startsWith('/login') && !ruta.startsWith('/registro');
}

/**
 * Rutas con número (/comprobantes/12, /facturacion/7…): solo enteros positivos. Cualquier otra cosa escrita
 * en la barra del navegador vuelve a la pantalla de inicio en lugar de abrir una página rota.
 * Que el número pertenezca a la cuenta lo valida la API (responde "no encontrado" si no es suyo).
 */
export function idValidoGuard(parametro = 'id'): CanActivateFn {
  return (ruta) =>
    /^[1-9]\d{0,9}$/.test(ruta.paramMap.get(parametro) ?? '') ||
    inject(Router).createUrlTree([inject(AuthService).rutaInicio()]);
}

export const invitadoGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.estaAutenticado() ? inject(Router).createUrlTree([auth.rutaInicio()]) : true;
};
