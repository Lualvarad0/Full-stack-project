import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ParametrosIva, TributacionService } from '@core/contabilidad/services/tributacion.service';
import { PaginaContable } from '../contabilidad/pagina-contable';
@Component({
  selector: 'app-parametros-iva',
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: ` <section class="card p-5 space-y-4">
    <h2 class="font-semibold">Saldo anterior y ajustes de IVA</h2>
    <p class="text-sm text-muted-fg">
      El factor de crédito y los ajustes requieren revisión del contador. No se deducen únicamente
      por encontrar IVA en una factura.
    </p>
    @if (valores(); as p) {
      <div class="grid gap-3 md:grid-cols-4">
        <label
          >Factor de crédito (0 a 1)<input
            class="input"
            type="number"
            min="0"
            max="1"
            step="0.0001"
            [(ngModel)]="p.factorCredito" /></label
        ><label
          >Crédito de períodos anteriores<input
            class="input"
            type="number"
            min="0"
            max="999999999"
            step="0.01"
            [(ngModel)]="p.creditoAnterior" /></label
        ><label
          >Ajuste del débito<input
            class="input"
            type="number"
            min="-999999999"
            max="999999999"
            step="0.01"
            [(ngModel)]="p.ajusteDebito" /></label
        ><label
          >Ajuste del crédito<input
            class="input"
            type="number"
            min="-999999999"
            max="999999999"
            step="0.01"
            [(ngModel)]="p.ajusteCredito"
        /></label>
      </div>
      <label class="block"
        >Sustento<textarea
          class="input"
          rows="3"
          maxlength="2000"
          [(ngModel)]="p.sustentoAjustes"
        ></textarea>
      </label>
      <button
        class="btn btn-primary"
        [disabled]="ocupado() || !cartera.puedeEditar()"
        (click)="guardar()"
      >
        Guardar ajustes del período
      </button>
    }
  </section>`,
})
export class ParametrosIvaFormulario extends PaginaContable {
  readonly parametros = input.required<ParametrosIva>();
  readonly mes = input.required<number>();
  readonly meses = input.required<number>();
  readonly cambiado = output<void>();
  private readonly api = inject(TributacionService);
  protected readonly valores = computed(() => structuredClone(this.parametros()));
  protected override refrescar(): void {
    this.cambiado.emit();
  }
  protected guardar(): void {
    this.ejecutar(
      this.api.guardarIva(2026, this.mes(), this.meses(), this.valores()),
      'Ajustes de IVA actualizados.',
    );
  }
}
