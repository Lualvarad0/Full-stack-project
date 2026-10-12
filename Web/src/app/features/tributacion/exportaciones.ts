import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  ExportacionesService,
  ValidacionAts,
} from '@core/contabilidad/services/exportaciones.service';
import { hoyEcuador } from '@core/contabilidad/services/tesoreria.service';
import { descargarArchivo } from '@core/utils';
import { Titulo } from '@shared/components';
import { PaginaContable } from '../contabilidad/pagina-contable';
@Component({
  selector: 'app-exportaciones',
  imports: [FormsModule, RouterLink, Titulo],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './exportaciones.html',
})
export class ExportacionesPagina extends PaginaContable {
  private readonly api = inject(ExportacionesService);
  protected mes = Number(hoyEcuador().slice(5, 7));
  protected meses = 1;
  protected readonly validacion = signal<ValidacionAts | null>(null);
  protected override limpiar(): void {
    this.validacion?.set(null);
    this.mes = Number(hoyEcuador().slice(5, 7));
    this.meses = 1;
  }
  protected periodo(): void {
    if (this.meses === 12) this.mes = 1;
    if (this.meses === 6) this.mes = this.mes <= 6 ? 1 : 7;
  }
  protected descargar(tipo: 'comprobantes' | 'balance' | 'paquete', formato = 'xlsx'): void {
    if (this.ocupado()) return;
    this.ocupado.set(true);
    const clave = this.auth.claveDatos();
    this.api.descargar(tipo, Number(this.mes), Number(this.meses), formato).subscribe({
      next: (blob) => {
        if (clave !== this.auth.claveDatos()) return;
        this.ocupado.set(false);
        descargarArchivo(
          blob,
          `${tipo}-2026-${String(this.mes).padStart(2, '0')}.${tipo === 'paquete' ? 'zip' : formato}`,
        );
      },
      error: (e) => {
        if (clave !== this.auth.claveDatos()) return;
        this.ocupado.set(false);
        this.avisos.error(e);
      },
    });
  }
  protected validar(event: Event): void {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    input.value = '';
    if (!archivo || this.ocupado()) return;
    if (archivo.size > 5 * 1024 * 1024 || !archivo.name.toLowerCase().endsWith('.xml')) {
      this.avisos.error('Selecciona un XML de hasta 5 MB.');
      return;
    }
    this.ocupado.set(true);
    this.validacion.set(null);
    const clave = this.auth.claveDatos();
    this.api.validarAts(archivo).subscribe({
      next: (v) => {
        if (clave !== this.auth.claveDatos()) return;
        this.ocupado.set(false);
        this.validacion.set(v);
      },
      error: (e) => {
        if (clave !== this.auth.claveDatos()) return;
        this.ocupado.set(false);
        this.avisos.error(e);
      },
    });
  }
}
