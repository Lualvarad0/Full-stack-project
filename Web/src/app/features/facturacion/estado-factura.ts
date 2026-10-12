import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { LucideCircleCheck, LucideCircleX, LucideClock } from '@lucide/angular';
import { ESTADOS_FACTURA, EstadoFactura } from '@core/facturacion';
/** Estado de una factura ante el SRI, con ícono y texto (nunca solo color). */
@Component({
  selector: 'app-estado-factura',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideCircleCheck, LucideCircleX, LucideClock],
  templateUrl: './estado-factura.html',
})
export class EstadoFacturaPildora {
  readonly estado = input.required<EstadoFactura>();
  protected readonly info = computed(() => ESTADOS_FACTURA[this.estado()]);
  protected readonly clase = computed(
    () =>
      ({
        ok: 'estado-ok',
        espera: 'estado-atencion',
        error: 'estado-error',
      })[this.info().tono],
  );
}
