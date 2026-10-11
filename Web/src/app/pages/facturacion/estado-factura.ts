import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { LucideCircleCheck, LucideCircleX, LucideClock } from '@lucide/angular';
import { ESTADOS_FACTURA } from '@core/constants';
import { EstadoFactura } from '@core/models';

/** Estado de una factura ante el SRI, con ícono y texto (nunca solo color). */
@Component({
  selector: 'app-estado-factura',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideCircleCheck, LucideCircleX, LucideClock],
  template: `
    <span class="pildora whitespace-nowrap" [class]="clase()">
      @switch (info().tono) {
        @case ('ok') { <svg lucideCircleCheck class="size-3.5" aria-hidden="true"></svg> }
        @case ('error') { <svg lucideCircleX class="size-3.5" aria-hidden="true"></svg> }
        @default { <svg lucideClock class="size-3.5" aria-hidden="true"></svg> }
      }
      {{ info().texto }}
    </span>
  `,
})
export class EstadoFacturaPildora {
  readonly estado = input.required<EstadoFactura>();
  protected readonly info = computed(() => ESTADOS_FACTURA[this.estado()]);
  protected readonly clase = computed(() => ({
    ok: 'bg-success-soft text-success',
    espera: 'bg-warning-soft text-warning',
    error: 'bg-danger-soft text-danger',
  })[this.info().tono]);
}
