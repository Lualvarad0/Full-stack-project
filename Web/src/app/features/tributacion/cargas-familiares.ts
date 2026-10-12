import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CargaFamiliar, TributacionService } from '@core/contabilidad/services/tributacion.service';
import { FechaCortaPipe } from '@shared/pipes';
import { PaginaContable } from '../contabilidad/pagina-contable';
@Component({
  selector: 'app-cargas-familiares',
  imports: [ReactiveFormsModule, FechaCortaPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './cargas-familiares.html',
})
export class CargasFamiliares extends PaginaContable {
  readonly cargas = input.required<CargaFamiliar[]>();
  readonly cambiado = output<void>();
  private readonly api = inject(TributacionService);
  protected readonly form = inject(NonNullableFormBuilder).group({
    identificacion: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9]{5,20}$/)]],
    nombre: ['', [Validators.required, Validators.maxLength(200)]],
    parentesco: ['Hijo'],
    fechaNacimiento: ['', Validators.required],
    ingresosGravados: [0, [Validators.min(0), Validators.max(482)]],
    discapacidad: [false],
    enfermedadCatastrofica: [false],
    dependenciaConfirmada: [false, Validators.requiredTrue],
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
        this.api.carga({ ...this.form.getRawValue(), anio: 2026 }),
        'Carga registrada para el cálculo local del ejercicio 2026.',
      );
  }
  protected quitar(id: number): void {
    this.ejecutar(this.api.quitarCarga(id));
  }
}
