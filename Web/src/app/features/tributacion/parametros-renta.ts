import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ParametrosRenta,
  TributacionService,
} from '@core/contabilidad/services/tributacion.service';
import { PaginaContable } from '../contabilidad/pagina-contable';
@Component({
  selector: 'app-parametros-renta',
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: ` <section class="card p-5 space-y-4">
    <h2 class="font-semibold">Valores externos y conciliación de renta</h2>
    <p class="text-sm text-muted-fg">
      Completa los sueldos gravados, aporte personal al IESS y retenciones del formulario 107. Los
      ajustes recogen operaciones que faltan en las facturas y beneficios acreditados; conserva su
      sustento.
    </p>
    @if (valores(); as p) {
      <div class="grid gap-3 md:grid-cols-3">
        @for (c of campos; track c.clave) {
          <label
            >{{ c.etiqueta
            }}<input
              class="input"
              type="number"
              [min]="c.clave === 'ingresosRimpeAjuste' ? -999999999 : 0"
              max="999999999"
              step="0.01"
              [(ngModel)]="p[c.clave]"
              [disabled]="!cartera.puedeEditar()"
          /></label>
        }
      </div>
      <label class="block"
        >Sustento de valores externos y ajustes<textarea
          class="input"
          maxlength="2000"
          rows="3"
          [(ngModel)]="p.sustentoAjustes"
          [disabled]="!cartera.puedeEditar()"
        ></textarea>
      </label>
      <button
        class="btn btn-primary"
        [disabled]="ocupado() || !cartera.puedeEditar()"
        (click)="guardar()"
      >
        Guardar valores de renta
      </button>
    }
  </section>`,
})
export class ParametrosRentaFormulario extends PaginaContable {
  readonly parametros = input.required<ParametrosRenta>();
  readonly cambiado = output<void>();
  private readonly api = inject(TributacionService);
  protected readonly valores = computed(() => structuredClone(this.parametros()));
  protected readonly campos = [
    { clave: 'sueldosGravados', etiqueta: 'Sueldos gravados (formulario 107)' },
    { clave: 'iessPersonal', etiqueta: 'IESS personal' },
    { clave: 'retencionesSueldo', etiqueta: 'Retenciones del empleador' },
    { clave: 'otrosIngresosGeneral', etiqueta: 'Otros ingresos del régimen general' },
    { clave: 'otrosGastosGeneral', etiqueta: 'Otros costos deducibles del régimen general' },
    { clave: 'ingresosRimpeAjuste', etiqueta: 'Ajuste de ingresos RIMPE percibidos' },
    { clave: 'exoneraciones', etiqueta: 'Exoneraciones y beneficios acreditados' },
    { clave: 'anticiposRenta', etiqueta: 'Anticipos de renta pagados' },
  ] as const;
  protected override refrescar(): void {
    this.cambiado.emit();
  }
  protected guardar(): void {
    this.ejecutar(this.api.guardarRenta(2026, this.valores()), 'Valores de renta actualizados.');
  }
}
