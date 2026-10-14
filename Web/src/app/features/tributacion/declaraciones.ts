import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { TributacionService } from '@core/contabilidad/services/tributacion.service';
import { hoyEcuador } from '@core/contabilidad/services/tesoreria.service';
import { cargar } from '@core/http';
import { Titulo, ErrorCarga, SelectorFecha } from '@shared/components';
import { DineroPipe } from '@shared/pipes';
import { PaginaContable } from '../contabilidad/pagina-contable';
import { ParametrosIvaFormulario } from './parametros-iva';
import { ParametrosRentaFormulario } from './parametros-renta';
import { RevisionFiscalDocumentos } from './revision-fiscal-documentos';
@Component({
  selector: 'app-declaraciones',
  imports: [
    SelectorFecha,
    FormsModule,
    RouterLink,
    Titulo,
    ErrorCarga,
    DineroPipe,
    ParametrosIvaFormulario,
    ParametrosRentaFormulario,
    RevisionFiscalDocumentos,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './declaraciones.html',
})
export class DeclaracionesPagina extends PaginaContable {
  private readonly api = inject(TributacionService);
  protected readonly tipo = signal('Iva');
  protected readonly mes = signal(Number(hoyEcuador().slice(5, 7)));
  protected readonly meses = signal(1);
  protected readonly periodoMes = computed(() =>
    this.tipo() === 'Renta' || this.tipo() === 'GastosPersonales' ? 1 : this.mes(),
  );
  protected readonly periodoMeses = computed(() =>
    this.tipo() === 'Renta' || this.tipo() === 'GastosPersonales' ? 12 : this.meses(),
  );
  protected confirmado = false;
  protected referencia = '';
  protected fechaPresentacion = hoyEcuador();
  protected readonly carga = cargar(
    () => [this.tipo(), this.periodoMes(), this.periodoMeses(), this.recarga()],
    () =>
      forkJoin({
        iva: this.api.iva(2026, this.periodoMes(), this.periodoMeses()),
        renta: this.api.renta(2026),
        documentos: this.api.documentos(2026, this.periodoMes(), this.periodoMeses()),
        retenciones: this.api.retenciones(2026, this.periodoMes(), this.periodoMeses()),
        ats: this.api.ats(2026, this.periodoMes(), this.periodoMeses()),
        revisiones: this.api.revisiones(2026),
      }),
  );
  protected readonly pendientes = computed(() => {
    const d = this.carga().datos;
    return !d
      ? []
      : this.tipo() === 'Renta'
        ? d.renta.pendientes
        : this.tipo() === 'Retenciones'
          ? d.retenciones.pendientes
          : this.tipo() === 'Ats'
            ? d.ats.pendientes
            : d.iva.pendientes;
  });
  protected override limpiar(): void {
    this.confirmado = false;
    this.referencia = '';
    this.fechaPresentacion = hoyEcuador();
  }
  protected override refrescar(): void {
    this.confirmado = false;
    super.refrescar();
  }
  protected cambiarMeses(meses: number): void {
    this.meses.set(Number(meses));
    if (meses === 6) this.mes.set(this.mes() <= 6 ? 1 : 7);
  }
  protected revisar(): void {
    this.ejecutar(
      this.api.revisar(this.tipo(), 2026, this.periodoMes(), this.periodoMeses(), this.confirmado),
      'Preparación revisada; todavía no presentada al SRI.',
    );
  }
  protected presentar(id: number): void {
    if (this.referencia.trim().length < 5 || !this.fechaPresentacion) {
      this.avisos.error('Indica fecha y referencia del comprobante de presentación del SRI.');
      return;
    }
    this.ejecutar(
      this.api.presentar(id, this.referencia, this.fechaPresentacion),
      'Presentación registrada como información manual.',
    );
  }
}
