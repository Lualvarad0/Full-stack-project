import { ListaCarga } from '@shared/components/lista-carga';
import { TablaComprobantes } from '../comprobante/tabla-comprobantes';
import { ListadoComprobantes } from '../comprobante/listado-comprobantes';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideFileUp, LucideX } from '@lucide/angular';
import {
  categoria,
  CATEGORIAS,
  CategoriaGasto,
  ComprobanteLista,
  FiltroComprobantes,
} from '@core/comprobantes';
import { AuthService } from '@core/auth';
import { AvisosService } from '@core/ui';
import {
  Buscador,
  ErrorCarga,
  Filtros,
  InsigniaPro,
  Titulo,
  UsoPlanAviso,
} from '@shared/components';
import { DineroPipe } from '@shared/pipes';
@Component({
  selector: 'app-gastos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ListaCarga,
    TablaComprobantes,
    Buscador,
    ErrorCarga,
    InsigniaPro,
    UsoPlanAviso,
    Titulo,
    Filtros,
    RouterLink,
    DineroPipe,
    LucideFileUp,
    LucideX,
  ],
  templateUrl: './gastos.html',
})
export class Gastos extends ListadoComprobantes {
  /** Query params (?categoria=SinCategoria, ?ruc=...) enlazados por el router. */
  readonly categoria = input<CategoriaGasto | ''>();
  readonly ruc = input<string | undefined>();
  /** ?ajenos=true: solo facturas que no están a nombre del usuario. */
  readonly ajenos = input(false, { transform: (v: unknown) => v === true || v === 'true' });

  private readonly avisos = inject(AvisosService);
  protected readonly auth = inject(AuthService);
  protected readonly categorias = CATEGORIAS;
  protected readonly guardando = signal<number | null>(null);
  protected readonly direccion = 'Recibido' as const;

  protected override filtrosAdicionales(): FiltroComprobantes {
    return { categoria: this.categoria() || undefined, ruc: this.ruc() };
  }
  protected override filtrosActivos(): boolean {
    return !!(this.categoria() || this.ruc() || this.ajenos());
  }
  protected override seleccionar(datos: ComprobanteLista[]): ComprobanteLista[] {
    return this.ajenos() ? datos.filter((c) => c.aNombreDelUsuario === false) : datos;
  }

  protected readonly proveedorFiltrado = computed(() => {
    const ruc = this.ruc();
    return ruc
      ? (this.lista().find((c) => c.rucEmisor === ruc)?.razonSocialEmisor ?? `RUC ${ruc}`)
      : null;
  });
  protected filtrarCategoria(valor: string): void {
    void this.router.navigate([], {
      queryParams: { categoria: valor || null },
      queryParamsHandling: 'merge',
    });
  }

  protected cambiarCategoria(c: ComprobanteLista, nueva: CategoriaGasto): void {
    this.guardando.set(c.id);
    this.comprobantes.cambiarCategoria(c.id, nueva, false).subscribe({
      next: () => {
        this.guardando.set(null);
        this.avisos.exito(
            `${categoria(nueva).nombre}: guardado para este comprobante.`,
        );
        this.recargas.update((n) => n + 1);
      },
      error: (e) => {
        this.guardando.set(null);
        this.avisos.error(e);
        this.recargas.update((n) => n + 1);
      },
    });
  }
}
