import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CupoPlan } from '@core/suscripcion';

/** A partir de este porcentaje del límite el contador se pone en ámbar. */
const UMBRAL_AVISO = 0.9;

/**
 * Contador "usados de límite" con barra (facturas del mes, contribuyentes de la cartera).
 * <app-cupo-plan titulo="Facturas de este mes" unidad="facturas" [cupo]="cupo" ayuda="…" />
 */
@Component({
  selector: 'app-cupo-plan',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    @if (cupo(); as c) {
      <section class="card p-4 sm:p-5" [attr.aria-label]="titulo()">
        <div class="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
          <p class="text-sm font-medium">{{ titulo() }}</p>
          <p class="text-sm">
            <span class="cifra text-base" [class.text-danger]="estado() === 'lleno'" [class.text-warning]="estado() === 'casi'">{{
              c.usados
            }}</span>
            @if (c.limite !== null) {
              <span class="text-muted-fg"> de {{ c.limite }} {{ unidad() }}</span>
            } @else {
              <span class="text-muted-fg"> {{ unidad() }} · sin límite</span>
            }
          </p>
        </div>
        @if (c.limite !== null) {
          <div
            class="mt-2 h-2 overflow-hidden rounded-full bg-muted"
            role="progressbar"
            aria-valuemin="0"
            [attr.aria-valuemax]="c.limite"
            [attr.aria-valuenow]="c.usados"
            [attr.aria-label]="titulo()"
          >
            <div
              class="h-full rounded-full transition-[width] duration-500"
              [class]="estado() === 'lleno' ? 'bg-danger' : estado() === 'casi' ? 'bg-warning' : 'bg-brand'"
              [style.width.%]="porcentaje()"
            ></div>
          </div>
          <p class="mt-1.5 text-xs" [class.text-danger]="estado() === 'lleno'" [class.text-muted-fg]="estado() !== 'lleno'">
            @switch (estado()) {
              @case ('lleno') {
                {{ mensajeLleno() }}
              }
              @case ('casi') {
                @let resto = c.limite - c.usados;
                {{ resto === 1 ? 'Te queda 1.' : 'Te quedan ' + resto + '.' }} {{ ayuda() }}
              }
              @default {
                {{ ayuda() }}
              }
            }
          </p>
        }
      </section>
    }
  `,
})
export class CupoPlanContador {
  readonly cupo = input<CupoPlan | null>(null);
  readonly titulo = input.required<string>();
  readonly unidad = input.required<string>();
  /** Texto bajo la barra mientras queda espacio. */
  readonly ayuda = input('');
  /** Texto cuando se llegó al límite. */
  readonly mensajeLleno = input('Llegaste al límite de tu plan.');

  protected readonly porcentaje = computed(() => {
    const c = this.cupo();
    return c?.limite ? Math.min(100, (c.usados / c.limite) * 100) : 0;
  });
  protected readonly estado = computed<'libre' | 'casi' | 'lleno'>(() => {
    const c = this.cupo();
    if (!c || c.limite === null) return 'libre';
    if (c.usados >= c.limite) return 'lleno';
    return c.usados >= c.limite * UMBRAL_AVISO ? 'casi' : 'libre';
  });
}
