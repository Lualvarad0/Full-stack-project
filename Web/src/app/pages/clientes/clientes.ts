import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Titulo } from '../../shared/marca';
import { RouterLink } from '@angular/router';
import { LucideCircleAlert, LucideIdCard, LucideSearch } from '@lucide/angular';
import { cargar } from '../../core/carga';
import { ComprobantesService } from '../../core/comprobantes.service';
import { PeriodoService } from '../../core/estado';
import { PerfilService } from '../../core/perfil.service';
import { DineroPipe, FechaCortaPipe } from '../../shared/formato';

/** Clientes a los que el usuario factura, a partir de sus facturas emitidas. */
@Component({
  selector: 'app-clientes',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Titulo, RouterLink, DineroPipe, FechaCortaPipe, LucideSearch, LucideCircleAlert, LucideIdCard],
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
        <div class="relative min-w-0 flex-1 basis-64">
          <svg lucideSearch class="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-fg" aria-hidden="true"></svg>
          <input type="search" class="input pl-9" placeholder="Buscar por nombre o identificación" aria-label="Buscar cliente"
            [value]="busqueda()" (input)="busqueda.set($any($event.target).value)" />
        </div>
        <div class="inline-flex rounded-lg bg-muted p-1 text-sm" role="radiogroup" aria-label="Período">
          <button type="button" role="radio" class="min-h-8 rounded-md px-3 font-medium transition-colors"
            [class]="!soloAnio() ? 'bg-card text-fg shadow-sm' : 'text-muted-fg hover:text-fg'"
            [attr.aria-checked]="!soloAnio()" (click)="soloAnio.set(false)">Histórico</button>
          <button type="button" role="radio" class="min-h-8 rounded-md px-3 font-medium transition-colors"
            [class]="soloAnio() ? 'bg-card text-fg shadow-sm' : 'text-muted-fg hover:text-fg'"
            [attr.aria-checked]="soloAnio()" (click)="soloAnio.set(true)">{{ periodo.anio() }}</button>
        </div>
      </div>

      @if (carga().error) {
        <div class="aviso-error mt-6" role="alert">
          <svg lucideCircleAlert class="mt-0.5 size-5 shrink-0 text-danger" aria-hidden="true"></svg>
          <p>{{ carga().error }}</p>
        </div>
      }

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
