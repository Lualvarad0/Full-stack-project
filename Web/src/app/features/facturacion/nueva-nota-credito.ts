import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { FacturacionService } from '@core/facturacion';
import { ComplementariosService } from '@core/facturacion/services/complementarios.service';
import { cargar } from '@core/http';
import { AvisosService } from '@core/ui';
import { CarteraService } from '@core/contabilidad/services/cartera.service';
import { ErrorCarga, Titulo } from '@shared/components';
import { DineroPipe } from '@shared/pipes';
@Component({ selector: 'app-nueva-nota-credito', imports: [ReactiveFormsModule, RouterLink, ErrorCarga, Titulo, DineroPipe], changeDetection: ChangeDetectionStrategy.OnPush, templateUrl: './nueva-nota-credito.html' })
export class NuevaNotaCredito {
  readonly facturaId = input.required<string>(); private readonly facturas = inject(FacturacionService); private readonly servicio = inject(ComplementariosService); private readonly avisos = inject(AvisosService); private readonly router = inject(Router); protected readonly cartera = inject(CarteraService);
  protected readonly carga = cargar(() => Number(this.facturaId()), id => this.facturas.factura(id)); protected readonly ocupado = signal(false);
  protected readonly filas = new FormArray<FormGroup<{ detalleId: FormControl<number>; cantidad: FormControl<number> }>>([]);
  protected readonly motivo = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(5), Validators.maxLength(300)] });
  constructor() { effect(() => { const f = this.carga().datos; this.filas.clear(); for (const d of f?.detalles ?? []) this.filas.push(new FormGroup({ detalleId: new FormControl(d.id ?? 0, { nonNullable: true }), cantidad: new FormControl(d.cantidad, { nonNullable: true, validators: [Validators.min(0), Validators.max(d.cantidad)] }) })); }); }
  protected emitir(): void {
    if (this.ocupado() || !this.cartera.puedeEditar() || !this.carga().datos) return; this.motivo.markAsTouched(); this.filas.markAllAsTouched();
    if (this.motivo.invalid || this.filas.invalid) return; const detalles = this.filas.getRawValue().filter(d => d.cantidad > 0);
    if (!detalles.length) { this.avisos.error('Elige al menos un concepto y una cantidad a acreditar.'); return; } this.ocupado.set(true);
    this.servicio.credito(Number(this.facturaId()), this.motivo.value, detalles).subscribe({ next: f => { this.ocupado.set(false); this.avisos.exito('Nota de crédito generada. Revisa su estado ante el SRI.'); void this.router.navigate(['/facturacion', f.id]); }, error: e => { this.ocupado.set(false); this.avisos.error(e); } });
  }
}
