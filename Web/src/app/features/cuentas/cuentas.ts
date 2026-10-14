import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideCalendarCheck,
  LucideCheck,
  LucideCopy,
  LucideCreditCard,
  LucideLoader,
  LucidePencil,
  LucidePlus,
  LucideSparkles,
  LucideTrash2,
  LucideUndo2,
} from '@lucide/angular';
import { AuthService } from '@core/auth';
import { PeriodoService } from '@core/comprobantes';
import {
  CATEGORIAS_CUENTA,
  CategoriaCuenta,
  CierreMes,
  CuentaBorrador,
  CuentaPersonal,
  CuentasService,
  DecisionCierre,
  estadoPresupuesto,
  estaVencida,
  gastosPorCategoria,
  proporcionDelIngreso,
} from '@core/cuentas';
import { cargar, mensajeDeError } from '@core/http';
import { AvisosService } from '@core/ui';
import { fechaIso, nombreMes } from '@core/utils';
import { ErrorCarga, MensajeError, Titulo, SelectorDia, SelectorFecha } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';
import { CierreMesModal } from './cierre-mes-modal';
import { IngresoModal } from './ingreso-modal';

const VACIA: CuentaBorrador = { nombre: '', categoria: 'Vivienda', monto: 0, diaVencimiento: null };

/**
 * Mis cuentas: el usuario anota sus cuentas del mes, dice cuánto gana y marca cada una con el día en que la pagó.
 * Es la sección principal del plan gratuito (subir facturas es de Profesional).
 */
@Component({
  selector: 'app-cuentas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    SelectorDia, SelectorFecha,
    RouterLink,
    Titulo,
    ErrorCarga,
    MensajeError,
    IngresoModal,
    CierreMesModal,
    LucideCalendarCheck,
    LucideCreditCard,
    DineroPipe,
    FechaCortaPipe,
    LucidePlus,
    LucidePencil,
    LucideTrash2,
    LucideCheck,
    LucideUndo2,
    LucideCopy,
    LucideLoader,
    LucideSparkles,
  ],
  templateUrl: './cuentas.html',
})
export class Cuentas {
  private readonly api = inject(CuentasService);
  private readonly avisos = inject(AvisosService);
  protected readonly auth = inject(AuthService);
  protected readonly periodo = inject(PeriodoService);

  protected readonly categorias = CATEGORIAS_CUENTA;
  protected readonly vacia = VACIA;
  private readonly recargas = signal(0);
  protected readonly carga = cargar(
    () => ({ anio: this.periodo.anio(), mes: this.periodo.mes(), n: this.recargas() }),
    ({ anio, mes }) => this.api.mes(anio, mes),
  );
  protected readonly datos = computed(() => this.carga().datos);

  // Formulario de cuenta
  protected readonly borrador = signal<CuentaBorrador>(VACIA);
  protected readonly guardando = signal(false);
  protected readonly error = signal<string | null>(null);

  // Pago: la cuenta cuyo día de pago se está eligiendo y la fecha elegida
  protected readonly pagando = signal<number | null>(null);
  protected readonly fechaPago = signal('');
  protected readonly hoy = fechaIso(new Date());

  // Ingreso
  protected readonly modalIngreso = signal(false);
  protected readonly guardandoIngreso = signal(false);
  protected readonly errorIngreso = signal<string | null>(null);
  /** Para no volver a abrir el modal solo si el usuario dijo "Ahora no". */
  private omitioIngreso = false;

  // Cierre del mes anterior (planes de pago): cuentas que quedaron pendientes
  protected readonly cierre = signal<CierreMes | null>(null);
  protected readonly modalCierre = signal(false);
  protected readonly guardandoCierre = signal(false);
  protected readonly errorCierre = signal<string | null>(null);
  /** "Decidir después": no se vuelve a abrir solo mientras siga en la página. */
  private omitioCierre = false;

  protected readonly estado = computed(() => {
    const d = this.datos();
    return d ? estadoPresupuesto(d.total, d.ingreso) : 'sinIngreso';
  });
  protected readonly proporcion = computed(() => {
    const d = this.datos();
    return d ? proporcionDelIngreso(d.total, d.ingreso) : null;
  });
  protected readonly porCategoria = computed(() => {
    const d = this.datos();
    return d ? gastosPorCategoria(d.cuentas, d.ingreso) : [];
  });
  protected readonly pagadas = computed(
    () => this.datos()?.cuentas.filter((c) => c.pagadaEl).length ?? 0,
  );

  constructor() {
    // La primera vez (nunca dijo cuánto gana) se lo preguntamos.
    effect(() => {
      const d = this.datos();
      if (d && d.ingreso === null && !this.omitioIngreso) this.modalIngreso.set(true);
    });
    if (this.auth.esProfesional()) this.cargarCierre();
  }

  /** Mes al que pasa una deuda trasladada desde el mes que se está viendo. */
  protected mesSiguiente(): string {
    return nombreMes((this.periodo.mes() % 12) + 1);
  }

  protected etiquetaCierre(c: CierreMes): string {
    return nombreMes(c.mes);
  }

  private cargarCierre(): void {
    this.api.cierre().subscribe({
      next: (c) => {
        const pendiente = c && c.pendientes.length > 0 ? c : null;
        this.cierre.set(pendiente);
        if (pendiente && !this.omitioCierre) this.modalCierre.set(true);
      },
      // Si falla, Mis cuentas sigue funcionando; el cierre se ofrece la próxima vez.
      error: () => this.cierre.set(null),
    });
  }

  protected cerrarMes(decisiones: DecisionCierre[]): void {
    const c = this.cierre();
    if (!c) return;
    this.guardandoCierre.set(true);
    this.errorCierre.set(null);
    this.api.cerrarMes(c.anio, c.mes, decisiones).subscribe({
      next: ({ pagadas, trasladadas }) => {
        this.guardandoCierre.set(false);
        this.modalCierre.set(false);
        const partes = [
          pagadas ? `${pagadas} ${pagadas === 1 ? 'pagada' : 'pagadas'}` : null,
          trasladadas ? `${trasladadas} ${trasladadas === 1 ? 'pasó' : 'pasaron'} a ${nombreMes((c.mes % 12) + 1)}` : null,
        ].filter(Boolean);
        this.avisos.exito(`Cierre de ${nombreMes(c.mes)}: ${partes.join(' y ')}.`);
        this.cargarCierre();
        this.recargar();
      },
      error: (e) => {
        this.guardandoCierre.set(false);
        this.errorCierre.set(mensajeDeError(e));
      },
    });
  }

  protected cierreCerrado(): void {
    this.omitioCierre = true;
    this.modalCierre.set(false);
  }

  protected etiquetaCategoria(valor: CategoriaCuenta): string {
    return CATEGORIAS_CUENTA.find((c) => c.valor === valor)?.etiqueta ?? valor;
  }

  protected vencida(c: CuentaPersonal): boolean {
    return estaVencida(c, this.periodo.anio(), this.periodo.mes());
  }

  protected cambiar(cambios: Partial<CuentaBorrador>): void {
    this.borrador.update((b) => ({ ...b, ...cambios }));
  }

  protected editar(c: CuentaPersonal): void {
    this.error.set(null);
    this.borrador.set({
      id: c.id,
      nombre: c.nombre,
      categoria: c.categoria,
      monto: c.monto,
      diaVencimiento: c.diaVencimiento,
    });
  }

  protected guardar(): void {
    const b = this.borrador();
    if (b.nombre.trim().length < 2) return this.error.set('Escribe el nombre de la cuenta (ej.: Arriendo).');
    if (!(b.monto > 0)) return this.error.set('El monto debe ser mayor que cero.');
    if (b.diaVencimiento !== null && (b.diaVencimiento < 1 || b.diaVencimiento > 31))
      return this.error.set('El día de vencimiento va del 1 al 31.');
    this.error.set(null);
    this.guardando.set(true);
    this.api
      .guardar(
        {
          anio: this.periodo.anio(),
          mes: this.periodo.mes(),
          nombre: b.nombre.trim(),
          categoria: b.categoria,
          monto: b.monto,
          diaVencimiento: b.diaVencimiento,
        },
        b.id,
      )
      .subscribe({
        next: () => {
          this.guardando.set(false);
          this.avisos.exito(b.id ? 'Cuenta actualizada.' : 'Cuenta agregada.');
          this.borrador.set(VACIA);
          this.recargar();
        },
        error: (e) => {
          this.guardando.set(false);
          this.error.set(mensajeDeError(e));
        },
      });
  }

  protected eliminar(c: CuentaPersonal): void {
    this.api.eliminar(c.id).subscribe({
      next: () => {
        this.avisos.exito(`Eliminaste "${c.nombre}".`);
        if (this.borrador().id === c.id) this.borrador.set(VACIA);
        this.recargar();
      },
      error: (e) => this.avisos.error(e),
    });
  }

  /** Abre la elección del día de pago: hoy, o el último día del mes si se mira un mes pasado. */
  protected elegirDiaPago(c: CuentaPersonal): void {
    const ultimoDelMes = fechaIso(new Date(this.periodo.anio(), this.periodo.mes(), 0));
    this.fechaPago.set(ultimoDelMes < this.hoy ? ultimoDelMes : this.hoy);
    this.pagando.set(c.id);
  }

  protected confirmarPago(c: CuentaPersonal): void {
    const fecha = this.fechaPago();
    if (!fecha || fecha > this.hoy) {
      this.avisos.error('Elige un día de pago que no sea posterior a hoy.');
      return;
    }
    this.api.pagar(c.id, fecha).subscribe({
      next: () => {
        this.pagando.set(null);
        this.avisos.exito(`"${c.nombre}" quedó pagada.`);
        this.recargar();
      },
      error: (e) => this.avisos.error(e),
    });
  }

  protected quitarPago(c: CuentaPersonal): void {
    this.api.quitarPago(c.id).subscribe({
      next: () => this.recargar(),
      error: (e) => this.avisos.error(e),
    });
  }

  protected copiarMesAnterior(): void {
    this.api.copiarMesAnterior(this.periodo.anio(), this.periodo.mes()).subscribe({
      next: ({ copiadas }) => {
        this.avisos.exito(
          copiadas
            ? `Copiamos ${copiadas} ${copiadas === 1 ? 'cuenta' : 'cuentas'} del mes anterior.`
            : 'No hay cuentas nuevas que copiar del mes anterior.',
        );
        this.recargar();
      },
      error: (e) => this.avisos.error(e),
    });
  }

  protected guardarIngreso(monto: number): void {
    this.guardandoIngreso.set(true);
    this.errorIngreso.set(null);
    this.api.guardarIngreso(this.periodo.anio(), this.periodo.mes(), monto).subscribe({
      next: () => {
        this.guardandoIngreso.set(false);
        this.modalIngreso.set(false);
        this.avisos.exito('Ingreso guardado.');
        this.recargar();
      },
      error: (e) => {
        this.guardandoIngreso.set(false);
        this.errorIngreso.set(mensajeDeError(e));
      },
    });
  }

  protected modalCerrado(): void {
    if (this.datos()?.ingreso === null) this.omitioIngreso = true;
    this.modalIngreso.set(false);
  }

  private recargar(): void {
    this.recargas.update((n) => n + 1);
  }
}
