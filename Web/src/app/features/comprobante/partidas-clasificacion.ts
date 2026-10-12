import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { CATEGORIAS, CategoriaGasto, CategoriaIngreso, Comprobante, ComprobantesService, FinalidadComprobante, TratamientoIva } from '@core/comprobantes';
import { CarteraService } from '@core/contabilidad/services/cartera.service';
import { AvisosService } from '@core/ui';
import { DineroPipe } from '@shared/pipes';

@Component({ selector: 'app-partidas-clasificacion', imports: [ReactiveFormsModule, DineroPipe], changeDetection: ChangeDetectionStrategy.OnPush, templateUrl: './partidas-clasificacion.html' })
export class PartidasClasificacion {
  readonly comprobante = input.required<Comprobante>(); readonly guardado = output<void>();
  private readonly servicio = inject(ComprobantesService); protected readonly cartera = inject(CarteraService); private readonly avisos = inject(AvisosService);
  protected readonly abierto = signal(false); protected readonly guardando = signal(false); protected readonly pagina = signal(0);
  protected readonly ingreso = computed(() => this.comprobante().direccion === 'Emitido');
  protected readonly datos = computed(() => this.comprobante().detalles.slice(this.pagina() * 100, (this.pagina() + 1) * 100));
  protected readonly categorias = CATEGORIAS.filter(c => c.deducible);
  protected readonly filas = new FormArray<FormGroup<{
    id: FormControl<number>; finalidad: FormControl<FinalidadComprobante>; categoria: FormControl<string>; categoriaIngreso: FormControl<string>; tratamientoIva: FormControl<TratamientoIva>;
  }>>([]);
  constructor() {
    effect(() => {
      const c = this.comprobante(); const datos = this.datos(); this.filas.clear();
      for (const d of datos) {
        const finalidad = d.finalidad && d.finalidad !== 'SinDefinir' ? d.finalidad : c.direccion === 'Emitido' ? 'Negocio' : d.sugerencia?.categoria ? 'Personal' : c.finalidad && c.finalidad !== 'Mixto' ? c.finalidad : 'SinDefinir';
        this.filas.push(new FormGroup({ id: new FormControl(d.id ?? 0, { nonNullable: true }),
          finalidad: new FormControl<FinalidadComprobante>(finalidad, { nonNullable: true }),
          categoria: new FormControl(d.categoria ?? d.sugerencia?.categoria ?? (c.finalidad === 'Personal' ? c.categoria : ''), { nonNullable: true }),
          categoriaIngreso: new FormControl(d.categoriaIngreso ?? d.sugerencia?.categoriaIngreso ?? c.categoriaIngreso ?? '', { nonNullable: true }),
          tratamientoIva: new FormControl<TratamientoIva>(d.tratamientoIva ?? 'Pendiente', { nonNullable: true }),
        }));
      }
      if (!this.cartera.puedeEditar()) this.filas.disable();
    });
  }
  protected guardar(): void {
    if (this.guardando() || !this.cartera.puedeEditar()) return;
    const valores = this.filas.getRawValue();
    if (valores.some(v => v.id <= 0 || v.finalidad === 'SinDefinir' || (v.finalidad === 'Personal' && !v.categoria) || (this.ingreso() && !v.categoriaIngreso))) { this.avisos.error('Elige finalidad y categoría para cada concepto de esta página.'); return; }
    this.guardando.set(true);
    this.servicio.clasificarPartidas(this.comprobante().id, valores.map(v => ({ id: v.id, finalidad: v.finalidad,
      categoria: v.finalidad === 'Personal' ? v.categoria as CategoriaGasto : null, categoriaIngreso: this.ingreso() ? v.categoriaIngreso as CategoriaIngreso : null,
      tratamientoIva: v.finalidad === 'Negocio' ? v.tratamientoIva : 'SinCredito',
    }))).subscribe({ next: () => { this.guardando.set(false); this.avisos.exito('Conceptos clasificados. Los gastos personales usan la base sin IVA ni ICE.'); this.guardado.emit(); }, error: e => { this.guardando.set(false); this.avisos.error(e); } });
  }
}
