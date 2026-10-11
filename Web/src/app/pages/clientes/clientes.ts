import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideIdCard } from '@lucide/angular';
import { ComprobantesService, PerfilService, PeriodoService } from '@core/services';
import { cargar } from '@core/utils';
import { Buscador, ErrorCarga, Segmentado, Titulo } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';

/** Clientes a los que el usuario factura, a partir de sus facturas emitidas. */
@Component({
  selector: 'app-clientes',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Buscador, ErrorCarga, Segmentado, Titulo, RouterLink, DineroPipe, FechaCortaPipe, LucideIdCard],
  template: `
    <app-titulo linea1="Tus" linea2="clientes" subtitulo="Las personas y empresas a las que facturas, según tus facturas emitidas." />

    @if (!perfil.tieneRuc()) {
      <section class="card mt-6 p-6 sm:p-10">
        <svg lucideIdCard class="size-8 text-accent" aria-hidden="true"></svg>
        <h2 class="mt-3 text-lg font-semibold">Registra tu RUC para ver tus clientes</h2>
        <p class="mt-2 max-w-2xl text-sm text-muted-fg">Los clientes salen de las facturas que emites con tu RUC.</p>
        <a routerLink="/configuracion" class="btn btn-primary mt-6">Completar mi perfil</a>
      </section>
    } @else {
      <div class="mt-6 flex flex-wrap items-center gap-3">
        <app-buscador placeholder="Buscar por nombre o identificación" [valor]="busqueda()" (valorChange)="busqueda.set($event)" />
        <app-segmentado [opciones]="periodos()" [(valor)]="soloAnio" etiqueta="Período" />
      </div>

      <app-error-carga [mensaje]="carga().error" />

      <section class="card mt-4 overflow-hidden" [attr.aria-busy]="carga().cargando">
        @if (carga().cargando && !carga().datos) {
          <div class="space-y-3 p-5">
            @for (i of [1, 2, 3]; track i) { <div class="h-12 animate-pulse rounded-lg bg-muted"></div> }
          </div>
        } @else if (filtrados().length === 0) {
          <p class="px-6 py-14 text-center text-sm text-muted-fg">
            {{ (carga().datos?.length ?? 0) === 0 ? 'Aún no hay facturas emitidas. Importa sus XML o claves de acceso.' : 'Ningún cliente coincide.' }}
          </p>
        } @else {
          <ul class="divide-y divide-border">
            @for (c of filtrados(); track c.identificacion) {
              <li>
                <a routerLink="/ingresos" [queryParams]="{ cliente: c.identificacion }" class="grid gap-1 px-4 py-4 transition-colors hover:bg-muted/50 sm:grid-cols-[1fr_auto_auto] sm:items-center sm:gap-6 sm:px-5">
                  <span class="min-w-0">
                    <span class="block truncate font-medium">{{ c.razonSocial }}</span>
                    <span class="text-xs text-muted-fg">{{ c.identificacion }} · última factura {{ c.ultimaFactura | fechaCorta }}</span>
                  </span>
                  <span class="text-sm text-muted-fg">{{ c.comprobantes }} {{ c.comprobantes === 1 ? 'factura' : 'facturas' }}</span>
                  <span class="money text-sm font-semibold sm:text-right">{{ c.total | dinero }}</span>
                </a>
              </li>
            }
          </ul>
        }
      </section>
    }
  `,
})
export class Clientes {
  private readonly comprobantes = inject(ComprobantesService);
  protected readonly periodo = inject(PeriodoService);
  protected readonly perfil = inject(PerfilService);

  protected readonly busqueda = signal('');
  protected readonly soloAnio = signal(false);
  protected readonly periodos = computed(() => [
    { valor: false, etiqueta: 'Histórico' },
    { valor: true, etiqueta: String(this.periodo.anio()) },
  ]);

  protected readonly carga = cargar(
    () => ({ anio: this.soloAnio() ? this.periodo.anio() : undefined, ruc: this.perfil.perfil()?.ruc }),
    ({ anio }) => this.comprobantes.clientes(anio),
  );

  protected readonly filtrados = computed(() => {
    const texto = this.busqueda().trim().toLowerCase();
    return (this.carga().datos ?? []).filter((c) =>
      !texto || c.razonSocial.toLowerCase().includes(texto) || c.identificacion.includes(texto));
  });
}
