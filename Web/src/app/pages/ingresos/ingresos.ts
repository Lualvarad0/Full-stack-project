import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { LucideFileUp, LucideIdCard, LucideX } from '@lucide/angular';
import { FILTRO_VACIO } from '@core/constants';
import { ComprobanteLista } from '@core/models';
import { ComprobantesService, PerfilService, PeriodoService } from '@core/services';
import { cargar, etiquetaRango, hayFiltroRango, parametrosFiltro } from '@core/utils';
import { Buscador, ErrorCarga, Filtros, Monto, Titulo } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';

/** Facturas emitidas por el usuario (ingresos) en el mes seleccionado. */
@Component({
  selector: 'app-ingresos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Buscador, ErrorCarga, Titulo, Filtros, Monto, RouterLink, DineroPipe, FechaCortaPipe, LucideFileUp, LucideX, LucideIdCard],
  templateUrl: './ingresos.html',
})
export class Ingresos {
  /** ?cliente=<identificación> */
  readonly cliente = input<string | undefined>();

  private readonly comprobantes = inject(ComprobantesService);
  private readonly router = inject(Router);
  protected readonly periodo = inject(PeriodoService);
  protected readonly perfil = inject(PerfilService);

  protected readonly busqueda = signal('');
  private readonly busquedaAplicada = signal('');
  private temporizador?: ReturnType<typeof setTimeout>;

  /** Rango de fechas y montos (si hay rango, reemplaza al mes de la barra superior). */
  protected readonly filtroRango = signal(FILTRO_VACIO);
  protected readonly etiquetaPeriodo = computed(() => etiquetaRango(this.filtroRango()) ?? this.periodo.etiqueta());

  protected readonly carga = cargar(
    () => ({
      ...parametrosFiltro(this.filtroRango(), this.periodo.anio(), this.periodo.mes()),
      cliente: this.cliente(),
      q: this.busquedaAplicada() || undefined,
      ruc: this.perfil.perfil()?.ruc,
    }),
    ({ ruc, ...filtro }) => this.comprobantes.listar({ direccion: 'Emitido', ...filtro }),
  );

  protected readonly lista = computed(() => this.carga().datos ?? []);
  protected readonly total = computed(() => this.lista().reduce((s, c) => s + signo(c) * c.total, 0));
  protected readonly iva = computed(() => this.lista().reduce((s, c) => s + signo(c) * c.iva, 0));
  protected readonly clienteFiltrado = computed(() => {
    const id = this.cliente();
    return id ? (this.lista().find((c) => c.identificacionComprador === id)?.razonSocialComprador ?? id) : null;
  });
  protected readonly hayFiltros = computed(() => !!(this.cliente() || this.busquedaAplicada()) || hayFiltroRango(this.filtroRango()));

  protected buscar(texto: string): void {
    this.busqueda.set(texto);
    clearTimeout(this.temporizador);
    this.temporizador = setTimeout(() => this.busquedaAplicada.set(texto.trim()), 300);
  }

  protected limpiarFiltros(): void {
    this.busqueda.set('');
    this.busquedaAplicada.set('');
    this.filtroRango.set(FILTRO_VACIO);
    void this.router.navigate([], { queryParams: {} });
  }

  protected esNotaCredito(c: ComprobanteLista): boolean {
    return signo(c) < 0;
  }
}

function signo(c: ComprobanteLista): number {
  return c.tipoComprobante === 'Nota de crédito' ? -1 : 1;
}
