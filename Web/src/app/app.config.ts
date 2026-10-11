import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  ViewTransitionInfo,
  withComponentInputBinding,
  withInMemoryScrolling,
  withViewTransitions,
} from '@angular/router';
import { authInterceptor } from '@core/interceptors';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
    provideRouter(routes, withComponentInputBinding(), withInMemoryScrolling({ scrollPositionRestoration: 'top', anchorScrolling: 'enabled' }), withViewTransitions({ skipInitialTransition: true, onViewTransitionCreated: saltarSiMismaPagina })),
  ],
};

/** Sin fundido entre /login y /registro: ahí la animación la hace el panel deslizante de la misma página. */
function saltarSiMismaPagina({ transition, from, to }: ViewTransitionInfo): void {
  const hoja = (r: ActivatedRouteSnapshot): ActivatedRouteSnapshot => (r.firstChild ? hoja(r.firstChild) : r);
  if (hoja(from).routeConfig === hoja(to).routeConfig) transition.skipTransition();
}
