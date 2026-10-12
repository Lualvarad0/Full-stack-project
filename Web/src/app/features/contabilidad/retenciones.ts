import { ChangeDetectionStrategy, Component, inject, signal, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '@core/auth';
import { PeriodoService } from '@core/comprobantes';
import { CarteraService } from '@core/contabilidad/services/cartera.service';
import { RetencionesService } from '@core/contabilidad/services/retenciones.service';
import { AvisosService } from '@core/ui';
import { cargar } from '@core/http';
import { descargarArchivo } from '@core/utils';
import { Titulo, ErrorCarga } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';
@Component({ selector: 'app-retenciones', imports: [FormsModule, RouterLink, Titulo, ErrorCarga, DineroPipe, FechaCortaPipe], changeDetection: ChangeDetectionStrategy.OnPush, templateUrl: './retenciones.html' })
export class RetencionesPagina {
  private readonly servicio = inject(RetencionesService); protected readonly cartera = inject(CarteraService); private readonly avisos = inject(AvisosService); private readonly periodo = inject(PeriodoService);
  private readonly recarga = signal(0); protected readonly seleccionado = signal(0); protected readonly ocupado = signal(false); protected texto = '';
  protected readonly carga = cargar(() => ({ anio: this.periodo.anio(), mes: this.periodo.mes(), r: this.recarga() }), v => this.servicio.listar(v.anio, v.mes));
  protected readonly detalle = cargar(() => ({ id: this.seleccionado(), r: this.recarga() }), v => v.id ? this.servicio.obtener(v.id) : of(null));
  constructor() { const auth = inject(AuthService); effect(() => { auth.claveDatos(); this.seleccionado.set(0); }); }
  protected importar(): void { if (!this.texto.trim() || this.texto.length > 1024 * 1024) return; this.procesar(this.servicio.importar(this.texto)); }
  protected archivos(event: Event): void { const input = event.target as HTMLInputElement; const archivos = [...(input.files ?? [])]; if (archivos.length) this.procesar(this.servicio.archivos(archivos)); input.value = ''; }
  private procesar(p: ReturnType<RetencionesService['importar']>): void {
    if (this.ocupado() || !this.cartera.puedeEditar()) return; this.ocupado.set(true);
    p.subscribe({ next: r => { this.ocupado.set(false); this.texto = ''; this.recarga.update(n => n + 1); this.avisos.exito(`Retenciones importadas: ${r.importadas}. Existentes: ${r.existentes}.`); if (r.problemas.length) this.avisos.error(r.problemas.join(' | ')); }, error: e => { this.ocupado.set(false); this.avisos.error(e); } });
  }
  protected revisar(id: number): void { this.servicio.revisar(id).subscribe({ next: () => { this.recarga.update(n => n + 1); this.avisos.exito('Retención revisada.'); }, error: e => this.avisos.error(e) }); }
  protected descargar(id: number, pdf: boolean): void { (pdf ? this.servicio.pdf(id) : this.servicio.xml(id)).subscribe({ next: b => descargarArchivo(b, `retencion-${id}.${pdf ? 'pdf' : 'xml'}`), error: e => this.avisos.error(e) }); }
}
