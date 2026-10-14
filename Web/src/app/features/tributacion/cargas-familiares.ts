import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { CargaFamiliar, TributacionService } from '@core/contabilidad/services/tributacion.service';
import { cedulaOPasaporteValido, cedulaOPasaporteValidator } from '@core/perfil';
import { fechaIso } from '@core/utils';
import { SelectorFecha } from '@shared/components';
import { FechaCortaPipe } from '@shared/pipes';
import { PaginaContable } from '../contabilidad/pagina-contable';
@Component({
  selector: 'app-cargas-familiares',
  imports: [ReactiveFormsModule, FechaCortaPipe, SelectorFecha],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './cargas-familiares.html',
})
export class CargasFamiliares extends PaginaContable {
  protected readonly hoy = fechaIso(new Date());
  readonly cargas = input.required<CargaFamiliar[]>();
  readonly cambiado = output<void>();
  private readonly api = inject(TributacionService);
  protected readonly form = inject(NonNullableFormBuilder).group({
    identificacion: ['', [Validators.required, cedulaOPasaporteValidator]],
    nombre: ['', [Validators.required, Validators.maxLength(200)]],
    parentesco: ['Hijo'],
    fechaNacimiento: ['', Validators.required],
    ingresosGravados: [0, [Validators.min(0), Validators.max(482)]],
    discapacidad: [false],
    enfermedadCatastrofica: [false],
    dependenciaConfirmada: [false, Validators.requiredTrue],
  });
  private readonly identificacion = toSignal(this.form.controls.identificacion.valueChanges, {
    initialValue: '',
  });
  protected readonly esPasaporte = computed(() => /\D/.test((this.identificacion() ?? '').trim()));
  /** Avisa mientras se escribe, pero solo cuando ya hay 10 dígitos o más (no a mitad de la cédula). */
  protected readonly identificacionInvalida = computed(() => {
    const id = (this.identificacion() ?? '').trim();
    return (!/^\d+$/.test(id) || id.length >= 10) && !!id && !cedulaOPasaporteValido(id);
  });
  protected override limpiar(): void {
    this.form?.reset();
  }
  protected override refrescar(): void {
    this.form.reset();
    this.cambiado.emit();
  }
  protected registrar(): void {
    this.form.markAllAsTouched();
    if (this.form.valid)
      this.ejecutar(
        this.api.carga({
          ...this.form.getRawValue(),
          identificacion: this.form.controls.identificacion.value.trim().toUpperCase(),
          anio: 2026,
        }),
        'Carga registrada para el cálculo local del ejercicio 2026.',
      );
  }
  protected quitar(id: number): void {
    this.ejecutar(this.api.quitarCarga(id));
  }
}
