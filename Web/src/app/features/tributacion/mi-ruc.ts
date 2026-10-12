import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import {
  FormsModule,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { TributacionService } from '@core/contabilidad/services/tributacion.service';
import { cargar } from '@core/http';
import { Titulo, ErrorCarga } from '@shared/components';
import { FechaCortaPipe } from '@shared/pipes';
import { PaginaContable } from '../contabilidad/pagina-contable';
@Component({
  selector: 'app-mi-ruc',
  imports: [Titulo, ErrorCarga, FormsModule, ReactiveFormsModule, FechaCortaPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './mi-ruc.html',
})
export class MiRucPagina extends PaginaContable {
  private readonly api = inject(TributacionService);
  protected readonly anio = 2026;
  protected readonly perfil = cargar(
    () => this.recarga(),
    () => this.api.perfil(),
  );
  protected readonly calendario = cargar(
    () => this.recarga(),
    () => this.api.calendario(this.anio),
  );
  protected readonly frecuencias = ['Ninguna', 'Mensual', 'Semestral'];
  protected readonly form = inject(NonNullableFormBuilder).group({
    iva: ['Mensual'],
    retenciones: ['Ninguna'],
    ats: ['Ninguna'],
    renta: [true],
    gastosPersonales: [false],
    sociedad: [false],
    especial: [false],
    galapagos: [false],
    actividades: ['', Validators.maxLength(2000)],
    feriados: [''],
    confirmado: [false],
  });
  constructor() {
    super();
    effect(() => {
      const p = this.perfil().datos;
      if (p)
        this.form.patchValue({
          ...p,
          feriados: (JSON.parse(p.feriados) as string[]).join('\n'),
          confirmado: !!p.confirmadoEn,
        });
    });
  }
  protected override limpiar(): void {
    this.form?.reset();
  }
  protected guardar(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const feriados = v.feriados.split(/[\s,;]+/).filter(Boolean);
    if (feriados.some((f) => !/^202[67]-\d{2}-\d{2}$/.test(f))) {
      this.avisos.error('Escribe cada feriado como AAAA-MM-DD.');
      return;
    }
    this.ejecutar(
      this.api.guardarPerfil({ ...v, feriados }),
      'Obligaciones guardadas. Revisa los vencimientos con el certificado del RUC.',
    );
  }
}
