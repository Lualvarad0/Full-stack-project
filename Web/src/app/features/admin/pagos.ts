import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { LucideCircleCheck, LucideCircleX } from '@lucide/angular';
import { AdminService } from '@core/admin';
import { cargar } from '@core/http';
import { ErrorCarga } from '@shared/components';
import { DineroPipe } from '@shared/pipes';
/** Panel de superadministrador: el negocio de un vistazo, usuarios, pagos y estado del servidor. */
@Component({
  selector: 'app-admin-pagos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ErrorCarga, DatePipe, DineroPipe, LucideCircleCheck, LucideCircleX],
  templateUrl: './pagos.html',
})
export class AdminPagos {
  private readonly servicio = inject(AdminService);
  protected readonly pagos = cargar(
    () => 0,
    () => this.servicio.pagos(),
  );
}
