import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, Observable, of } from 'rxjs';
import { Asiento, LibrosService } from '@core/contabilidad/services/libros.service';
import { TesoreriaService, hoyEcuador } from '@core/contabilidad/services/tesoreria.service';
import { CarteraService } from '@core/contabilidad/services/cartera.service';
import { cargar } from '@core/http';
import { AvisosService } from '@core/ui';
import { Titulo, ErrorCarga } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';
import { PlanCuentas } from './plan-cuentas';
import { EditorAsiento } from './editor-asiento';
import { PaginaContable } from './pagina-contable';
@Component({
  selector: 'app-libros',
  imports: [
    FormsModule,
    Titulo,
    ErrorCarga,
    DineroPipe,
    FechaCortaPipe,
    PlanCuentas,
    EditorAsiento,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './libros.html',
})
export class LibrosPagina extends PaginaContable {
  private readonly api = inject(LibrosService);
  private readonly tesoreria = inject(TesoreriaService);
  protected readonly tab = signal('asientos');
  protected readonly anio = signal(Number(hoyEcuador().slice(0, 4)));
  protected readonly mes = signal(Number(hoyEcuador().slice(5, 7)));
  protected readonly cuentaId = signal(0);
  protected readonly editando = signal<Asiento | null>(null);
  protected readonly editor = signal(false);
  protected motivo = '';
  protected fuente = 0;
  protected tipoFuente = 'comprobante';
  protected readonly desde = computed(() => `${this.anio()}-01-01`);
  protected readonly hasta = computed(
    () =>
      `${this.anio()}-${String(this.mes()).padStart(2, '0')}-${new Date(this.anio(), this.mes(), 0).getDate()}`,
  );
  protected readonly carga = cargar(
    () => [this.anio(), this.mes(), this.recarga()],
    () =>
      forkJoin({
        cuentas: this.api.cuentas(),
        asientos: this.api.asientos(this.anio(), this.mes()),
        estados: this.api.estados(this.desde(), this.hasta()),
        comprobantes: this.tesoreria.saldos(),
        pagos: this.tesoreria.pagos(),
      }),
  );
  protected readonly mayor = cargar(
    () => [this.cuentaId(), this.anio(), this.mes(), this.recarga()],
    () => (this.cuentaId() ? this.api.mayor(this.cuentaId(), this.desde(), this.hasta()) : of([])),
  );
  protected nuevo(): void {
    this.editando.set(null);
    this.editor.set(true);
  }
  protected editar(a: Asiento): void {
    this.editando.set(a);
    this.editor.set(true);
  }
  protected override refrescar(): void {
    this.editor.set(false);
    this.editando.set(null);
    super.refrescar();
  }
  protected contabilizar(id: number): void {
    this.ejecutar(this.api.contabilizar(id));
  }
  protected anular(id: number): void {
    if (this.motivo.trim().length < 10) {
      this.avisos.error('Explica la anulación con al menos diez caracteres.');
      return;
    }
    this.ejecutar(this.api.anular(id, this.motivo));
  }
  protected preparar(): void {
    if (!this.fuente) return;
    this.ejecutar(
      this.tipoFuente === 'pago'
        ? this.api.desdePago(Number(this.fuente))
        : this.api.desdeComprobante(Number(this.fuente)),
    );
  }
  protected override limpiar(): void {
    this.editando?.set(null);
    this.editor?.set(false);
    this.cuentaId?.set(0);
    this.motivo = '';
    this.fuente = 0;
  }
}
