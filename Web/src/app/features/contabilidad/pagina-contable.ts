import { Directive, effect, inject, signal } from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { AuthService } from '@core/auth';
import { CarteraService } from '@core/contabilidad/services/cartera.service';
import { AvisosService } from '@core/ui';

/** Acciones compartidas: permisos, espera y cancelación al cambiar de titular. */
@Directive()
export abstract class PaginaContable {
  protected readonly cartera = inject(CarteraService);
  protected readonly auth = inject(AuthService);
  protected readonly avisos = inject(AvisosService);
  protected readonly ocupado = signal(false);
  protected readonly recarga = signal(0);
  private accion?: Subscription;
  constructor() {
    effect((onCleanup) => {
      this.auth.claveDatos();
      this.ocupado.set(false);
      this.limpiar();
      onCleanup(() => this.accion?.unsubscribe());
    });
  }
  protected limpiar(): void {}
  protected refrescar(): void {
    this.recarga.update((n) => n + 1);
  }
  protected ejecutar(obs: Observable<unknown>, mensaje = 'Información actualizada.'): void {
    if (this.ocupado() || !this.cartera.puedeEditar()) return;
    this.ocupado.set(true);
    const clave = this.auth.claveDatos();
    this.accion = obs.subscribe({
      next: () => {
        if (clave !== this.auth.claveDatos()) return;
        this.ocupado.set(false);
        this.refrescar();
        this.avisos.exito(mensaje);
      },
      error: (e) => {
        if (clave !== this.auth.claveDatos()) return;
        this.ocupado.set(false);
        this.avisos.error(e);
      },
    });
  }
}
