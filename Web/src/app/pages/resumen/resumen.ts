import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Monto, Titulo } from '../../shared/marca';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import {
  LucideCircleAlert,
  LucideCircleCheck,
  LucideFileUp,
  LucideHandCoins,
  LucideIdCard,
  LucideInfo,
  LucideTriangleAlert,
} from '@lucide/angular';
import { cargar } from '../../core/carga';
import { categoria } from '../../core/categorias';
import { ComprobantesService } from '../../core/comprobantes.service';
import { PeriodoService, capitalizar, nombreMes } from '../../core/estado';
import { calcularTope, rebajaEstimada } from '../../core/gastos-personales';
import { PerfilService } from '../../core/perfil.service';
import { DineroPipe } from '../../shared/formato';
import { Barra, BarrasHorizontales, BarrasMes, PuntoMes } from '../../shared/graficos';

@Component({
  selector: 'app-resumen',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Titulo, Monto, 
    RouterLink, DecimalPipe, DineroPipe, BarrasHorizontales, BarrasMes,
    LucideFileUp, LucideHandCoins, LucideTriangleAlert, LucideCircleCheck, LucideCircleAlert, LucideInfo, LucideIdCard,
  ],
  templateUrl: './resumen.html',
})
export class Resumen {
  private readonly comprobantes = inject(ComprobantesService);
  protected readonly periodo = inject(PeriodoService);
  protected readonly perfil = inject(PerfilService);

  protected readonly carga = cargar(
    () => ({ anio: this.periodo.anio(), mes: this.periodo.mes(), ruc: this.perfil.perfil()?.ruc }),
    ({ anio, mes }) => forkJoin({ mes: this.comprobantes.resumen(anio, mes), anual: this.comprobantes.resumen(anio) }),
  );

  protected readonly mes = computed(() => this.carga().datos?.mes ?? null);
  protected readonly anual = computed(() => this.carga().datos?.anual ?? null);
  protected readonly sinDatos = computed(() => {
    const anual = this.anual();
    return !!anual && anual.comprobantes === 0 && anual.comprobantesEmitidos === 0;
  });
  protected readonly nombreMes = computed(() => nombreMes(this.periodo.mes()));

  /** Serie del gráfico mensual: una a la vez (nunca dos escalas en el mismo eje). */
  protected readonly serieMensual = signal<'gastos' | 'ingresos'>('gastos');

  protected readonly porCategoria = computed<Barra[]>(() =>
    (this.mes()?.porCategoria ?? [])
      .filter((c) => c.total !== 0)
      .map((c) => ({
        etiqueta: categoria(c.categoria).nombre,
        valor: c.total,
        color: categoria(c.categoria).color,
        detalle: `${c.comprobantes} ${c.comprobantes === 1 ? 'comprobante' : 'comprobantes'}`,
      })),
  );

  protected readonly porMes = computed<PuntoMes[]>(() => {
    const ingresos = this.serieMensual() === 'ingresos';
    return (this.anual()?.porMes ?? []).map((m) => ({
      mes: m.mes,
      etiqueta: capitalizar(nombreMes(m.mes)),
      valor: ingresos ? m.ingresos : m.total,
      detalle: ingresos ? `IVA cobrado ${formatear(m.ivaCobrado)}` : `${m.comprobantes} comprobantes`,
    }));
  });

  protected readonly totalAnualSerie = computed(() =>
    this.serieMensual() === 'ingresos' ? this.anual()?.ingresos ?? 0 : this.anual()?.total ?? 0);

  /** Tope de gastos personales del año y avance del gasto deducible acumulado. */
  protected readonly tope = computed(() => {
    const anual = this.anual();
    const perfil = this.perfil.perfil();
    const tope = calcularTope(this.periodo.anio(), perfil?.cargasFamiliares ?? 0, perfil?.enfermedadCatastrofica ?? false);
    if (!anual || !tope) return null;
    const deducible = Math.max(anual.totalDeducible, 0);
    const porcentaje = tope.tope > 0 ? (deducible / tope.tope) * 100 : 0;
    return {
      ...tope,
      deducible,
      porcentaje,
      restante: Math.max(tope.tope - deducible, 0),
      rebaja: rebajaEstimada(deducible, tope),
      estado: porcentaje >= 100 ? 'alcanzado' : porcentaje >= 80 ? 'cerca' : 'normal',
    } as const;
  });

  protected readonly cargasFamiliares = computed(() => this.perfil.perfil()?.cargasFamiliares ?? 0);
}

const MONEDA = new Intl.NumberFormat('es-EC', { style: 'currency', currency: 'USD' });
function formatear(valor: number): string {
  return MONEDA.format(valor);
}
