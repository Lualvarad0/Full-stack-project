import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';
import { TIPOS_IDENTIFICACION } from '@core/facturacion';
import { ClienteFacturado } from '@core/comprobantes';
import { soloDigitos } from '@core/utils';
import { Segmentado } from '@shared/components';
import { etiquetaIdentificacion, identificacionInvalida } from './validators/cliente.validator';
@Component({
  selector: 'app-cliente-factura',
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Segmentado],
  templateUrl: './cliente-formulario.html',
})
export class ClienteFacturaFormulario {
  readonly tipo = model.required<string>();
  readonly identificacion = model.required<string>();
  readonly razonSocial = model.required<string>();
  readonly correo = model.required<string>();
  readonly direccion = model.required<string>();
  readonly clientes = input<ClienteFacturado[]>([]);
  protected readonly tipos = TIPOS_IDENTIFICACION;
  protected readonly etiquetaIdentificacion = computed(() => etiquetaIdentificacion(this.tipo()));
  protected readonly identificacionInvalida = computed(() =>
    identificacionInvalida(this.tipo(), this.identificacion()),
  );
  protected cambiarTipo(tipo: string): void {
    this.tipo.set(tipo);
  }
  /** Si es un cliente al que ya facturaste, completa su nombre. */
  protected escribirIdentificacion(valor: string): void {
    const id = this.tipo() === '06' ? valor.trim() : soloDigitos(valor);
    this.identificacion.set(id);
    const conocido = this.clientes().find((c) => c.identificacion === id);
    if (conocido && !this.razonSocial()) this.razonSocial.set(conocido.razonSocial);
  }
}
