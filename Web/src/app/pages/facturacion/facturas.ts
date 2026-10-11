import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { LucideFilePlus2, LucideFileText, LucideTriangleAlert } from '@lucide/angular';
import { FacturacionService } from '@core/services';
import { cargar } from '@core/utils';
import { ErrorCarga, Titulo } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';
import { EstadoFacturaPildora } from './estado-factura';

/** Facturas emitidas desde Nudo y acceso a crear una nueva. */
@Component({
  selector: 'app-facturas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Titulo, ErrorCarga, DineroPipe, FechaCortaPipe, EstadoFacturaPildora, LucideFilePlus2, LucideFileText, LucideTriangleAlert],
  template: `
    <app-titulo linea1="Tus" linea2="facturas" subtitulo="Emite facturas electrónicas firmadas con tu firma y autorizadas por el SRI.">
      <a routerLink="/facturacion/servicios" class="btn btn-secondary">Mis servicios</a>
      <a routerLink="/facturacion/nueva" class="btn btn-primary">
        <svg lucideFilePlus2 class="size-4" aria-hidden="true"></svg> Nueva factura
      </a>
    </app-titulo>

    @if (carga().datos; as d) {
      @if (d.datos.pendientes.length) {
        <div class="aviso-atencion mt-6">
          <svg lucideTriangleAlert class="mt-0.5 size-5 shrink-0 text-warning" aria-hidden="true"></svg>
          <div>
            <p class="font-medium">Antes de facturar completa: {{ d.datos.pendientes.join(', ') }}.</p>
            <div class="mt-1 flex flex-wrap gap-4">
              @if (d.datos.pendientes.includes('el plan Profesional')) {
                <a routerLink="/suscripcion" [queryParams]="{ plan: 'Profesional' }" class="font-medium text-accent hover:underline">Activar plan Profesional</a>
              }
              <a routerLink="/configuracion" fragment="facturacion" class="font-medium text-accent hover:underline">Ir a Configuración</a>
            </div>
          </div>
        </div>
      } @else if (d.datos.ambiente === 'Pruebas') {
        <p class="aviso-atencion mt-6">
          <svg lucideTriangleAlert class="mt-0.5 size-5 shrink-0 text-warning" aria-hidden="true"></svg>
          Estás en el ambiente de pruebas del SRI: las facturas se validan pero no tienen validez tributaria.
          <a routerLink="/configuracion" fragment="facturacion" class="ml-1 font-medium text-accent hover:underline">Cambiar a producción</a>
        </p>
      }

      @if (d.facturas.length) {
        <div class="card mt-6 overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="border-b border-border text-left text-muted-fg">
              <tr>
                <th class="px-5 py-3 font-medium">Número</th>
                <th class="px-5 py-3 font-medium">Fecha</th>
                <th class="px-5 py-3 font-medium">Cliente</th>
                <th class="px-5 py-3 text-right font-medium">Total</th>
                <th class="px-5 py-3 font-medium">Estado</th>
              </tr>
            </thead>
            <tbody>
              @for (f of d.facturas; track f.id) {
                <tr class="border-b border-border last:border-0 hover:bg-muted/50">
                  <td class="px-5 py-3">
                    <a [routerLink]="['/facturacion', f.id]" class="font-mono font-medium hover:text-accent hover:underline">{{ f.numero }}</a>
                    @if (f.ambiente === 'Pruebas') {
                      <span class="ml-2 rounded-full bg-muted px-2 py-0.5 text-[11px] text-muted-fg">Pruebas</span>
                    }
                  </td>
                  <td class="px-5 py-3 whitespace-nowrap text-muted-fg">{{ f.fechaEmision | fechaCorta }}</td>
                  <td class="px-5 py-3">{{ f.razonSocialComprador }}</td>
                  <td class="money px-5 py-3 text-right font-medium">{{ f.total | dinero }}</td>
                  <td class="px-5 py-3"><app-estado-factura [estado]="f.estado" /></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      } @else {
        <div class="card mt-6 flex flex-col items-center px-6 py-14 text-center">
          <span class="grid size-12 place-items-center rounded-full bg-primary-soft text-primary">
            <svg lucideFileText class="size-6" aria-hidden="true"></svg>
          </span>
          <p class="mt-4 font-medium">Aún no emites facturas desde Nudo.</p>
          <p class="mt-1 max-w-md text-sm text-muted-fg">Crea la primera: la firmamos con tu firma electrónica, la enviamos al SRI y te mostramos su autorización.</p>
          <a routerLink="/facturacion/nueva" class="btn btn-primary mt-5">Crear mi primera factura</a>
        </div>
      }
    } @else if (carga().cargando) {
      <div class="card mt-6 h-48 animate-pulse"></div>
    }
    <app-error-carga [mensaje]="carga().error" />
  `,
})
export class Facturas {
  private readonly servicio = inject(FacturacionService);
  protected readonly carga = cargar(() => 0, () => forkJoin({ facturas: this.servicio.facturas(), datos: this.servicio.datos() }));
}
