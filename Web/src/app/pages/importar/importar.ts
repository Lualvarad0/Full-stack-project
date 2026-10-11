import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { LucideCircleCheck, LucideCircleDashed, LucideCircleMinus, LucideCircleX, LucideClipboardPaste, LucideFileUp, LucideLoader, LucideX } from '@lucide/angular';
import { ESTADOS_IMPORTACION, MAX_BYTES_IMPORTACION } from '@core/constants';
import { ImportacionResponse, ModoImportacion } from '@core/models';
import { ComprobantesService } from '@core/services';
import { mensajeDeError } from '@core/utils';
import { BuzonTarjeta, MensajeError, Titulo, UsoPlanAviso, ZonaArchivos } from '@shared/components';

@Component({
  selector: 'app-importar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensajeError, ZonaArchivos, BuzonTarjeta, UsoPlanAviso, Titulo, RouterLink, LucideFileUp, LucideClipboardPaste, LucideLoader, LucideX, LucideCircleCheck, LucideCircleMinus, LucideCircleDashed, LucideCircleX],
  templateUrl: './importar.html',
})
export class Importar {
  private readonly comprobantes = inject(ComprobantesService);

  protected readonly modo = signal<ModoImportacion>('archivos');
  protected readonly archivos = signal<File[]>([]);
  protected readonly texto = signal('');
  protected readonly importando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly resultado = signal<ImportacionResponse | null>(null);

  protected readonly estados = ESTADOS_IMPORTACION;
  protected readonly pesoTotal = computed(() => this.archivos().reduce((s, a) => s + a.size, 0));
  protected readonly puedeImportar = computed(() =>
    !this.importando() && (this.modo() === 'archivos' ? this.archivos().length > 0 : this.texto().trim().length > 0),
  );
  protected readonly problemas = computed(() =>
    (this.resultado()?.claves ?? []).filter((c) => c.resultado !== 'Importado' && c.resultado !== 'YaExistia'),
  );

  protected agregar(lista: File[]): void {
    const validos = lista.filter((a) => /\.(txt|xml|csv)$/i.test(a.name));
    if (validos.length < lista.length) this.error.set('Solo se aceptan archivos .txt o .xml (el reporte del SRI o los XML de tus facturas).');
    else this.error.set(null);

    const nombres = new Set(this.archivos().map((a) => a.name));
    this.archivos.update((actuales) => [...actuales, ...validos.filter((a) => !nombres.has(a.name))]);
    if (this.pesoTotal() > MAX_BYTES_IMPORTACION) this.error.set('Los archivos superan 10 MB en total. Súbelos en partes.');
  }

  protected quitar(archivo: File): void {
    this.archivos.update((lista) => lista.filter((a) => a !== archivo));
  }

  protected importar(): void {
    if (!this.puedeImportar() || this.pesoTotal() > MAX_BYTES_IMPORTACION) return;
    const peticion: Observable<ImportacionResponse> = this.modo() === 'archivos'
      ? this.comprobantes.importarArchivos(this.archivos())
      : this.comprobantes.importarTexto(this.texto());

    this.importando.set(true);
    this.error.set(null);
    this.resultado.set(null);
    peticion.subscribe({
      next: (r) => {
        this.importando.set(false);
        this.resultado.set(r);
        this.archivos.set([]);
        this.texto.set('');
      },
      error: (e) => {
        this.importando.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }

  protected tamano(bytes: number): string {
    return bytes < 1024 ? `${bytes} B` : bytes < 1024 * 1024 ? `${(bytes / 1024).toFixed(0)} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }
}
