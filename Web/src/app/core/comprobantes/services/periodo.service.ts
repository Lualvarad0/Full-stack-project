import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { AuthService } from '@core/auth';
import { capitalizar, nombreMes } from '@core/utils';
/** Mes de trabajo seleccionado en la barra superior; lo comparten todas las páginas. */
@Injectable({ providedIn: 'root' })
export class PeriodoService {
  private readonly hoy = new Date();
  readonly anio = signal(this.hoy.getFullYear());
  readonly mes = signal(this.hoy.getMonth() + 1);

  constructor() {
    const auth = inject(AuthService);
    effect(() => {
      auth.usuario()?.id;
      auth.claveDatos?.();
      const hoy = new Date();
      this.anio.set(hoy.getFullYear());
      this.mes.set(hoy.getMonth() + 1);
    });
  }

  readonly etiqueta = computed(() => `${capitalizar(nombreMes(this.mes()))} ${this.anio()}`);
  readonly esMesActual = computed(
    () => this.anio() === this.hoy.getFullYear() && this.mes() === this.hoy.getMonth() + 1,
  );

  mover(delta: number): void {
    const fecha = new Date(this.anio(), this.mes() - 1 + delta, 1);
    this.anio.set(fecha.getFullYear());
    this.mes.set(fecha.getMonth() + 1);
  }
}
