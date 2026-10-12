import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import {
  LucideArrowLeft,
  LucideBan,
  LucideCircleAlert,
  LucideDownload,
  LucideLoader,
  LucideRefreshCw,
  LucideTrash2,
} from '@lucide/angular';
import { FORMAS_PAGO, TARIFAS_IVA, FacturaEmitida, FacturacionService } from '@core/facturacion';
import { AvisosService } from '@core/ui';
import { cargar } from '@core/http';
import { descargarArchivo } from '@core/utils';
import { ErrorCarga } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';
import { EstadoFacturaPildora } from './estado-factura';
import { FacturaDocumento } from './factura-documento';
import { AuthService } from '@core/auth';
/** Una factura emitida: su estado ante el SRI, los motivos si fue rechazada, el detalle y su XML. */
@Component({
  selector: 'app-factura-detalle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    DatePipe,
    DineroPipe,
    FechaCortaPipe,
    ErrorCarga,
    EstadoFacturaPildora,
    FacturaDocumento,
    LucideArrowLeft,
    LucideDownload,
    LucideRefreshCw,
    LucideLoader,
    LucideCircleAlert,
    LucideTrash2,
    LucideBan,
  ],
  templateUrl: './factura-detalle.html',
})
export class FacturaDetalle {
  /** Parámetro de la ruta. */
  readonly id = input.required<string>();

  private readonly servicio = inject(FacturacionService);
  private readonly avisos = inject(AvisosService);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  protected readonly carga = cargar(
    () => Number(this.id()),
    (id) => this.servicio.factura(id),
  );
  private readonly actualizada = signal<{ datos: FacturaEmitida; token: string | null; clave: string } | null>(null);
  protected readonly factura = computed(() => {
    const a = this.actualizada();
    return a?.datos.id === Number(this.id()) && a.token === this.auth.token() && a.clave === this.auth.claveDatos() ? a.datos : this.carga().datos;
  });
  protected readonly reintentando = signal(false);
  protected readonly puedeReintentar = computed(() =>
    ['Firmada', 'Recibida', 'EnProceso'].includes(this.factura()?.estado ?? ''),
  );
  protected readonly puedeBorrar = computed(() =>
    ['Firmada', 'Devuelta', 'NoAutorizada'].includes(this.factura()?.estado ?? ''),
  );
  protected readonly confirmar = signal<'borrar' | 'anular' | null>(null);
  protected readonly trabajando = signal(false);
  protected readonly mensajes = computed(() =>
    (this.factura()?.mensajes ?? '').split(' | ').filter(Boolean),
  );

  protected tarifa(valor: string): string {
    return TARIFAS_IVA.find((t) => t.valor === valor)?.etiqueta ?? valor;
  }

  protected formaPago(valor: string): string {
    return FORMAS_PAGO.find((f) => f.valor === valor)?.etiqueta ?? valor;
  }

  protected reintentar(): void {
    const f = this.factura();
    if (!f) return;
    this.reintentando.set(true);
    this.servicio.reintentar(f.id).subscribe({
      next: (r) => {
        this.reintentando.set(false);
        this.actualizada.set({ datos: r, token: this.auth.token(), clave: this.auth.claveDatos() });
        if (r.estado === 'Autorizada')
          this.avisos.exito(`Factura ${r.numero} autorizada por el SRI.`);
      },
      error: (e) => {
        this.reintentando.set(false);
        this.avisos.error(e);
      },
    });
  }

  protected eliminar(f: FacturaEmitida): void {
    this.trabajando.set(true);
    this.servicio.eliminar(f.id).subscribe({
      next: () => {
        this.avisos.exito(`Eliminaste la factura ${f.numero}.`);
        void this.router.navigate(['/facturacion']);
      },
      error: (e) => {
        this.trabajando.set(false);
        this.avisos.error(e);
      },
    });
  }

  protected anular(f: FacturaEmitida): void {
    this.trabajando.set(true);
    this.servicio.anular(f.id).subscribe({
      next: (r) => {
        this.trabajando.set(false);
        this.confirmar.set(null);
        this.actualizada.set({ datos: r, token: this.auth.token(), clave: this.auth.claveDatos() });
        this.avisos.exito('Factura validada. Realiza el trámite en SRI en Línea; su estado se conserva.');
      },
      error: (e) => {
        this.trabajando.set(false);
        this.avisos.error(e);
      },
    });
  }

  protected descargarXml(f: FacturaEmitida): void {
    this.servicio.descargarXml(f.id).subscribe({
      next: (blob) => {
        descargarArchivo(blob, `factura-${f.numero}.xml`);
      },
      error: (e) => this.avisos.error(e),
    });
  }
}
