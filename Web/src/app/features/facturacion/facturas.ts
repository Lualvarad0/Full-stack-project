import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { LucideFilePlus2, LucideFileText, LucideTriangleAlert } from '@lucide/angular';
import { FacturacionService } from '@core/facturacion';
import { cargar } from '@core/http';
import { CupoPlanContador, ErrorCarga, Titulo } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';
import { EstadoFacturaPildora } from './estado-factura';
/** Facturas emitidas desde Nudo y acceso a crear una nueva. */
@Component({
  selector: 'app-facturas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    Titulo,
    ErrorCarga,
    CupoPlanContador,
    DineroPipe,
    FechaCortaPipe,
    EstadoFacturaPildora,
    LucideFilePlus2,
    LucideFileText,
    LucideTriangleAlert,
  ],
  templateUrl: './facturas.html',
})
export class Facturas {
  private readonly servicio = inject(FacturacionService);
  protected readonly carga = cargar(
    () => 0,
    () =>
      forkJoin({
        facturas: this.servicio.facturas(),
        datos: this.servicio.datos(),
        cupo: this.servicio.cupo(),
      }),
  );
}
