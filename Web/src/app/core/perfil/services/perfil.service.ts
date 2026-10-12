import { HttpClient } from '@angular/common/http';
import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { Observable, Subscription, tap } from 'rxjs';
import { environment } from '@env/environment';
import { Perfil, PerfilActualizado, PerfilRequest } from '../models/perfil.model';
import { PlanSuscripcion } from '@core/suscripcion';
import { AuthService, Usuario } from '@core/auth';
/** Perfil tributario del usuario autenticado; lo comparten todas las páginas. */
@Injectable({ providedIn: 'root' })
export class PerfilService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private solicitud?: Subscription;
  private readonly url = `${environment.apiUrl}/perfil`;

  private readonly perfilActual = signal<{ clave: string; datos: Perfil } | null>(null);
  private clave(): string { return this.auth.claveDatos?.() ?? String(this.auth.usuario()?.id ?? ''); }
  readonly perfil = computed(() =>
    this.perfilActual()?.clave === this.clave()
      ? (this.perfilActual()?.datos ?? null)
      : null,
  );
  readonly completo = computed(() => this.perfil()?.completo ?? true);
  readonly tieneRuc = computed(() => !!this.perfil()?.ruc);

  constructor() {
    effect((limpiar) => {
      const usuarioId = this.auth.usuario()?.id;
      this.clave();
      this.perfilActual.set(null);
      if (usuarioId) this.cargar();
      limpiar(() => this.solicitud?.unsubscribe());
    });
  }

  cargar(): void {
    this.solicitud?.unsubscribe();
    const usuarioId = this.auth.usuario()?.id;
    const clave = this.clave();
    if (!usuarioId) {
      this.perfilActual.set(null);
      return;
    }
    this.solicitud = this.http.get<Perfil>(this.url).subscribe({
      next: (p) => {
        if (this.clave() === clave) this.perfilActual.set({ clave, datos: p });
      },
      // Sin perfil la app sigue funcionando (sin avisos de perfil); cada página muestra sus propios errores de red.
      error: () => {
        if (this.clave() === clave) this.perfilActual.set(null);
      },
    });
  }

  guardar(request: PerfilRequest): Observable<PerfilActualizado> {
    const usuarioId = this.auth.usuario()?.id;
    const clave = this.clave();
    return this.http.put<PerfilActualizado>(this.url, request).pipe(
      tap((r) => {
        if (usuarioId && this.clave() === clave)
          this.perfilActual.set({ clave, datos: r.perfil });
      }),
    );
  }

  cambiarPlan(plan: PlanSuscripcion): Observable<Usuario> {
    return this.http.put<Usuario>(`${this.url}/plan`, { plan });
  }
}
