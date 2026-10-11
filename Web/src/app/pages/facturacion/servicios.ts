import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideArrowLeft, LucideLoader, LucidePencil, LucidePlus, LucideTrash2 } from '@lucide/angular';
import { TARIFAS_IVA } from '@core/constants';
import { ServicioBorrador, ServicioFacturable, TarifaIva } from '@core/models';
import { AvisosService, FacturacionService } from '@core/services';
import { cargar, mensajeDeError } from '@core/utils';
import { ErrorCarga, MensajeError, Titulo } from '@shared/components';
import { DineroPipe } from '@shared/pipes';

const VACIO: ServicioBorrador = { codigo: '', descripcion: '', precioUnitario: 0, tarifa: 'Iva15' };

/** Catálogo de productos y servicios que el usuario factura a menudo (se eligen al crear una factura). */
@Component({
  selector: 'app-servicios',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensajeError, RouterLink, Titulo, ErrorCarga, DineroPipe, LucideArrowLeft, LucidePlus, LucidePencil, LucideTrash2, LucideLoader],
  template: `
    <a routerLink="/facturacion" class="btn btn-ghost -ml-3">
      <svg lucideArrowLeft class="size-4" aria-hidden="true"></svg> Volver a facturas
    </a>
    <app-titulo class="mt-2 block" linea1="Mis" linea2="servicios" subtitulo="Guarda lo que facturas a menudo y agrégalo a una factura con un clic." />

    <div class="mt-6 grid gap-6 lg:grid-cols-[1fr_22rem]">
      <section class="card overflow-x-auto" aria-label="Servicios guardados">
        @if (carga().datos; as lista) {
          <table class="w-full text-sm">
            <thead class="border-b border-border text-left text-muted-fg">
              <tr>
                <th class="px-5 py-3 font-medium">Descripción</th>
                <th class="px-5 py-3 text-right font-medium">Precio</th>
                <th class="px-5 py-3 font-medium">IVA</th>
                <th class="px-5 py-3"><span class="sr-only">Acciones</span></th>
              </tr>
            </thead>
            <tbody>
              @for (s of lista; track s.id) {
                <tr class="border-b border-border last:border-0">
                  <td class="px-5 py-3">{{ s.descripcion }} @if (s.codigo) { <span class="font-mono text-xs text-muted-fg">{{ s.codigo }}</span> }</td>
                  <td class="money px-5 py-3 text-right font-medium">{{ s.precioUnitario | dinero }}</td>
                  <td class="px-5 py-3 whitespace-nowrap text-muted-fg">{{ tarifa(s.tarifa) }}</td>
                  <td class="px-5 py-3">
                    <div class="flex justify-end gap-1">
                      <button type="button" class="btn btn-ghost min-h-8 px-2" [attr.aria-label]="'Editar ' + s.descripcion" (click)="editar(s)">
                        <svg lucidePencil class="size-4" aria-hidden="true"></svg>
                      </button>
                      <button type="button" class="btn btn-ghost min-h-8 px-2 text-danger" [attr.aria-label]="'Eliminar ' + s.descripcion" (click)="eliminar(s)">
                        <svg lucideTrash2 class="size-4" aria-hidden="true"></svg>
                      </button>
                    </div>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="4" class="px-5 py-12 text-center text-muted-fg">Aún no guardas servicios. Crea el primero con el formulario.</td></tr>
              }
            </tbody>
          </table>
        } @else if (carga().cargando) {
          <div class="h-40 animate-pulse"></div>
        }
        <app-error-carga [mensaje]="carga().error" />
      </section>

      <form class="card h-fit p-5" (submit)="$event.preventDefault(); guardar()" novalidate aria-labelledby="titulo-form-servicio">
        <h2 id="titulo-form-servicio" class="font-semibold">{{ borrador().id ? 'Editar servicio' : 'Nuevo servicio' }}</h2>
        <div class="mt-4 space-y-4">
          <div>
            <label class="field-label" for="srv-desc">Descripción</label>
            <input id="srv-desc" class="input" maxlength="300" [value]="borrador().descripcion" (input)="cambiar({ descripcion: $any($event.target).value })" />
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="field-label" for="srv-precio">Precio</label>
              <input id="srv-precio" type="number" min="0" step="0.01" class="input" [value]="borrador().precioUnitario" (input)="cambiar({ precioUnitario: +$any($event.target).value || 0 })" />
            </div>
            <div>
              <label class="field-label" for="srv-codigo">Código <span class="font-normal text-muted-fg">(opcional)</span></label>
              <input id="srv-codigo" class="input font-mono" maxlength="25" [value]="borrador().codigo" (input)="cambiar({ codigo: $any($event.target).value })" />
            </div>
          </div>
          <div>
            <label class="field-label" for="srv-iva">IVA</label>
            <select id="srv-iva" class="input" (change)="cambiar({ tarifa: $any($event.target).value })">
              @for (t of tarifas; track t.valor) { <option [value]="t.valor" [selected]="t.valor === borrador().tarifa">{{ t.etiqueta }}</option> }
            </select>
          </div>
        </div>
        <app-mensaje-error class="mt-4 block" [mensaje]="error()" />
        <div class="mt-5 flex gap-2">
          <button type="submit" class="btn btn-primary flex-1" [disabled]="guardando()">
            @if (guardando()) { <svg lucideLoader class="size-4 animate-spin" aria-hidden="true"></svg> } @else { <svg lucidePlus class="size-4" aria-hidden="true"></svg> }
            {{ borrador().id ? 'Guardar cambios' : 'Agregar servicio' }}
          </button>
          @if (borrador().id) {
            <button type="button" class="btn btn-ghost" (click)="borrador.set(vacio)">Cancelar</button>
          }
        </div>
      </form>
    </div>
  `,
})
export class Servicios {
  private readonly servicio = inject(FacturacionService);
  private readonly avisos = inject(AvisosService);

  protected readonly tarifas = TARIFAS_IVA;
  protected readonly vacio = VACIO;
  private readonly recargas = signal(0);
  protected readonly carga = cargar(() => this.recargas(), () => this.servicio.servicios());
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
    this.borrador.set({ id: s.id, codigo: s.codigo ?? '', descripcion: s.descripcion, precioUnitario: s.precioUnitario, tarifa: s.tarifa });
  }

  protected guardar(): void {
    const b = this.borrador();
    if (b.descripcion.trim().length < 2) {
      this.error.set('Escribe la descripción del servicio.');
      return;
    }
    this.error.set(null);
    this.guardando.set(true);
    this.servicio.guardarServicio({ codigo: b.codigo.trim() || null, descripcion: b.descripcion.trim(), precioUnitario: b.precioUnitario, tarifa: b.tarifa }, b.id).subscribe({
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
