import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { Barra, PuntoComparado, PuntoMes } from '@core/ui';
import { DineroPipe } from '@shared/pipes';
/**
 * Barras horizontales con etiqueta y valor directos (magnitud por categoría).
 * Marcas delgadas, extremo redondeado, valores en tinta de texto.
 */
@Component({
  selector: 'app-barras-horizontales',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DineroPipe],
  template: `
    <ul class="space-y-3" [attr.aria-label]="titulo()">
      @for (b of barras(); track b.etiqueta) {
        <li
          class="group"
          [title]="b.etiqueta + ': ' + (b.valor | dinero) + (b.detalle ? ' · ' + b.detalle : '')"
        >
          <div class="mb-1 flex items-baseline justify-between gap-3 text-sm">
            <span class="inline-flex min-w-0 items-center gap-2">
              <span
                class="size-2.5 shrink-0 rounded-full"
                [style.background]="b.color"
                aria-hidden="true"
              ></span>
              <span class="truncate">{{ b.etiqueta }}</span>
              @if (b.detalle) {
                <span class="hidden shrink-0 text-xs text-muted-fg sm:inline">{{ b.detalle }}</span>
              }
            </span>
            <span class="money shrink-0 font-medium">{{ b.valor | dinero }}</span>
          </div>
          <div class="h-2 overflow-hidden rounded-full bg-muted">
            <div
              class="h-full rounded-full transition-[width,opacity] duration-300 group-hover:opacity-80"
              [style.width.%]="ancho(b.valor)"
              [style.background]="b.color"
            ></div>
          </div>
        </li>
      }
    </ul>
  `,
})
export class BarrasHorizontales {
  readonly barras = input.required<Barra[]>();
  readonly titulo = input('');

  private readonly maximo = computed(() =>
    Math.max(...this.barras().map((b) => Math.abs(b.valor)), 0),
  );

  protected ancho(valor: number): number {
    const max = this.maximo();
    return max > 0 ? Math.max((Math.abs(valor) / max) * 100, valor !== 0 ? 1.5 : 0) : 0;
  }
}

/**
 * Columnas agrupadas por mes con dos series en la misma escala (USD): ingresos vs gastos.
 * Leyenda siempre visible; tooltip con ambos valores al pasar el cursor o enfocar.
 */
@Component({
  selector: 'app-barras-comparadas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DineroPipe],
  template: `
    <div class="mb-4 flex flex-wrap gap-4 text-xs text-muted-fg">
      <span class="inline-flex items-center gap-1.5"
        ><span class="size-2.5 rounded-sm" [style.background]="colorA()"></span>{{ serieA() }}</span
      >
      <span class="inline-flex items-center gap-1.5"
        ><span class="size-2.5 rounded-sm" [style.background]="colorB()"></span>{{ serieB() }}</span
      >
    </div>
    <div class="relative">
      <div
        class="flex h-48 items-end gap-1 border-b border-border sm:gap-2"
        role="list"
        [attr.aria-label]="titulo()"
      >
        @for (p of puntos(); track p.mes) {
          <button
            type="button"
            role="listitem"
            class="group relative flex h-full flex-1 items-end justify-center gap-[2px] rounded-t"
            [attr.aria-label]="
              p.etiqueta +
              ': ' +
              serieA() +
              ' ' +
              (p.a | dinero) +
              ', ' +
              serieB() +
              ' ' +
              (p.b | dinero)
            "
            (mouseenter)="activo.set(p.mes)"
            (mouseleave)="activo.set(null)"
            (focus)="activo.set(p.mes)"
            (blur)="activo.set(null)"
          >
            <span
              class="block w-full max-w-4 rounded-t-[3px] transition-opacity group-hover:opacity-80"
              [style.height.%]="alto(p.a)"
              [style.background]="colorA()"
              [style.opacity]="resaltado() && p.mes !== resaltado() ? 0.45 : 1"
            ></span>
            <span
              class="block w-full max-w-4 rounded-t-[3px] transition-opacity group-hover:opacity-80"
              [style.height.%]="alto(p.b)"
              [style.background]="colorB()"
              [style.opacity]="resaltado() && p.mes !== resaltado() ? 0.45 : 1"
            ></span>
            @if (activo() === p.mes) {
              <span
                class="pointer-events-none absolute bottom-full z-10 mb-2 rounded-lg border border-border bg-card px-3 py-2 text-left text-xs whitespace-nowrap shadow-lg"
              >
                <span class="block font-medium text-fg">{{ p.etiqueta }}</span>
                <span class="mt-1 flex items-center gap-1.5 text-fg"
                  ><span class="size-2 rounded-sm" [style.background]="colorA()"></span
                  >{{ serieA() }}
                  <span class="money ml-auto pl-3 font-semibold">{{ p.a | dinero }}</span></span
                >
                <span class="flex items-center gap-1.5 text-fg"
                  ><span class="size-2 rounded-sm" [style.background]="colorB()"></span
                  >{{ serieB() }}
                  <span class="money ml-auto pl-3 font-semibold">{{ p.b | dinero }}</span></span
                >
              </span>
            }
          </button>
        }
      </div>
      <div class="mt-2 flex gap-1 text-center text-xs text-muted-fg sm:gap-2" aria-hidden="true">
        @for (p of puntos(); track p.mes) {
          <span
            class="flex-1"
            [class.font-semibold]="p.mes === resaltado()"
            [class.text-fg]="p.mes === resaltado()"
            >{{ p.etiqueta.slice(0, 3) }}</span
          >
        }
      </div>
    </div>
  `,
})
export class BarrasComparadas {
  readonly puntos = input.required<PuntoComparado[]>();
  readonly serieA = input('Ingresos');
  readonly serieB = input('Gastos');
  readonly colorA = input('var(--bar)');
  readonly colorB = input('var(--cat-2)');
  readonly resaltado = input<number | null>(null);
  readonly titulo = input('');

  protected readonly activo = signal<number | null>(null);
  private readonly maximo = computed(() =>
    Math.max(...this.puntos().flatMap((p) => [p.a, p.b]), 0),
  );

  protected alto(valor: number): number {
    const max = this.maximo();
    if (max <= 0 || valor <= 0) return 0;
    return Math.max((valor / max) * 100, 2);
  }
}

/** Columnas por mes (una serie): el mes seleccionado se resalta; tooltip al pasar el cursor o enfocar. */
@Component({
  selector: 'app-barras-mes',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DineroPipe],
  template: `
    <div class="relative">
      <div
        class="flex h-44 items-end gap-1.5 border-b border-border sm:gap-2"
        role="list"
        [attr.aria-label]="titulo()"
      >
        @for (p of puntos(); track p.mes) {
          <button
            type="button"
            role="listitem"
            class="group relative flex h-full flex-1 items-end justify-center rounded-t focus-visible:outline-offset-0"
            [attr.aria-label]="p.etiqueta + ': ' + (p.valor | dinero)"
            (mouseenter)="activo.set(p.mes)"
            (mouseleave)="activo.set(null)"
            (focus)="activo.set(p.mes)"
            (blur)="activo.set(null)"
          >
            <span
              class="block w-full max-w-9 rounded-t transition-colors duration-150 group-hover:bg-bar group-focus-visible:bg-bar"
              [class.bg-bar]="p.mes === resaltado()"
              [class.bg-bar-soft]="p.mes !== resaltado()"
              [style.height.%]="alto(p.valor)"
            ></span>
            @if (activo() === p.mes) {
              <span
                class="pointer-events-none absolute bottom-full z-10 mb-2 whitespace-nowrap rounded-lg border border-border bg-card px-3 py-2 text-left text-xs shadow-lg"
              >
                <span class="block font-medium text-fg">{{ p.etiqueta }}</span>
                <span class="money block text-sm font-semibold text-fg">{{
                  p.valor | dinero
                }}</span>
                @if (p.detalle) {
                  <span class="block text-muted-fg">{{ p.detalle }}</span>
                }
              </span>
            }
          </button>
        }
      </div>
      <div class="mt-2 flex gap-1.5 text-center text-xs text-muted-fg sm:gap-2" aria-hidden="true">
        @for (p of puntos(); track p.mes) {
          <span
            class="flex-1"
            [class.font-semibold]="p.mes === resaltado()"
            [class.text-fg]="p.mes === resaltado()"
          >
            {{ p.etiqueta.slice(0, 3) }}
          </span>
        }
      </div>
    </div>
  `,
})
export class BarrasMes {
  readonly puntos = input.required<PuntoMes[]>();
  readonly resaltado = input<number | null>(null);
  readonly titulo = input('');

  protected readonly activo = signal<number | null>(null);
  private readonly maximo = computed(() => Math.max(...this.puntos().map((p) => p.valor), 0));

  protected alto(valor: number): number {
    const max = this.maximo();
    if (max <= 0 || valor <= 0) return 0;
    return Math.max((valor / max) * 100, 2);
  }
}
