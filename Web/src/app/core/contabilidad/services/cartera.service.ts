import { HttpClient } from '@angular/common/http';
import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { AuthService } from '@core/auth';
import { environment } from '@env/environment';
import { CupoPlan } from '@core/suscripcion';

export interface Contribuyente { id: number; nombre: string; ruc: string | null; permiso: 'Consulta' | 'Edicion'; propio: boolean; }
export interface VinculoContador { id: number; propietarioId: number; contadorId: number; nombrePropietario: string; ruc: string | null; nombreContador: string; correoContador: string; permiso: 'Consulta' | 'Edicion'; estado: 'Pendiente' | 'Activo' | 'Rechazado' | 'Revocado'; expiraEn: string; puedeResponder: boolean; }
@Injectable({ providedIn: 'root' })
export class CarteraService {
  private readonly http = inject(HttpClient); private readonly auth = inject(AuthService); private readonly url = `${environment.apiUrl}/cartera`;
  private readonly estado = signal<{ usuario: number; lista: Contribuyente[] } | null>(null);
  readonly lista = computed(() => this.estado()?.usuario === this.auth.usuario()?.id ? this.estado()!.lista : []);
  readonly activo = computed(() => this.lista().find(c => c.id === this.auth.contribuyenteId()) ?? null);
  readonly propio = computed(() => this.auth.contribuyenteId() === this.auth.usuario()?.id);
  readonly puedeEditar = computed(() => this.propio() || this.activo()?.permiso === 'Edicion');
  constructor() {
    effect(cleanup => {
      const id = this.auth.usuario()?.id;
      // Se conserva el contribuyente elegido en esta pestaña (recargar no cambia de perfil) mientras siga en la cartera.
      this.estado.set(null);
      if (!id) return;
      const solicitud = this.http.get<Contribuyente[]>(this.url).subscribe({
        next: lista => {
          if (this.auth.usuario()?.id !== id) return;
          this.estado.set({ usuario: id, lista });
          if (!lista.some(c => c.id === this.auth.contribuyenteId())) this.auth.seleccionarContribuyente(null);
        },
        error: () => this.auth.seleccionarContribuyente(null),
      });
      cleanup(() => solicitud.unsubscribe());
    });
  }
  recargar(): void {
    const usuario = this.auth.usuario()?.id; if (!usuario) return;
    this.http.get<Contribuyente[]>(this.url).subscribe({ next: lista => { if (this.auth.usuario()?.id !== usuario) return; this.estado.set({ usuario, lista }); if (!lista.some(c => c.id === this.auth.contribuyenteId())) this.auth.seleccionarContribuyente(null); } });
  }
  seleccionar(id: number): void { if (this.lista().some(c => c.id === id)) this.auth.seleccionarContribuyente(id); }
  /** Contribuyentes usados de la cartera frente al límite del plan Contadores. */
  cupo() { return this.http.get<CupoPlan>(`${this.url}/cupo`); }
  solicitudes() { return this.http.get<VinculoContador[]>(`${this.url}/solicitudes`); }
  solicitar(correo: string, permiso: 'Consulta' | 'Edicion') { return this.http.post<{ mensaje: string }>(`${this.url}/solicitudes`, { correo, permiso }); }
  aceptar(id: number) { return this.http.post<void>(`${this.url}/solicitudes/${id}/aceptar`, {}); }
  rechazar(id: number) { return this.http.post<void>(`${this.url}/solicitudes/${id}/rechazar`, {}); }
  revocar(id: number) { return this.http.delete<void>(`${this.url}/accesos/${id}`); }
}
