import { computed, inject, Signal } from '@angular/core';
import { AuthService } from '@core/auth';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, Observable, of, scan, startWith, switchMap } from 'rxjs';
import { Carga } from '../models/carga.model';
import { mensajeDeError } from './errores';
/**
 * Carga datos cada vez que cambia `clave` (p. ej. el mes seleccionado o un contador de recarga).
 * Conserva los datos anteriores mientras recarga para evitar parpadeos.
 * Debe llamarse en un contexto de inyección (inicializador de campo o constructor).
 */
export function cargar<K, T>(
  clave: () => K,
  fuente: (clave: K) => Observable<T>,
): Signal<Carga<T>> {
  const inicial: Carga<T> = { cargando: true, datos: null, error: null };
  const auth = inject(AuthService, { optional: true });
  const usuario = () => auth?.claveDatos?.() ?? auth?.usuario?.()?.id ?? null;
  const estado = toSignal(
    toObservable(computed(() => ({ clave: clave(), usuarioId: usuario() }))).pipe(
      switchMap(({ clave, usuarioId }) =>
        fuente(clave).pipe(
          map((datos) => ({
            usuarioId,
            carga: { cargando: false, datos, error: null } as Carga<T> | null,
          })),
          catchError((e) =>
            of({
              usuarioId,
              carga: { cargando: false, datos: null, error: mensajeDeError(e) } as Carga<T> | null,
            }),
          ),
          startWith({ usuarioId, carga: null as Carga<T> | null }),
        ),
      ),
      scan(
        (previo, actual) => {
          const anterior = previo.usuarioId === actual.usuarioId ? previo.carga : inicial;
          return {
            usuarioId: actual.usuarioId,
            carga:
              actual.carga === null
                ? { ...anterior, cargando: true, error: null }
                : {
                    ...actual.carga,
                    datos: actual.carga.error ? anterior.datos : actual.carga.datos,
                  },
          };
        },
        { usuarioId: usuario(), carga: inicial },
      ),
    ),
    { initialValue: { usuarioId: usuario(), carga: inicial } },
  );
  return computed(() => (estado().usuarioId === usuario() ? estado().carga : inicial));
}
