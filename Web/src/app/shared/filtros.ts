import { ChangeDetectionStrategy, Component, computed, input, model, signal } from '@angular/core';
import { LucideCalendarRange, LucideSlidersHorizontal, LucideX } from '@lucide/angular';
import { Segmentado } from './controles';

/** Filtros de fecha y monto que se suman a la búsqueda y la categoría de cada lista. */
export interface FiltroRango {
  /** false: usa el mes de la barra superior; true: usa desde/hasta. */
  rango: boolean;
  desde: string;
  hasta: string;
  min: number | null;
  max: number | null;
}

export const FILTRO_VACIO: FiltroRango = { rango: false, desde: '', hasta: '', min: null, max: null };

/** Convierte el filtro en parámetros para la API (el mes se usa solo si no hay rango). */
export function parametrosFiltro(f: FiltroRango, anio: number, mes: number) {
  const usarRango = f.rango && (f.desde || f.hasta);
  return {
    anio: usarRango ? undefined : anio,
    mes: usarRango ? undefined : mes,
    desde: usarRango ? f.desde || undefined : undefined,
    hasta: usarRango ? f.hasta || undefined : undefined,
    min: f.min ?? undefined,
    max: f.max ?? undefined,
  };
}

/** "03/01/2026 – 31/03/2026" si hay un rango activo; null si se usa el mes. */
export function etiquetaRango(f: FiltroRango): string | null {
  if (!f.rango || (!f.desde && !f.hasta)) return null;
  const fecha = (iso: string) => iso.split('-').reverse().join('/');
  if (f.desde && f.hasta) return `${fecha(f.desde)} – ${fecha(f.hasta)}`;
  return f.desde ? `desde el ${fecha(f.desde)}` : `hasta el ${fecha(f.hasta)}`;
}

export function hayFiltroRango(f: FiltroRango): boolean {
  return (f.rango && !!(f.desde || f.hasta)) || f.min !== null || f.max !== null;
}

@Component({
  selector: 'app-filtros',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Segmentado, LucideSlidersHorizontal, LucideCalendarRange, LucideX],
  template: `
    <button
      type="button"
      class="btn btn-secondary min-h-10"
      [attr.aria-expanded]="abierto()"
      aria-controls="panel-filtros"
      (click)="abierto.set(!abierto())"
    >
      <svg lucideSlidersHorizontal class="size-4" aria-hidden="true"></svg>
      Filtros
      @if (activos() > 0) {
        <span class="grid size-5 place-items-center rounded-full bg-brand text-[11px] font-semibold text-brand-ink">{{ activos() }}</span>
      }
    </button>

    @if (abierto()) {
      <div id="panel-filtros" class="card mt-3 w-full basis-full p-4 sm:p-5">
        <div class="grid gap-5 md:grid-cols-2">
          <fieldset>
            <legend class="field-label">Fechas</legend>
            <app-segmentado [opciones]="tiposPeriodo()" [valor]="valor().rango" (valorChange)="cambiar({ rango: $event })" etiqueta="Tipo de período" />

            @if (valor().rango) {
              <div class="mt-3 grid grid-cols-2 gap-3">
                <label class="text-sm">
                  <span class="mb-1 block text-muted-fg">Desde</span>
                  <input type="date" class="input" [value]="valor().desde" [max]="valor().hasta || null" (change)="cambiar({ desde: $any($event.target).value })" />
                </label>
                <label class="text-sm">
                  <span class="mb-1 block text-muted-fg">Hasta</span>
                  <input type="date" class="input" [value]="valor().hasta" [min]="valor().desde || null" (change)="cambiar({ hasta: $any($event.target).value })" />
                </label>
              </div>
              <div class="mt-3 flex flex-wrap gap-2">
                @for (p of atajos; track p.nombre) {
                  <button type="button" class="pildora border border-border bg-card px-3 py-1 text-fg hover:bg-muted" (click)="aplicarAtajo(p.meses)">
                    <svg lucideCalendarRange class="size-3.5" aria-hidden="true"></svg> {{ p.nombre }}
                  </button>
                }
              </div>
            }
          </fieldset>

          <fieldset>
            <legend class="field-label">Monto total (USD)</legend>
            <div class="grid grid-cols-2 gap-3">
              <label class="text-sm">
                <span class="mb-1 block text-muted-fg">Mínimo</span>
                <input type="number" inputmode="decimal" min="0" step="0.01" class="input" placeholder="0,00"
                  [value]="valor().min ?? ''" (change)="cambiar({ min: numero($any($event.target).value) })" />
              </label>
              <label class="text-sm">
                <span class="mb-1 block text-muted-fg">Máximo</span>
                <input type="number" inputmode="decimal" min="0" step="0.01" class="input" placeholder="Sin límite"
                  [value]="valor().max ?? ''" (change)="cambiar({ max: numero($any($event.target).value) })" />
              </label>
            </div>
            @if (montoInvalido()) {
              <p class="field-error">El mínimo no puede ser mayor que el máximo.</p>
            }
          </fieldset>
        </div>

        @if (activos() > 0) {
          <button type="button" class="btn btn-ghost mt-4 -ml-3" (click)="valor.set(vacio)">
            <svg lucideX class="size-4" aria-hidden="true"></svg> Limpiar fechas y montos
          </button>
        }
      </div>
    }
  `,
  host: { class: 'contents' },
})
export class Filtros {
  readonly valor = model<FiltroRango>(FILTRO_VACIO);
  readonly etiquetaMes = input('Mes seleccionado');

  protected readonly abierto = signal(false);
  protected readonly tiposPeriodo = computed(() => [
    { valor: false, etiqueta: this.etiquetaMes() },
    { valor: true, etiqueta: 'Rango de fechas' },
  ]);
  protected readonly vacio = FILTRO_VACIO;
  protected readonly atajos = [
    { nombre: 'Últimos 3 meses', meses: 3 },
    { nombre: 'Últimos 6 meses', meses: 6 },
    { nombre: 'Este año', meses: 0 },
  ];

  protected readonly activos = computed(() => {
    const v = this.valor();
    return (v.rango && (v.desde || v.hasta) ? 1 : 0) + (v.min !== null || v.max !== null ? 1 : 0);
  });
  protected readonly montoInvalido = computed(() => {
    const { min, max } = this.valor();
    return min !== null && max !== null && min > max;
  });

  protected cambiar(parcial: Partial<FiltroRango>): void {
    this.valor.update((v) => ({ ...v, ...parcial }));
  }

  protected aplicarAtajo(meses: number): void {
    const hoy = new Date();
    const desde = meses === 0 ? new Date(hoy.getFullYear(), 0, 1) : new Date(hoy.getFullYear(), hoy.getMonth() - meses + 1, 1);
    this.cambiar({ rango: true, desde: iso(desde), hasta: iso(hoy) });
  }

  protected numero(texto: string): number | null {
    const n = Number(texto);
    return texto === '' || Number.isNaN(n) || n < 0 ? null : n;
  }
}

function iso(fecha: Date): string {
  const mes = String(fecha.getMonth() + 1).padStart(2, '0');
  const dia = String(fecha.getDate()).padStart(2, '0');
  return `${fecha.getFullYear()}-${mes}-${dia}`;
}
