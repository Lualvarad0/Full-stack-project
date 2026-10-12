import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { TributacionService } from '@core/contabilidad/services/tributacion.service';
import { cargar } from '@core/http';
import { Titulo, ErrorCarga } from '@shared/components';
import { DineroPipe } from '@shared/pipes';
import { CargasFamiliares } from './cargas-familiares';
import { PaginaContable } from '../contabilidad/pagina-contable';
@Component({
  selector: 'app-gastos-personales',
  imports: [FormsModule, Titulo, ErrorCarga, DineroPipe, CargasFamiliares],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './gastos-personales.html',
})
export class GastosPersonalesPagina extends PaginaContable {
  private readonly api = inject(TributacionService);
  protected readonly carga = cargar(
    () => this.recarga(),
    () =>
      forkJoin({
        gastos: this.api.gastos(2026),
        cargas: this.api.cargas(2026),
        renta: this.api.renta(2026),
      }),
  );
  protected readonly parametros = computed(() => {
    const p = this.carga().datos?.renta.parametros;
    return p ? structuredClone(p) : null;
  });
  protected confirmado = false;
  protected override limpiar(): void {
    this.confirmado = false;
  }
  protected override refrescar(): void {
    this.confirmado = false;
    super.refrescar();
  }
  protected revisar(): void {
    this.ejecutar(
      this.api.revisar('GastosPersonales', 2026, 1, 12, this.confirmado),
      'Anexo personal revisado internamente; presenta y registra su referencia desde Declaraciones.',
    );
  }
  protected guardar(): void {
    const p = this.parametros();
    const d = this.carga().datos;
    if (!p || !d) return;
    p.proyeccion = d.gastos.rubros.map((r) => ({
      categoria: r.categoria,
      importe: Number(r.proyectado),
    }));
    this.ejecutar(this.api.guardarRenta(2026, p), 'Proyección actualizada.');
  }
}
