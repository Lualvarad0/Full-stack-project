import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import {
  FormArray,
  FormControl,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
  FormsModule,
} from '@angular/forms';
import { of, forkJoin, Observable } from 'rxjs';
import { PeriodoService } from '@core/comprobantes';
import { CarteraService } from '@core/contabilidad/services/cartera.service';
import { TesoreriaService, hoyEcuador } from '@core/contabilidad/services/tesoreria.service';
import { cargar } from '@core/http';
import { AvisosService } from '@core/ui';
import { descargarArchivo } from '@core/utils';
import { Titulo, ErrorCarga, SelectorFecha } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';
import { PaginaContable } from '../contabilidad/pagina-contable';
@Component({
  selector: 'app-bancos',
  imports: [FormsModule, ReactiveFormsModule, SelectorFecha, Titulo, ErrorCarga, DineroPipe, FechaCortaPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './bancos.html',
})
export class BancosPagina extends PaginaContable {
  private readonly servicio = inject(TesoreriaService);
  private readonly periodo = inject(PeriodoService);
  private readonly fb = inject(NonNullableFormBuilder);
  protected readonly cuentaId = signal(0);
  protected readonly movimientoId = signal(0);
  protected motivo = '';
  protected readonly cuentas = cargar(
    () => this.recarga(),
    () => this.servicio.cuentas(),
  );
  protected readonly operaciones = cargar(
    () => ({
      id: this.cuentaId(),
      anio: this.periodo.anio(),
      mes: this.periodo.mes(),
      r: this.recarga(),
    }),
    (v) =>
      v.id
        ? forkJoin({
            movimientos: this.servicio.movimientos(v.id, v.anio, v.mes),
            pagos: this.servicio.pagos(),
          })
        : of({ movimientos: [], pagos: [] }),
  );
  protected readonly movimientoActual = computed(
    () => this.operaciones().datos?.movimientos.find((m) => m.id === this.movimientoId()) ?? null,
  );
  protected readonly pagosDisponibles = computed(() =>
    (this.operaciones().datos?.pagos ?? []).filter(
      (p) =>
        !p.anulado &&
        p.pendienteConciliar > 0 &&
        p.entrada === (this.movimientoActual()?.importe ?? 0) > 0,
    ),
  );
  protected readonly formCuenta = this.fb.group({
    nombre: ['', [Validators.required, Validators.maxLength(100)]],
    entidad: ['', [Validators.required, Validators.maxLength(100)]],
    numero: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9 -]{4,50}$/)]],
    saldoInicial: [0],
    fechaInicial: [hoyEcuador(), Validators.required],
  });
  protected readonly formMovimiento = this.fb.group({
    idExterno: ['', [Validators.required, Validators.maxLength(100)]],
    fecha: [hoyEcuador(), Validators.required],
    concepto: ['', [Validators.required, Validators.maxLength(300)]],
    referencia: ['', Validators.maxLength(100)],
    importe: [0],
  });
  protected readonly aplicaciones = new FormArray<
    FormGroup<{ pagoId: FormControl<number>; importe: FormControl<number> }>
  >([]);
  constructor() {
    super();
    effect(() => {
      this.cuentas().datos;
      if (this.cuentas().datos?.length === 1) this.cuentaId.set(this.cuentas().datos![0].id);
    });
  }
  protected crearCuenta(): void {
    this.formCuenta.markAllAsTouched();
    if (!this.formCuenta.invalid)
      this.ejecutar(this.servicio.crearCuenta(this.formCuenta.getRawValue()));
  }
  protected crearMovimiento(): void {
    this.formMovimiento.markAllAsTouched();
    if (this.cuentaId() && !this.formMovimiento.invalid) {
      const v = this.formMovimiento.getRawValue();
      this.ejecutar(
        this.servicio.movimiento(this.cuentaId(), { ...v, referencia: v.referencia || null }),
      );
    }
  }
  protected archivo(event: Event): void {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    if (archivo && this.cuentaId()) this.ejecutar(this.servicio.importar(this.cuentaId(), archivo));
    input.value = '';
  }
  protected plantilla(): void {
    descargarArchivo(
      new Blob(
        [
          'id;fecha;concepto;importe;referencia\nMOV-001;' +
            hoyEcuador() +
            ';Cobro cliente;100.00;REF-001\n',
        ],
        { type: 'text/csv;charset=utf-8' },
      ),
      'plantilla-banco.csv',
    );
  }
  protected seleccionarMovimiento(id: number): void {
    this.movimientoId.set(id);
    this.aplicaciones.clear();
    this.agregar();
  }
  protected agregar(): void {
    if (this.aplicaciones.length < 100)
      this.aplicaciones.push(
        new FormGroup({
          pagoId: new FormControl(0, { nonNullable: true }),
          importe: new FormControl(0, { nonNullable: true }),
        }),
      );
  }
  protected conciliar(): void {
    if (this.movimientoId())
      this.ejecutar(
        this.servicio.conciliar(
          this.movimientoId(),
          this.aplicaciones
            .getRawValue()
            .map((p) => ({ pagoId: Number(p.pagoId), importe: p.importe })),
        ),
      );
  }
  protected revisarOtro(id: number): void {
    if (this.motivo.trim().length < 10) {
      this.avisos.error('Indica una justificación de al menos 10 caracteres.');
      return;
    }
    this.ejecutar(this.servicio.revisarOtro(id, this.motivo));
  }
  protected desconciliar(id: number): void {
    if (this.motivo.trim().length < 10) {
      this.avisos.error('Indica el motivo para deshacer la conciliación.');
      return;
    }
    this.ejecutar(this.servicio.desconciliar(id, this.motivo));
  }
  protected override limpiar(): void {
    this.formCuenta?.reset();
    this.formMovimiento?.reset();
    this.cuentaId?.set(0);
    this.movimientoId?.set(0);
    this.aplicaciones?.clear();
    this.motivo = '';
  }
  protected override refrescar(): void {
    this.movimientoId.set(0);
    super.refrescar();
  }
}
