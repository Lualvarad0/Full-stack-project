import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '@env/environment';
import { Perfil, PerfilActualizado, PerfilRequest, PlanSuscripcion, Usuario } from '@core/models';

/** Perfil tributario del usuario autenticado; lo comparten todas las páginas. */
@Injectable({ providedIn: 'root' })
export class PerfilService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/perfil`;

  readonly perfil = signal<Perfil | null>(null);
  readonly completo = computed(() => this.perfil()?.completo ?? true);
  readonly tieneRuc = computed(() => !!this.perfil()?.ruc);

  cargar(): void {
    this.http.get<Perfil>(this.url).subscribe({
      next: (p) => this.perfil.set(p),
      // Sin perfil la app sigue funcionando (sin avisos de perfil); cada página muestra sus propios errores de red.
      error: () => this.perfil.set(null),
    });
  }

  guardar(request: PerfilRequest): Observable<PerfilActualizado> {
    return this.http.put<PerfilActualizado>(this.url, request).pipe(tap((r) => this.perfil.set(r.perfil)));
  }

  cambiarPlan(plan: PlanSuscripcion): Observable<Usuario> {
    return this.http.put<Usuario>(`${this.url}/plan`, { plan });
  }
}
