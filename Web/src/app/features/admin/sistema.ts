import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { LucideCircleCheck, LucideCircleX } from '@lucide/angular';
import { AdminService } from '@core/admin';
import { cargar } from '@core/http';
import { ErrorCarga } from '@shared/components';
/** Panel de superadministrador: el negocio de un vistazo, usuarios, pagos y estado del servidor. */
@Component({
  selector: 'app-admin-sistema',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ErrorCarga, DatePipe, LucideCircleCheck, LucideCircleX],
  templateUrl: './sistema.html',
})
export class AdminSistema {
  private readonly servicio = inject(AdminService);
  protected readonly sistema = cargar(
    () => 0,
    () => this.servicio.sistema(),
  );
}
