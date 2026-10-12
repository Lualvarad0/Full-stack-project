import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
/** Esqueleto común para listados, con una sola definición de tamaño y color. */
@Component({
  selector: 'app-lista-carga',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-3 p-5" role="status" [attr.aria-label]="etiqueta()">
      @for (fila of filas(); track fila) {
        <div class="h-12 animate-pulse rounded-lg bg-muted" aria-hidden="true"></div>
      }
    </div>
  `,
})
export class ListaCarga {
  readonly cantidad = input(3);
  readonly etiqueta = input('Cargando datos');
  protected readonly filas = computed(() => Array.from({ length: this.cantidad() }, (_, i) => i));
}
