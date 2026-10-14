import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { LucideCalendarCheck, LucideFileText, LucideLoader, LucideMail } from '@lucide/angular';
import { CierreMes, DecisionCierre } from '@core/cuentas';
import { fechaIso, nombreMes } from '@core/utils';
import { MensajeError, SelectorFecha } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';

type Accion = 'Pagada' | 'Trasladar';

interface Respuesta {
  accion: Accion | null;
  /** Usar la fecha de la factura o del correo encontrado (si lo hay). */
  usarRespaldo: boolean;
  fecha: string;
}

/**
 * Cierre del mes (planes de pago): por cada cuenta que quedó pendiente, el usuario dice si ya la pagó o si la paga
 * el mes siguiente. Si hay una factura o un correo que respalde el pago se usa su fecha; si no, se pide el día.
 */
@Component({
  selector: 'app-cierre-mes-modal',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensajeError, SelectorFecha, DineroPipe, FechaCortaPipe, LucideCalendarCheck, LucideFileText, LucideMail, LucideLoader],
  template: `
    <dialog
      #dialogo
      class="m-auto w-[min(40rem,calc(100vw-2rem))] rounded-2xl border border-border bg-bg-2 p-0 text-fg shadow-xl backdrop:bg-black/40"
      aria-labelledby="titulo-cierre"
      (close)="cerrado.emit()"
    >
      @if (cierre(); as c) {
        <form class="flex max-h-[85dvh] flex-col" (submit)="$event.preventDefault(); guardar()" novalidate>
          <div class="border-b border-border p-6">
            <span class="grid size-11 place-items-center rounded-xl bg-primary-soft text-primary">
              <svg lucideCalendarCheck class="size-5" aria-hidden="true"></svg>
            </span>
            <h2 id="titulo-cierre" class="mt-4 text-lg font-semibold">Cerremos {{ etiquetaMes() }}</h2>
            <p class="mt-1 text-sm text-muted-fg">
              {{ c.pendientes.length === 1 ? 'Quedó 1 cuenta pendiente' : 'Quedaron ' + c.pendientes.length + ' cuentas pendientes' }}.
              ¿Ya las pagaste o las pagas en {{ siguienteMes() }}?
            </p>
          </div>

          <ul class="flex-1 divide-y divide-border overflow-y-auto">
            @for (p of c.pendientes; track p.cuenta.id; let i = $index) {
              <li class="px-6 py-4">
                <div class="flex items-baseline justify-between gap-3">
                  <p class="font-medium">{{ p.cuenta.nombre }}</p>
                  <p class="money cifra">{{ p.cuenta.monto | dinero }}</p>
                </div>

                @if (p.sugerencia; as s) {
                  <p class="mt-2 flex items-start gap-2 rounded-lg bg-success-soft px-3 py-2 text-xs text-fg">
                    @if (s.tipo === 'Factura') {
                      <svg lucideFileText class="mt-0.5 size-3.5 shrink-0 text-success" aria-hidden="true"></svg>
                    } @else {
                      <svg lucideMail class="mt-0.5 size-3.5 shrink-0 text-success" aria-hidden="true"></svg>
                    }
                    <span>
                      Encontramos {{ s.descripcion }} del {{ s.fecha | fechaCorta }}
                      @if (s.monto !== null) {
                        por {{ s.monto | dinero }}
                      }
                    </span>
                  </p>
                }

                <fieldset class="mt-3 flex flex-wrap gap-2">
                  <legend class="sr-only">¿Qué pasó con {{ p.cuenta.nombre }}?</legend>
                  @for (op of opciones; track op.valor) {
                    <label
                      class="flex min-h-9 cursor-pointer items-center gap-2 rounded-full border px-3 text-sm transition-colors"
                      [class]="respuestas()[i].accion === op.valor ? 'border-accent bg-primary-soft' : 'border-border hover:bg-muted'"
                    >
                      <input
                        type="radio"
                        class="accent-[var(--accent)]"
                        [name]="'accion-' + p.cuenta.id"
                        [checked]="respuestas()[i].accion === op.valor"
                        (change)="cambiar(i, { accion: op.valor })"
                      />
                      {{ op.valor === 'Trasladar' ? 'La pago en ' + siguienteMes() : op.etiqueta }}
                    </label>
                  }
                </fieldset>

                @if (respuestas()[i].accion === 'Pagada') {
                  <div class="mt-3 space-y-2 text-sm">
                    @if (p.sugerencia; as s) {
                      <label class="flex items-center gap-2">
                        <input
                          type="checkbox"
                          [checked]="respuestas()[i].usarRespaldo"
                          (change)="cambiar(i, { usarRespaldo: $any($event.target).checked })"
                        />
                        Usar la fecha {{ s.tipo === 'Factura' ? 'de la factura' : 'del correo' }} ({{ s.fecha | fechaCorta }})
                      </label>
                    }
                    @if (!p.sugerencia || !respuestas()[i].usarRespaldo) {
                      <div class="flex flex-wrap items-center gap-2">
                        <label [attr.for]="'fecha-cierre-' + p.cuenta.id" class="text-muted-fg">
                          No hay factura ni correo: ¿qué día la pagaste?
                        </label>
                        <app-selector-fecha
                          [idCampo]="'fecha-cierre-' + p.cuenta.id"
                          [compacto]="true"
                          [max]="hoy"
                          [valor]="respuestas()[i].fecha"
                          (valorChange)="cambiar(i, { fecha: $event })"
                        />
                      </div>
                    }
                  </div>
                }
              </li>
            }
          </ul>

          <div class="border-t border-border p-6">
            <app-mensaje-error class="mb-3 block" [mensaje]="mensaje()" />
            <div class="flex flex-wrap justify-end gap-2">
              <button type="button" class="btn btn-ghost" (click)="cerrar()">Decidir después</button>
              <button type="submit" class="btn btn-primary" [disabled]="guardando() || decididas() === 0">
                @if (guardando()) {
                  <svg lucideLoader class="size-4 animate-spin" aria-hidden="true"></svg>
                }
                Guardar {{ decididas() }} de {{ c.pendientes.length }}
              </button>
            </div>
          </div>
        </form>
      }
    </dialog>
  `,
})
export class CierreMesModal {
  readonly cierre = input<CierreMes | null>(null);
  readonly abierto = input(false);
  readonly guardando = input(false);
  readonly error = input<string | null>(null);
  readonly confirmar = output<DecisionCierre[]>();
  readonly cerrado = output<void>();

  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');
  protected readonly hoy = fechaIso(new Date());
  protected readonly opciones: readonly { valor: Accion; etiqueta: string }[] = [
    { valor: 'Pagada', etiqueta: 'Ya la pagué' },
    { valor: 'Trasladar', etiqueta: 'La pago el próximo mes' },
  ];
  protected readonly respuestas = signal<Respuesta[]>([]);
  private readonly errorLocal = signal<string | null>(null);
  protected readonly mensaje = computed(() => this.errorLocal() ?? this.error());
  protected readonly decididas = computed(() => this.respuestas().filter((r) => r.accion).length);
  protected readonly etiquetaMes = computed(() => {
    const c = this.cierre();
    return c ? `${nombreMes(c.mes)} ${c.anio}` : '';
  });
  protected readonly siguienteMes = computed(() => {
    const c = this.cierre();
    return c ? nombreMes((c.mes % 12) + 1) : 'el próximo mes';
  });

  constructor() {
    effect(() => {
      const d = this.dialogo().nativeElement;
      const c = this.cierre();
      if (this.abierto() && c && !d.open) {
        // Con factura o correo que respalde el pago, se propone "Ya la pagué" con esa fecha.
        this.respuestas.set(
          c.pendientes.map((p) => ({ accion: p.sugerencia ? 'Pagada' : null, usarRespaldo: !!p.sugerencia, fecha: '' })),
        );
        this.errorLocal.set(null);
        d.showModal();
      } else if (!this.abierto() && d.open) {
        d.close();
      }
    });
  }

  protected cambiar(i: number, cambios: Partial<Respuesta>): void {
    this.respuestas.update((rs) => rs.map((r, j) => (j === i ? { ...r, ...cambios } : r)));
  }

  protected guardar(): void {
    const c = this.cierre();
    if (!c) return;
    const decisiones: DecisionCierre[] = [];
    for (const [i, r] of this.respuestas().entries()) {
      const p = c.pendientes[i]!;
      if (!r.accion) continue;
      if (r.accion === 'Trasladar') {
        decisiones.push({ cuentaId: p.cuenta.id, accion: 'Trasladar' });
        continue;
      }
      const s = p.sugerencia;
      if (s && r.usarRespaldo) {
        decisiones.push({ cuentaId: p.cuenta.id, accion: 'Pagada', comprobanteId: s.comprobanteId, correoId: s.correoId });
        continue;
      }
      if (!r.fecha || r.fecha > this.hoy)
        return this.errorLocal.set(`Indica qué día pagaste "${p.cuenta.nombre}" (no puede ser posterior a hoy).`);
      decisiones.push({ cuentaId: p.cuenta.id, accion: 'Pagada', fecha: r.fecha });
    }
    this.errorLocal.set(null);
    this.confirmar.emit(decisiones);
  }

  protected cerrar(): void {
    this.dialogo().nativeElement.close();
  }
}
