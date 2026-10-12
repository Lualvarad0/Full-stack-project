import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideTriangleAlert } from '@lucide/angular';
import { CategoriaGasto, ComprobanteLista, Direccion } from '@core/comprobantes';
import { CategoriaSelect } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';
@Component({
  selector: 'app-tabla-comprobantes',
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CategoriaSelect, DineroPipe, FechaCortaPipe, LucideTriangleAlert],
  templateUrl: './tabla-comprobantes.html',
})
export class TablaComprobantes {
  readonly lista = input.required<ComprobanteLista[]>();
  readonly direccion = input.required<Direccion>();
  readonly etiquetaPeriodo = input.required<string>();
  readonly cargando = input(false);
  readonly guardando = input<number | null>(null);
  readonly categoriaCambio = output<{ comprobante: ComprobanteLista; categoria: CategoriaGasto }>();
  protected readonly esIngreso = computed(() => this.direccion() === 'Emitido');
  protected esNotaCredito(c: ComprobanteLista): boolean {
    return c.tipoComprobante === 'Nota de crédito';
  }
}
