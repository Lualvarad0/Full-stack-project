import { Injectable, computed, inject } from '@angular/core';
import { forkJoin, map, of } from 'rxjs';
import {
  categoria,
  ComprobanteLista,
  VariacionMensual,
  ComprobantesService,
  PeriodoService,
} from '@core/comprobantes';
import { Barra, PuntoComparado, PuntoMes } from '@core/ui';
import { AuthService } from '@core/auth';
import { CorreoService } from '@core/correo';
import { PerfilService, calcularTope, rebajaEstimada } from '@core/perfil';
import { capitalizar, nombreMes } from '@core/utils';
import { cargar } from '@core/http';
@Injectable()
export class ResumenEstado {
  private readonly comprobantes = inject(ComprobantesService);
  private readonly correo = inject(CorreoService);
  public readonly periodo = inject(PeriodoService);
  public readonly perfil = inject(PerfilService);
  private readonly auth = inject(AuthService);
  public readonly esPersonal = computed(
    () => this.auth.usuario()?.plan === 'Personal' && this.auth.usuario()?.rol !== 'admin',
  );
  /** Ingresos, clientes e IVA cobrado: necesitan RUC y el plan Profesional. */
  public readonly verIngresos = computed(() => this.perfil.tieneRuc() && this.auth.esProfesional());

  public readonly carga = cargar(
    () => ({ anio: this.periodo.anio(), mes: this.periodo.mes(), conIngresos: this.verIngresos() }),
    ({ anio, mes, conIngresos }) =>
      forkJoin({
        mes: this.comprobantes.resumen(anio, mes),
        anual: this.comprobantes.resumen(anio),
        // Últimos movimientos del mes: gastos e ingresos mezclados por fecha.
        recientes: forkJoin([
          this.comprobantes.listar({ direccion: 'Recibido', anio, mes }),
          conIngresos
            ? this.comprobantes.listar({ direccion: 'Emitido', anio, mes })
            : of([] as ComprobanteLista[]),
        ]).pipe(
          map(([g, i]) =>
            [...g, ...i]
              .sort((a, b) => b.fechaEmision.localeCompare(a.fechaEmision) || b.id - a.id)
              .slice(0, 6),
          ),
        ),
      }),
  );
  public readonly correoEstado = cargar(
    () => 0,
    () => this.correo.buzon(),
  );

  public readonly mes = computed(() => this.carga().datos?.mes ?? null);
  public readonly anual = computed(() => this.carga().datos?.anual ?? null);
  public readonly recientes = computed(() => this.carga().datos?.recientes ?? []);
  public readonly sinDatos = computed(() => {
    const anual = this.anual();
    return !!anual && anual.comprobantes === 0 && anual.comprobantesEmitidos === 0;
  });
  public readonly nombreMes = computed(() => nombreMes(this.periodo.mes()));
  public readonly balance = computed(() => {
    const m = this.mes();
    return m ? m.ingresos - m.total : 0;
  });

  /** Variación contra el mes anterior del mismo año (null en enero o si el mes anterior está vacío). */
  public readonly variacionIngresos = computed(() => this.variacion((p) => p.ingresos));
  public readonly variacionGastos = computed(() => this.variacion((p) => p.total));

  public readonly porCategoria = computed<Barra[]>(() =>
    (this.mes()?.porCategoria ?? [])
      .filter((c) => c.total !== 0)
      .map((c) => ({
        etiqueta: categoria(c.categoria).nombre,
        valor: c.total,
        color: categoria(c.categoria).color,
        detalle: `${c.comprobantes} ${c.comprobantes === 1 ? 'comprobante' : 'comprobantes'}`,
      })),
  );

  public readonly comparado = computed<PuntoComparado[]>(() =>
    (this.anual()?.porMes ?? []).map((m) => ({
      mes: m.mes,
      etiqueta: capitalizar(nombreMes(m.mes)),
      a: m.ingresos,
      b: m.total,
    })),
  );

  public readonly porMes = computed<PuntoMes[]>(() =>
    (this.anual()?.porMes ?? []).map((m) => ({
      mes: m.mes,
      etiqueta: capitalizar(nombreMes(m.mes)),
      valor: m.total,
      detalle: `${m.comprobantes} comprobantes`,
    })),
  );

  /** Tope de gastos personales del año y avance del gasto deducible acumulado. */
  public readonly tope = computed(() => {
    const anual = this.anual();
    const perfil = this.perfil.perfil();
    const tope = calcularTope(
      this.periodo.anio(),
      perfil?.cargasFamiliares ?? 0,
      perfil?.enfermedadCatastrofica ?? false,
    );
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

  public readonly cargasFamiliares = computed(() => this.perfil.perfil()?.cargasFamiliares ?? 0);

  public esIngreso(c: ComprobanteLista): boolean {
    return c.direccion === 'Emitido';
  }

  public hace(iso: string | null): string {
    if (!iso) return 'sin correos aún';
    const minutos = Math.round((Date.now() - new Date(iso).getTime()) / 60000);
    if (minutos < 60) return `último correo hace ${Math.max(minutos, 1)} min`;
    const horas = Math.round(minutos / 60);
    return horas < 48
      ? `último correo hace ${horas} h`
      : `último correo hace ${Math.round(horas / 24)} días`;
  }

  private variacion(
    valor: (p: { ingresos: number; total: number }) => number,
  ): VariacionMensual | null {
    const mes = this.periodo.mes();
    const porMes = this.anual()?.porMes;
    if (!porMes || mes === 1) return null;
    const puntoActual = porMes.find((p) => p.mes === mes);
    const puntoAnterior = porMes.find((p) => p.mes === mes - 1);
    if (!puntoActual || !puntoAnterior) return null;
    const actual = valor(puntoActual);
    const anterior = valor(puntoAnterior);
    if (anterior <= 0) return null;
    const porcentaje = ((actual - anterior) / anterior) * 100;
    return {
      porcentaje: Math.abs(porcentaje),
      sube: porcentaje >= 0,
      mesAnterior: nombreMes(mes - 1),
    };
  }
}
