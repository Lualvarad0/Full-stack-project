import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { PLANES } from '@core/suscripcion';
import { AdminService } from '@core/admin';
import { capitalizar, nombreMes } from '@core/utils';
import { cargar } from '@core/http';
import { BienvenidaResumen, ErrorCarga, Segmentado } from '@shared/components';
import { DineroPipe } from '@shared/pipes';
/** Panel de superadministrador: el negocio de un vistazo, usuarios, pagos y estado del servidor. */
@Component({
  selector: 'app-admin-resumen',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BienvenidaResumen, ErrorCarga, DineroPipe, Segmentado],
  templateUrl: './resumen.html',
})
export class AdminResumen {
  private readonly servicio = inject(AdminService);
  protected readonly planes = PLANES;
  protected readonly vista = signal<'registros' | 'planes'>('registros');
  protected readonly opciones = [
    { valor: 'registros', etiqueta: 'Registros por mes' },
    { valor: 'planes', etiqueta: 'Usuarios por plan' },
  ] as const;
  protected readonly metricas = cargar(
    () => 0,
    () => this.servicio.metricas(),
  );
  protected readonly maxRegistros = computed(() =>
    Math.max(0, ...(this.metricas().datos?.registrosPorMes.map((r) => r.usuarios) ?? [])),
  );

  protected mesCorto(mes: number): string {
    return capitalizar(nombreMes(mes).slice(0, 3));
  }
}
