import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, filter, takeUntil, throwError } from 'rxjs';
import { environment } from '@env/environment';
import { AuthService } from '../services/auth.service';
/** Agrega el Bearer token a las llamadas a la API y cierra la sesión si el token ya no es válido. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();
  const claveDatos = auth.claveDatos?.() ?? token;
  const destino = new URL(req.url, document.baseURI);
  const api = new URL(environment.apiUrl, document.baseURI);
  const raiz = api.pathname.replace(/\/$/, '');
  const esApi =
    destino.origin === api.origin &&
    (destino.pathname === raiz || destino.pathname.startsWith(raiz + '/'));
  const cambioSesion = auth.cambiosSesion.pipe(filter(() => auth.token() !== token));
  const cambioDatos = (auth.cambiosDatos ?? auth.cambiosSesion).pipe(filter(() => (auth.claveDatos?.() ?? auth.token()) !== claveDatos));
  const areas = ['/comprobantes', '/clientes', '/productos', '/facturas', '/facturacion', '/perfil', '/contabilidad'];
  const area = destino.pathname.slice(raiz.length);
  const esContable = esApi && areas.some(a => area === a || area.startsWith(a + '/'));
  const peticion =
    token && esApi ? req.clone({ setHeaders: {
      Authorization: `Bearer ${token}`,
      ...(esContable && auth.contribuyenteId?.() ? { 'X-Contribuyente': String(auth.contribuyenteId()) } : {}),
    } }) : req;

  return next(peticion).pipe(
    // Una respuesta de la sesión anterior no puede llenar las vistas de la siguiente.
    takeUntil(cambioSesion.pipe(filter(() => esApi && !!token))),
    takeUntil(cambioDatos.pipe(filter(() => esContable))),
    filter(() => !esApi || !token || auth.token() === token),
    filter(() => !esContable || (auth.claveDatos?.() ?? auth.token()) === claveDatos),
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        esApi &&
        auth.token() === token &&
        !req.url.includes('/auth/')
      ) {
        auth.cerrarSesion();
      }
      return throwError(() => error);
    }),
  );
};
