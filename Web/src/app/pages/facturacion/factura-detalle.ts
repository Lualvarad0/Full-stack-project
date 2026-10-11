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
import { FORMAS_PAGO, TARIFAS_IVA } from '@core/constants';
import { FacturaEmitida } from '@core/models';
import { AvisosService, FacturacionService } from '@core/services';
import { cargar, descargarArchivo } from '@core/utils';
import { ErrorCarga } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';
import { EstadoFacturaPildora } from './estado-factura';

/** Una factura emitida: su estado ante el SRI, los motivos si fue rechazada, el detalle y su XML. */
@Component({
  selector: 'app-factura-detalle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, DineroPipe, FechaCortaPipe, ErrorCarga, EstadoFacturaPildora, LucideArrowLeft, LucideDownload, LucideRefreshCw, LucideLoader, LucideCircleAlert, LucideTrash2, LucideBan],
  template: `
    <a routerLink="/facturacion" class="btn btn-ghost -ml-3">
      <svg lucideArrowLeft class="size-4" aria-hidden="true"></svg> Volver a facturas
    </a>

    @if (factura(); as f) {
      <div class="mt-2 flex flex-wrap items-start justify-between gap-4">
        <div>
          <p class="text-sm text-muted-fg">Factura {{ f.ambiente === 'Pruebas' ? '· ambiente de pruebas' : '' }}</p>
          <h1 class="titular font-mono text-[clamp(28px,4vw,40px)]">{{ f.numero }}</h1>
          <div class="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted-fg">
            <app-estado-factura [estado]="f.estado" />
            Emitida el {{ f.fechaEmision | fechaCorta }}
            @if (f.fechaAutorizacion) { · autorizada el {{ f.fechaAutorizacion | date: 'dd/MM/yyyy HH:mm' }} }
          </div>
        </div>
        <div class="flex flex-wrap gap-2">
          @if (puedeReintentar()) {
            <button type="button" class="btn btn-primary" [disabled]="reintentando()" (click)="reintentar()">
              @if (reintentando()) { <svg lucideLoader class="size-4 animate-spin" aria-hidden="true"></svg> } @else { <svg lucideRefreshCw class="size-4" aria-hidden="true"></svg> }
              {{ f.estado === 'Firmada' ? 'Enviar al SRI' : 'Consultar autorización' }}
            </button>
          }
          <button type="button" class="btn btn-secondary" (click)="descargarXml(f)">
            <svg lucideDownload class="size-4" aria-hidden="true"></svg> XML
          </button>
          @if (puedeBorrar()) {
            <button type="button" class="btn btn-ghost text-danger" (click)="confirmar.set('borrar')">
              <svg lucideTrash2 class="size-4" aria-hidden="true"></svg> Eliminar
            </button>
          } @else if (f.estado === 'Autorizada') {
            <button type="button" class="btn btn-ghost text-danger" (click)="confirmar.set('anular')">
              <svg lucideBan class="size-4" aria-hidden="true"></svg> Anular
            </button>
          }
        </div>
      </div>

      @if (confirmar(); as accion) {
        <div class="aviso-atencion mt-5" role="alertdialog" aria-label="Confirmar">
          <div class="flex-1 text-sm">
            @if (accion === 'borrar') {
              <p class="font-medium">¿Eliminar esta factura?</p>
              <p>Nunca se autorizó, así que no existe para el SRI. Se borra de Nudo.</p>
            } @else {
              <p class="font-medium">¿Anular la factura {{ f.numero }}?</p>
              <p>Se quita de tus ingresos. Luego complétala en SRI en Línea → Facturación electrónica → Anulación de comprobantes; tu cliente debe aceptarla.</p>
            }
            <div class="mt-3 flex gap-2">
              <button type="button" class="btn btn-danger min-h-9" [disabled]="trabajando()" (click)="accion === 'borrar' ? eliminar(f) : anular(f)">{{ accion === 'borrar' ? 'Sí, eliminar' : 'Sí, anular' }}</button>
              <button type="button" class="btn btn-ghost min-h-9" (click)="confirmar.set(null)">Cancelar</button>
            </div>
          </div>
        </div>
      }

      @if (f.mensajes) {
        <div class="mt-5" [class]="f.estado === 'Devuelta' || f.estado === 'NoAutorizada' ? 'aviso-error' : 'aviso-atencion'" role="status">
          <svg lucideCircleAlert class="mt-0.5 size-5 shrink-0" aria-hidden="true"></svg>
          <div class="text-sm">
            <p class="font-medium">{{ f.estado === 'Devuelta' || f.estado === 'NoAutorizada' ? 'El SRI la rechazó:' : 'Pendiente' }}</p>
            @for (m of mensajes(); track $index) { <p>{{ m }}</p> }
            @if (f.estado === 'Devuelta' || f.estado === 'NoAutorizada') {
              <p class="mt-1 text-muted-fg">Corrige el dato y emite una factura nueva; este número queda sin uso.</p>
            }
          </div>
        </div>
      }

      <div class="mt-6 grid gap-6 lg:grid-cols-[1fr_20rem]">
        <section class="card overflow-x-auto" aria-label="Detalle">
          <table class="w-full text-sm">
            <thead class="border-b border-border text-left text-muted-fg">
              <tr>
                <th class="px-5 py-3 font-medium">Descripción</th>
                <th class="px-5 py-3 text-right font-medium">Cant.</th>
                <th class="px-5 py-3 text-right font-medium">Precio</th>
                <th class="px-5 py-3 font-medium">IVA</th>
                <th class="px-5 py-3 text-right font-medium">Subtotal</th>
              </tr>
            </thead>
            <tbody>
              @for (d of f.detalles ?? []; track $index) {
                <tr class="border-b border-border last:border-0">
                  <td class="px-5 py-3">{{ d.descripcion }} @if (d.codigo) { <span class="font-mono text-xs text-muted-fg">{{ d.codigo }}</span> }</td>
                  <td class="money px-5 py-3 text-right">{{ d.cantidad }}</td>
                  <td class="money px-5 py-3 text-right">{{ d.precioUnitario | dinero }}</td>
                  <td class="px-5 py-3 whitespace-nowrap text-muted-fg">{{ tarifa(d.tarifa) }}</td>
                  <td class="money px-5 py-3 text-right font-medium">{{ d.subtotal | dinero }}</td>
                </tr>
              }
            </tbody>
          </table>
        </section>

        <aside class="space-y-4">
          <section class="card p-5 text-sm">
            <h2 class="font-semibold">Cliente</h2>
            <p class="mt-2 font-medium">{{ f.razonSocialComprador }}</p>
            <p class="font-mono text-muted-fg">{{ f.identificacionComprador }}</p>
            @if (f.correoComprador) { <p class="text-muted-fg">{{ f.correoComprador }}</p> }
          </section>
          <section class="card p-5 text-sm">
            <dl class="space-y-2">
              <div class="flex justify-between"><dt class="text-muted-fg">Subtotal</dt><dd class="money">{{ f.subtotal | dinero }}</dd></div>
              <div class="flex justify-between"><dt class="text-muted-fg">IVA</dt><dd class="money">{{ f.iva | dinero }}</dd></div>
              <div class="flex justify-between border-t border-border pt-2 text-base font-semibold"><dt>Total</dt><dd class="money">{{ f.total | dinero }}</dd></div>
              <div class="pt-2 text-muted-fg">{{ formaPago(f.formaPago) }}</div>
            </dl>
          </section>
          <section class="card p-5 text-sm">
            <h2 class="font-semibold">Clave de acceso</h2>
            <p class="mt-2 font-mono text-xs break-all text-muted-fg">{{ f.claveAcceso }}</p>
          </section>
        </aside>
      </div>
    } @else if (carga().cargando) {
      <div class="card mt-6 h-48 animate-pulse"></div>
    }
    <app-error-carga [mensaje]="carga().error" />
  `,
})
export class FacturaDetalle {
  /** Parámetro de la ruta. */
  readonly id = input.required<string>();

  private readonly servicio = inject(FacturacionService);
  private readonly avisos = inject(AvisosService);
  private readonly router = inject(Router);

  protected readonly carga = cargar(() => Number(this.id()), (id) => this.servicio.factura(id));
  private readonly actualizada = signal<FacturaEmitida | null>(null);
  protected readonly factura = computed(() => this.actualizada() ?? this.carga().datos);
  protected readonly reintentando = signal(false);
  protected readonly puedeReintentar = computed(() => ['Firmada', 'Recibida', 'EnProceso'].includes(this.factura()?.estado ?? ''));
  protected readonly puedeBorrar = computed(() => ['Firmada', 'Devuelta', 'NoAutorizada'].includes(this.factura()?.estado ?? ''));
  protected readonly confirmar = signal<'borrar' | 'anular' | null>(null);
  protected readonly trabajando = signal(false);
  protected readonly mensajes = computed(() => (this.factura()?.mensajes ?? '').split(' | ').filter(Boolean));

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
        this.actualizada.set(r);
        if (r.estado === 'Autorizada') this.avisos.exito(`Factura ${r.numero} autorizada por el SRI.`);
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
        this.actualizada.set(r);
        this.avisos.exito(`Factura ${f.numero} anulada en Nudo. Recuerda completarla en SRI en Línea.`);
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
