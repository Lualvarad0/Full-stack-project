import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormArray, FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ComprobantesService, PeriodoService } from '@core/comprobantes';
import { ComplementariosService } from '@core/facturacion/services/complementarios.service';
import { CarteraService } from '@core/contabilidad/services/cartera.service';
import { AvisosService } from '@core/ui';
import { cargar } from '@core/http';
import { ErrorCarga, Titulo } from '@shared/components';
import { DineroPipe } from '@shared/pipes';
@Component({ selector: 'app-nueva-retencion', imports: [ReactiveFormsModule, RouterLink, Titulo, ErrorCarga, DineroPipe], changeDetection: ChangeDetectionStrategy.OnPush, templateUrl: './nueva-retencion.html' })
export class NuevaRetencion {
  private readonly servicio = inject(ComplementariosService); private readonly comprobantes = inject(ComprobantesService); private readonly periodo = inject(PeriodoService);
  protected readonly cartera = inject(CarteraService); private readonly avisos = inject(AvisosService); private readonly router = inject(Router); protected readonly ocupado = signal(false);
  protected readonly carga = cargar(() => ({ anio: this.periodo.anio(), mes: this.periodo.mes() }), v => this.comprobantes.listar({ ...v, direccion: 'Recibido' }));
  protected readonly form = inject(NonNullableFormBuilder).group({ comprobanteId: [0, Validators.min(1)], codigoSustento: ['01', [Validators.required, Validators.pattern(/^[0-9]{2}$/)]] });
  protected readonly filas = new FormArray<FormGroup<{ codigoImpuesto: FormControl<string>; codigoRetencion: FormControl<string>; baseImponible: FormControl<number>; porcentaje: FormControl<number> }>>([]);
  constructor() { this.agregar(); }
  protected agregar(): void { if (this.filas.length >= 100) return; this.filas.push(new FormGroup({
    codigoImpuesto: new FormControl('1', { nonNullable: true }), codigoRetencion: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.pattern(/^[0-9]{1,10}$/)] }),
    baseImponible: new FormControl(0, { nonNullable: true, validators: [Validators.min(0.01), Validators.max(999999999)] }), porcentaje: new FormControl(0, { nonNullable: true, validators: [Validators.min(0.01), Validators.max(100)] }),
  })); }
  protected quitar(i: number): void { if (this.filas.length > 1) this.filas.removeAt(i); }
  protected emitir(): void {
    if (this.ocupado() || !this.cartera.puedeEditar()) return; this.form.markAllAsTouched(); this.filas.markAllAsTouched();
    if (this.form.invalid || this.filas.invalid) { this.avisos.error('Revisa el sustento, códigos, bases y porcentajes de retención.'); return; }
    this.ocupado.set(true); const v = this.form.getRawValue();
    this.servicio.retencion(Number(v.comprobanteId), v.codigoSustento, this.filas.getRawValue()).subscribe({ next: f => { this.ocupado.set(false); this.avisos.exito('Retención generada. Revisa su estado ante el SRI.'); void this.router.navigate(['/facturacion', f.id]); }, error: e => { this.ocupado.set(false); this.avisos.error(e); } });
  }
}
