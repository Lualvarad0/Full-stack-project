import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PeriodoService } from '@core/comprobantes';
import { PeriodosContablesService } from '@core/contabilidad/services/periodos-contables.service';
import { CarteraService } from '@core/contabilidad/services/cartera.service';
import { AvisosService } from '@core/ui';
import { cargar } from '@core/http';
import { Titulo, ErrorCarga } from '@shared/components';
@Component({ selector: 'app-cierre-periodo', imports: [Titulo, ErrorCarga, DatePipe, FormsModule], changeDetection: ChangeDetectionStrategy.OnPush, templateUrl: './cierre-periodo.html' })
export class CierrePeriodoPagina {
  protected readonly periodo = inject(PeriodoService); protected readonly cartera = inject(CarteraService);
  private readonly servicio = inject(PeriodosContablesService); private readonly avisos = inject(AvisosService);
  private readonly recarga = signal(0); protected readonly ocupado = signal(false); protected motivo = '';
  protected readonly revision = cargar(() => ({ anio: this.periodo.anio(), mes: this.periodo.mes(), recarga: this.recarga() }), v => this.servicio.revisar(v.anio, v.mes));
  protected readonly auditoria = cargar(() => this.recarga(), () => this.servicio.auditoria());
  protected cerrar(): void { this.ejecutar(this.servicio.cerrar(this.periodo.anio(), this.periodo.mes())); }
  protected reabrir(): void { if (this.motivo.trim().length < 10) { this.avisos.error('Explica el motivo de reapertura con al menos 10 caracteres.'); return; } this.ejecutar(this.servicio.reabrir(this.periodo.anio(), this.periodo.mes(), this.motivo)); }
  private ejecutar(p: ReturnType<PeriodosContablesService['cerrar']>): void {
    if (this.ocupado() || !this.cartera.puedeEditar()) return; this.ocupado.set(true);
    p.subscribe({ next: () => { this.ocupado.set(false); this.motivo = ''; this.recarga.update(n => n + 1); this.avisos.exito('Estado del período actualizado.'); }, error: e => { this.ocupado.set(false); this.avisos.error(e); } });
  }
}
