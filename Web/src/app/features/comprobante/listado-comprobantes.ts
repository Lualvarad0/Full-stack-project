import { computed, Directive, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  FILTRO_VACIO,
  ComprobanteLista,
  Direccion,
  FiltroComprobantes,
  ComprobantesService,
  PeriodoService,
  etiquetaRango,
  hayFiltroRango,
  parametrosFiltro,
} from '@core/comprobantes';
import { cargar } from '@core/http';
import { crearBusqueda } from '@core/utils/busqueda';
/** Patrón plantilla: cada listado especializa sus filtros y selección, sin duplicar la carga. */
@Directive()
export abstract class ListadoComprobantes {
  protected readonly comprobantes = inject(ComprobantesService);
  protected readonly router = inject(Router);
  protected readonly periodo = inject(PeriodoService);
  private readonly buscador = crearBusqueda();
  protected readonly busqueda = this.buscador.valor;
  protected readonly recargas = signal(0);
  protected readonly filtroRango = signal(FILTRO_VACIO);
  protected readonly etiquetaPeriodo = computed(
    () => etiquetaRango(this.filtroRango()) ?? this.periodo.etiqueta(),
  );

  protected abstract readonly direccion: Direccion;
  protected abstract filtrosAdicionales(): FiltroComprobantes;
  protected abstract filtrosActivos(): boolean;

  protected seleccionar(datos: ComprobanteLista[]): ComprobanteLista[] {
    return datos;
  }

  protected readonly carga = cargar(
    () => ({
      ...parametrosFiltro(this.filtroRango(), this.periodo.anio(), this.periodo.mes()),
      ...this.filtrosAdicionales(),
      direccion: this.direccion,
      q: this.buscador.aplicada() || undefined,
      recarga: this.recargas(),
    }),
    ({ recarga, ...filtro }) => this.comprobantes.listar(filtro),
  );
  protected readonly lista = computed(() => this.seleccionar(this.carga().datos ?? []));
  protected readonly total = computed(() => this.sumar('total'));
  protected readonly iva = computed(() => this.sumar('iva'));
  protected readonly hayFiltros = computed(
    () => this.filtrosActivos() || !!this.buscador.aplicada() || hayFiltroRango(this.filtroRango()),
  );

  protected buscar(texto: string): void {
    this.buscador.buscar(texto);
  }

  protected limpiarFiltros(): void {
    this.buscador.limpiar();
    this.filtroRango.set(FILTRO_VACIO);
    void this.router.navigate([], { queryParams: {} });
  }

  protected esNotaCredito(c: ComprobanteLista): boolean {
    return c.tipoComprobante === 'Nota de crédito';
  }

  private sumar(campo: 'total' | 'iva'): number {
    return this.lista().reduce((suma, c) => suma + (this.esNotaCredito(c) ? -1 : 1) * c[campo], 0);
  }
}
