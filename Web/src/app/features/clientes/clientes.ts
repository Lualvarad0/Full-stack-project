import { PerfilRequerido } from '@shared/components/perfil-requerido';
import { ListaCarga } from '@shared/components/lista-carga';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ComprobantesService, PeriodoService } from '@core/comprobantes';
import { PerfilService } from '@core/perfil';
import { cargar } from '@core/http';
import { Buscador, ErrorCarga, Segmentado, Titulo } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';
/** Clientes a los que el usuario factura, a partir de sus facturas emitidas. */
@Component({
  selector: 'app-clientes',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PerfilRequerido,
    ListaCarga,
    Buscador,
    ErrorCarga,
    Segmentado,
    Titulo,
    RouterLink,
    DineroPipe,
    FechaCortaPipe,
  ],
  templateUrl: './clientes.html',
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
    () => ({
      anio: this.soloAnio() ? this.periodo.anio() : undefined,
      ruc: this.perfil.perfil()?.ruc,
    }),
    ({ anio }) => this.comprobantes.clientes(anio),
  );

  protected readonly filtrados = computed(() => {
    const texto = this.busqueda().trim().toLowerCase();
    return (this.carga().datos ?? []).filter(
      (c) =>
        !texto || c.razonSocial.toLowerCase().includes(texto) || c.identificacion.includes(texto),
    );
  });
}
