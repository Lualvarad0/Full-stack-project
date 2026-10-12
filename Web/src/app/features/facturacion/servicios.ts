import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideArrowLeft,
  LucideLoader,
  LucidePencil,
  LucidePlus,
  LucideTrash2,
} from '@lucide/angular';
import {
  TARIFAS_IVA,
  ServicioBorrador,
  ServicioFacturable,
  TarifaIva,
  FacturacionService,
} from '@core/facturacion';
import { AvisosService } from '@core/ui';
import { cargar, mensajeDeError } from '@core/http';
import { ErrorCarga, MensajeError, Titulo } from '@shared/components';
import { DineroPipe } from '@shared/pipes';
const VACIO: ServicioBorrador = { codigo: '', descripcion: '', precioUnitario: 0, tarifa: 'Iva15' };

/** Catálogo de productos y servicios que el usuario factura a menudo (se eligen al crear una factura). */
@Component({
  selector: 'app-servicios',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MensajeError,
    RouterLink,
    Titulo,
    ErrorCarga,
    DineroPipe,
    LucideArrowLeft,
    LucidePlus,
    LucidePencil,
    LucideTrash2,
    LucideLoader,
  ],
  templateUrl: './servicios.html',
})
export class Servicios {
  private readonly servicio = inject(FacturacionService);
  private readonly avisos = inject(AvisosService);

  protected readonly tarifas = TARIFAS_IVA;
  protected readonly vacio = VACIO;
  private readonly recargas = signal(0);
  protected readonly carga = cargar(
    () => this.recargas(),
    () => this.servicio.servicios(),
  );
  protected readonly borrador = signal<ServicioBorrador>(VACIO);
  protected readonly guardando = signal(false);
  protected readonly error = signal<string | null>(null);

  protected tarifa(valor: TarifaIva): string {
    return TARIFAS_IVA.find((t) => t.valor === valor)?.etiqueta ?? valor;
  }

  protected cambiar(cambios: Partial<ServicioBorrador>): void {
    this.borrador.update((b) => ({ ...b, ...cambios }));
  }

  protected editar(s: ServicioFacturable): void {
    this.error.set(null);
    this.borrador.set({
      id: s.id,
      codigo: s.codigo ?? '',
      descripcion: s.descripcion,
      precioUnitario: s.precioUnitario,
      tarifa: s.tarifa,
    });
  }

  protected guardar(): void {
    const b = this.borrador();
    if (b.descripcion.trim().length < 2) {
      this.error.set('Escribe la descripción del servicio.');
      return;
    }
    this.error.set(null);
    this.guardando.set(true);
    this.servicio
      .guardarServicio(
        {
          codigo: b.codigo.trim() || null,
          descripcion: b.descripcion.trim(),
          precioUnitario: b.precioUnitario,
          tarifa: b.tarifa,
        },
        b.id,
      )
      .subscribe({
        next: () => {
          this.guardando.set(false);
          this.avisos.exito(b.id ? 'Servicio actualizado.' : 'Servicio agregado.');
          this.borrador.set(VACIO);
          this.recargas.update((n) => n + 1);
        },
        error: (e) => {
          this.guardando.set(false);
          this.error.set(mensajeDeError(e));
        },
      });
  }

  protected eliminar(s: ServicioFacturable): void {
    this.servicio.eliminarServicio(s.id).subscribe({
      next: () => {
        this.avisos.exito(`Eliminaste "${s.descripcion}".`);
        if (this.borrador().id === s.id) this.borrador.set(VACIO);
        this.recargas.update((n) => n + 1);
      },
      error: (e) => this.avisos.error(e),
    });
  }
}
