import {
  ChangeDetectionStrategy,
  computed,
  Component,
  ElementRef,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { LucideLoader, LucideWallet } from '@lucide/angular';
import { MensajeError } from '@shared/components';

/**
 * Pregunta cuánto gana el usuario en el mes. Se abre sola la primera vez (sin ingreso registrado)
 * y desde "Cambiar" en el resumen. Usa <dialog> nativo: atrapa el foco y cierra con Escape.
 */
@Component({
  selector: 'app-ingreso-modal',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensajeError, LucideLoader, LucideWallet],
  template: `
    <dialog
      #dialogo
      class="m-auto w-[min(28rem,calc(100vw-2rem))] rounded-2xl border border-border bg-bg-2 p-0 text-fg shadow-xl backdrop:bg-black/40"
      aria-labelledby="titulo-ingreso"
      (close)="cerrado.emit()"
    >
      <form class="p-6" (submit)="$event.preventDefault(); guardar()" novalidate>
        <span class="grid size-11 place-items-center rounded-xl bg-primary-soft text-primary">
          <svg lucideWallet class="size-5" aria-hidden="true"></svg>
        </span>
        <h2 id="titulo-ingreso" class="mt-4 text-lg font-semibold">¿Cuánto ganas al mes?</h2>
        <p class="mt-1 text-sm text-muted-fg">
          Con este valor calculamos cuánto se va en tus cuentas y cuánto te queda en
          {{ etiquetaMes() }}. Si no cambia, lo usamos también los meses siguientes.
        </p>
        <label class="field-label mt-5" for="monto-ingreso">Ingreso del mes (lo que recibes)</label>
        <div class="relative">
          <span class="pointer-events-none absolute inset-y-0 left-3 grid place-items-center text-muted-fg" aria-hidden="true">$</span>
          <input
            id="monto-ingreso"
            type="number"
            inputmode="decimal"
            min="0"
            step="0.01"
            class="input pl-7 text-lg"
            autocomplete="off"
            [value]="valor() ?? ''"
            (input)="valor.set($any($event.target).value === '' ? null : +$any($event.target).value)"
          />
        </div>
        <app-mensaje-error class="mt-3 block" [mensaje]="mensaje()" />
        <div class="mt-6 flex flex-wrap justify-end gap-2">
          <button type="button" class="btn btn-ghost" (click)="cerrar()">
            {{ ingresoActual() === null ? 'Ahora no' : 'Cancelar' }}
          </button>
          <button type="submit" class="btn btn-primary" [disabled]="guardando()">
            @if (guardando()) {
              <svg lucideLoader class="size-4 animate-spin" aria-hidden="true"></svg>
            }
            Guardar ingreso
          </button>
        </div>
      </form>
    </dialog>
  `,
})
export class IngresoModal {
  readonly abierto = input(false);
  readonly ingresoActual = input<number | null>(null);
  readonly etiquetaMes = input('este mes');
  readonly guardando = input(false);
  readonly error = input<string | null>(null);
  readonly guardarIngreso = output<number>();
  readonly cerrado = output<void>();

  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');
  protected readonly valor = signal<number | null>(null);
  private readonly errorLocal = signal<string | null>(null);

  constructor() {
    effect(() => {
      const d = this.dialogo().nativeElement;
      if (this.abierto() && !d.open) {
        this.valor.set(this.ingresoActual());
        this.errorLocal.set(null);
        d.showModal();
      } else if (!this.abierto() && d.open) {
        d.close();
      }
    });
  }

  protected readonly mensaje = computed(() => this.errorLocal() ?? this.error());

  protected guardar(): void {
    const v = this.valor();
    if (v === null || !(v >= 0)) {
      this.errorLocal.set('Escribe cuánto ganas (puede ser 0).');
      return;
    }
    this.errorLocal.set(null);
    this.guardarIngreso.emit(Math.round(v * 100) / 100);
  }

  protected cerrar(): void {
    this.dialogo().nativeElement.close();
  }
}
