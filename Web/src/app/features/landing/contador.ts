import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { MONEDA } from '@core/utils';
import { sinMovimiento } from './demo-datos';
/** Muestra un monto que sube o baja hasta su nuevo valor (sin animación si se pide reducir movimiento). */
@Component({
  selector: 'app-contador',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `{{ texto() }}`,
})
export class Contador {
  readonly valor = input.required<number>();
  private readonly actual = signal(0);
  protected readonly texto = computed(() => MONEDA.format(this.actual()));

  constructor() {
    let cuadro = 0;
    effect(() => {
      const destino = this.valor();
      const desde = untracked(this.actual);
      cancelAnimationFrame(cuadro);
      if (sinMovimiento() || desde === destino) {
        this.actual.set(destino);
        return;
      }
      const inicio = performance.now();
      const paso = (ahora: number) => {
        const t = Math.min(1, (ahora - inicio) / 900);
        this.actual.set(desde + (destino - desde) * (1 - Math.pow(1 - t, 3)));
        if (t < 1) cuadro = requestAnimationFrame(paso);
      };
      cuadro = requestAnimationFrame(paso);
    });
    inject(DestroyRef).onDestroy(() => cancelAnimationFrame(cuadro));
  }
}
