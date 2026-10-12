import { computed, effect, inject, Injectable, signal, untracked } from '@angular/core';
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
    // Al entrar (o cambiar de contribuyente): el mes actual, o el anterior si así lo eligió en Configuración.
    // Solo depende de la clave de datos, para no reiniciar el mes cuando se edita el perfil.
    effect(() => {
      auth.claveDatos?.() ?? auth.usuario()?.id; // (algunas pruebas simulan AuthService sin claveDatos)
      const anterior = untracked(() => auth.usuario()?.mesAlEntrar === 'Anterior');
      const hoy = new Date();
      const inicio = new Date(hoy.getFullYear(), hoy.getMonth() - (anterior ? 1 : 0), 1);
      this.anio.set(inicio.getFullYear());
      this.mes.set(inicio.getMonth() + 1);
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
