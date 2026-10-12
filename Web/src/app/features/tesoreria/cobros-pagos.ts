import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import {
  FormsModule,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin, Observable } from 'rxjs';
import { CarteraService } from '@core/contabilidad/services/cartera.service';
import { TesoreriaService, hoyEcuador } from '@core/contabilidad/services/tesoreria.service';
import { cargar } from '@core/http';
import { AvisosService } from '@core/ui';
import { Titulo, ErrorCarga } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';
import { PaginaContable } from '../contabilidad/pagina-contable';
@Component({
  selector: 'app-cobros-pagos',
  imports: [
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
    Titulo,
    ErrorCarga,
    DineroPipe,
    FechaCortaPipe,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './cobros-pagos.html',
})
export class CobrosPagosPagina extends PaginaContable {
  private readonly servicio = inject(TesoreriaService);
  protected readonly seleccionado = signal(0);
  protected motivo = '';
  protected vencimiento = '';
  protected readonly carga = cargar(
    () => this.recarga(),
    () =>
      forkJoin({
        saldos: this.servicio.saldos(),
        pagos: this.servicio.pagos(),
        cuentas: this.servicio.cuentas(),
      }),
  );
  protected readonly pendientes = computed(() =>
    (this.carga().datos?.saldos ?? []).filter((s) => s.saldo !== 0),
  );
  protected readonly porCobrar = computed(() =>
    this.pendientes()
      .filter((s) => s.direccion === 'Emitido')
      .reduce((t, s) => t + Math.max(0, s.saldo), 0),
  );
  protected readonly porPagar = computed(() =>
    this.pendientes()
      .filter((s) => s.direccion === 'Recibido')
      .reduce((t, s) => t + Math.max(0, s.saldo), 0),
  );
  protected readonly form = inject(NonNullableFormBuilder).group({
    comprobanteId: [0, Validators.min(1)],
    fecha: [hoyEcuador(), Validators.required],
    importe: [0, [Validators.min(0.01), Validators.max(999999999)]],
    tipo: ['Pago'],
    medio: ['Transferencia'],
    referencia: ['', Validators.maxLength(100)],
    cuentaBancariaId: [0],
  });
  protected registrar(): void {
    if (this.ocupado()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.ejecutar(
      this.servicio.registrar({
        ...v,
        comprobanteId: Number(v.comprobanteId),
        cuentaBancariaId: Number(v.cuentaBancariaId) || null,
        referencia: v.referencia || null,
      }),
    );
  }
  protected anular(id: number): void {
    if (this.motivo.trim().length < 10) {
      this.avisos.error('Explica el motivo de anulación.');
      return;
    }
    this.ejecutar(this.servicio.anular(id, this.motivo));
  }
  protected guardarVencimiento(): void {
    if (this.seleccionado() && this.vencimiento)
      this.ejecutar(this.servicio.vencimiento(this.seleccionado(), this.vencimiento));
  }
  protected override limpiar(): void {
    this.form?.reset();
    this.seleccionado?.set(0);
    this.motivo = '';
    this.vencimiento = '';
  }
  protected override refrescar(): void {
    this.motivo = '';
    super.refrescar();
  }
}
