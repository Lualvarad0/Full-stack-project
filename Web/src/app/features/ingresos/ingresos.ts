import { PerfilRequerido } from '@shared/components/perfil-requerido';
import { ListaCarga } from '@shared/components/lista-carga';
import { TablaComprobantes } from '../comprobante/tabla-comprobantes';
import { ListadoComprobantes } from '../comprobante/listado-comprobantes';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideFileUp, LucideX } from '@lucide/angular';
import { FiltroComprobantes } from '@core/comprobantes';
import { PerfilService } from '@core/perfil';
import { Buscador, ErrorCarga, Filtros, Monto, Titulo } from '@shared/components';
/** Facturas emitidas por el usuario (ingresos) en el mes seleccionado. */
@Component({
  selector: 'app-ingresos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PerfilRequerido,
    ListaCarga,
    TablaComprobantes,
    Buscador,
    ErrorCarga,
    Titulo,
    Filtros,
    Monto,
    RouterLink,
    LucideFileUp,
    LucideX,
  ],
  templateUrl: './ingresos.html',
})
export class Ingresos extends ListadoComprobantes {
  /** ?cliente=<identificación> */
  readonly cliente = input<string | undefined>();

  protected readonly perfil = inject(PerfilService);
  protected readonly direccion = 'Emitido' as const;

  protected override filtrosAdicionales(): FiltroComprobantes {
    // El RUC es una dependencia reactiva: al actualizar el perfil se recarga el listado.
    this.perfil.perfil()?.ruc;
    return { cliente: this.cliente() };
  }
  protected override filtrosActivos(): boolean {
    return !!this.cliente();
  }

  protected readonly clienteFiltrado = computed(() => {
    const id = this.cliente();
    return id
      ? (this.lista().find((c) => c.identificacionComprador === id)?.razonSocialComprador ?? id)
      : null;
  });
}
