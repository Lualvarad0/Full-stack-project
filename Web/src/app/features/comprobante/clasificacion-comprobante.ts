import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '@core/auth';
import { CategoriaGasto, CategoriaIngreso, Comprobante, ComprobantesService, categoria } from '@core/comprobantes';
import { CategoriaSelect } from '@shared/components';
import { AvisosService } from '@core/ui';

/** Revisión de sugerencias y elección manual; las reglas viven en el servidor. */
@Component({
  selector: 'app-clasificacion-comprobante',
  imports: [CategoriaSelect],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './clasificacion-comprobante.html',
})
export class ClasificacionComprobante {
  readonly comprobante = input.required<Comprobante>();
  readonly guardado = output<void>();
  private readonly servicio = inject(ComprobantesService);
  private readonly avisos = inject(AvisosService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthService);
  protected readonly guardando = signal(false);
  protected readonly aplicarProveedor = signal(false);
  protected readonly ingreso = computed(() => this.comprobante().direccion === 'Emitido');
  protected readonly nombreSugerido = computed(() => {
    const s = this.comprobante().sugerencia;
    return s?.categoria ? categoria(s.categoria).nombre : this.nombreIngreso(s?.categoriaIngreso);
  });
  constructor() {
    effect(() => {
      this.comprobante().id; this.auth.token(); this.auth.claveDatos?.();
      this.aplicarProveedor.set(false); this.guardando.set(false);
    });
  }
  protected nombreIngreso(valor?: CategoriaIngreso | null): string {
    return valor === 'OtrosIngresos' ? 'Otros ingresos' : valor ?? 'Sin categoría';
  }
  protected confirmar(): void {
    const s = this.comprobante().sugerencia;
    if (this.ingreso() && s?.categoriaIngreso) this.cambiarIngreso(s.categoriaIngreso);
    else if (s?.categoria) this.cambiarGasto(s.categoria);
  }
  protected cambiarGasto(valor: CategoriaGasto): void {
    this.guardar(this.servicio.cambiarCategoria(this.comprobante().id, valor, this.aplicarProveedor()));
  }
  protected cambiarIngreso(valor: string): void {
    if (['Ventas', 'Servicios', 'OtrosIngresos'].includes(valor))
      this.guardar(this.servicio.cambiarCategoriaIngreso(this.comprobante().id, valor as CategoriaIngreso));
  }
  private guardar(peticion: ReturnType<ComprobantesService['obtener']>): void {
    this.guardando.set(true);
    const id = this.comprobante().id, token = this.auth.token(), clave = this.auth.claveDatos?.() ?? token;
    peticion.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { if (id !== this.comprobante().id || token !== this.auth.token() || clave !== (this.auth.claveDatos?.() ?? this.auth.token())) return; this.guardando.set(false); this.avisos.exito('Clasificación guardada en tu cuenta.'); this.guardado.emit(); },
      error: (e) => { if (id !== this.comprobante().id || token !== this.auth.token()) return; this.guardando.set(false); this.avisos.error(e); },
    });
  }
}
