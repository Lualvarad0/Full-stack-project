import { ChangeDetectionStrategy, Component, effect, input, output, signal } from '@angular/core';
import { LucideLoader } from '@lucide/angular';
import { EstadoCuenta, EstadoCuentaRequest } from '@core/tarjetas';
import { MensajeError, SelectorFecha } from '@shared/components';

/**
 * Datos del estado de cuenta: para confirmar (y corregir) lo leído del PDF o escribirlos a mano.
 * El usuario elige si la deuda del mes es el saldo total o el pago mínimo.
 */
@Component({
  selector: 'app-estado-cuenta-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensajeError, SelectorFecha, LucideLoader],
  template: `
    <form class="grid gap-3 sm:grid-cols-2" (submit)="$event.preventDefault(); enviar()" novalidate>
      <div>
        <label class="field-label" [attr.for]="id() + '-saldo'">Saldo total (total a pagar)</label>
        <input [id]="id() + '-saldo'" type="number" inputmode="decimal" min="0" step="0.01" class="input"
          [value]="saldo() ?? ''" (input)="saldo.set(numero($event))" />
      </div>
      <div>
        <label class="field-label" [attr.for]="id() + '-minimo'">Pago mínimo</label>
        <input [id]="id() + '-minimo'" type="number" inputmode="decimal" min="0" step="0.01" class="input"
          [value]="minimo() ?? ''" (input)="minimo.set(numero($event))" />
      </div>
      <div>
        <label class="field-label" [attr.for]="id() + '-maxima'">Fecha máxima de pago</label>
        <app-selector-fecha [idCampo]="id() + '-maxima'" [(valor)]="fechaMaxima" />
      </div>
      <div>
        <label class="field-label" [attr.for]="id() + '-corte'">Fecha de corte <span class="text-muted-fg">(opcional)</span></label>
        <app-selector-fecha [idCampo]="id() + '-corte'" [borrable]="true" [(valor)]="fechaCorte" />
      </div>
      <fieldset class="sm:col-span-2">
        <legend class="field-label">¿Qué vas a pagar este mes?</legend>
        <div class="flex flex-wrap gap-4 text-sm">
          <label class="flex items-center gap-2">
            <input type="radio" [name]="id() + '-pago'" [checked]="!pagarMinimo()" (change)="pagarMinimo.set(false)" /> El saldo total
          </label>
          <label class="flex items-center gap-2">
            <input type="radio" [name]="id() + '-pago'" [checked]="pagarMinimo()" (change)="pagarMinimo.set(true)" /> El pago mínimo
          </label>
        </div>
      </fieldset>
      <app-mensaje-error class="block sm:col-span-2" [mensaje]="error() ?? errorExterno()" />
      <div class="flex flex-wrap justify-end gap-2 sm:col-span-2">
        <ng-content />
        <button type="submit" class="btn btn-primary" [disabled]="guardando()">
          @if (guardando()) {
            <svg lucideLoader class="size-4 animate-spin" aria-hidden="true"></svg>
          }
          {{ etiqueta() }}
        </button>
      </div>
    </form>
  `,
})
export class EstadoCuentaForm {
  readonly id = input.required<string>();
  readonly inicial = input<EstadoCuenta | null>(null);
  readonly etiqueta = input('Guardar');
  readonly guardando = input(false);
  readonly errorExterno = input<string | null>(null, { alias: 'error' });
  readonly guardar = output<EstadoCuentaRequest>();

  protected readonly saldo = signal<number | null>(null);
  protected readonly minimo = signal<number | null>(null);
  protected readonly fechaMaxima = signal('');
  protected readonly fechaCorte = signal('');
  protected readonly pagarMinimo = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(() => {
      const e = this.inicial();
      this.saldo.set(e?.saldoTotal ?? null);
      this.minimo.set(e?.pagoMinimo ?? null);
      this.fechaMaxima.set(e?.fechaMaximaPago ?? '');
      this.fechaCorte.set(e?.fechaCorte ?? '');
    });
  }

  protected numero(evento: Event): number | null {
    const valor = (evento.target as HTMLInputElement).value;
    return valor === '' ? null : Math.round(Number(valor) * 100) / 100;
  }

  protected enviar(): void {
    const saldo = this.saldo();
    const minimo = this.minimo();
    if (!(saldo !== null && saldo > 0)) return this.error.set('Escribe el saldo total del estado de cuenta.');
    if (minimo !== null && minimo > saldo) return this.error.set('El pago mínimo no puede ser mayor que el saldo total.');
    if (!this.fechaMaxima()) return this.error.set('Indica la fecha máxima de pago.');
    if (this.pagarMinimo() && !(minimo !== null && minimo > 0)) return this.error.set('Escribe el pago mínimo.');
    if (this.fechaCorte() && this.fechaCorte() > this.fechaMaxima())
      return this.error.set('La fecha de corte va antes de la fecha máxima de pago.');
    this.error.set(null);
    this.guardar.emit({
      saldoTotal: saldo,
      pagoMinimo: minimo,
      fechaMaximaPago: this.fechaMaxima(),
      fechaCorte: this.fechaCorte() || null,
      pagarMinimo: this.pagarMinimo(),
    });
  }
}
