import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { Monto, Titulo } from '../../shared/marca';
import { Router, RouterLink } from '@angular/router';
import { LucideCircleAlert, LucideFileUp, LucideIdCard, LucideSearch, LucideX } from '@lucide/angular';
import { cargar } from '../../core/carga';
import { ComprobantesService } from '../../core/comprobantes.service';
import { PeriodoService } from '../../core/estado';
import { ComprobanteLista } from '../../core/models';
import { PerfilService } from '../../core/perfil.service';
import { DineroPipe, FechaCortaPipe } from '../../shared/formato';

/** Facturas emitidas por el usuario (ingresos) en el mes seleccionado. */
@Component({
  selector: 'app-ingresos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Titulo, Monto, RouterLink, DineroPipe, FechaCortaPipe, LucideSearch, LucideFileUp, LucideX, LucideCircleAlert, LucideIdCard],
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

  protected readonly carga = cargar(
    () => ({
      anio: this.periodo.anio(),
      mes: this.periodo.mes(),
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
  protected readonly hayFiltros = computed(() => !!(this.cliente() || this.busquedaAplicada()));

  protected buscar(texto: string): void {
    this.busqueda.set(texto);
    clearTimeout(this.temporizador);
    this.temporizador = setTimeout(() => this.busquedaAplicada.set(texto.trim()), 300);
  }

  protected limpiarFiltros(): void {
    this.busqueda.set('');
    this.busquedaAplicada.set('');
    void this.router.navigate([], { queryParams: {} });
  }

  protected esNotaCredito(c: ComprobanteLista): boolean {
    return signo(c) < 0;
  }
}

function signo(c: ComprobanteLista): number {
  return c.tipoComprobante === 'Nota de crédito' ? -1 : 1;
}
