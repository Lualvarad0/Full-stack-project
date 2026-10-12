import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FacturacionService } from '@core/facturacion';
import { AuthService } from '@core/auth';
import { AvisosService } from '@core/ui';
import { descargarArchivo } from '@core/utils';

@Component({
  selector: 'app-factura-documento',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="card mt-6 p-5" aria-label="Documento PDF">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <h2 class="font-semibold">Factura en PDF</h2>
        <div class="flex gap-2">
          <button class="btn btn-secondary" type="button" [disabled]="cargando()" (click)="abrir()">{{ cargando() ? 'Cargando PDF…' : 'Ver PDF' }}</button>
          <button class="btn btn-secondary" type="button" [disabled]="cargando()" (click)="descargar()">Descargar PDF</button>
          @if (vista()) { <button class="btn btn-ghost" type="button" (click)="cerrar()">Cerrar</button> }
        </div>
      </div>
      @if (vista(); as url) {
        <iframe class="mt-4 h-[70vh] w-full rounded-lg border border-border" [src]="url" title="Vista de la factura PDF"></iframe>
        <p class="mt-2 text-sm text-muted-fg">Si tu navegador no muestra el documento, utiliza Descargar PDF.</p>
      }
    </section>`,
})
export class FacturaDocumento {
  readonly id = input.required<number>();
  readonly numero = input.required<string>();
  private readonly servicio = inject(FacturacionService);
  private readonly auth = inject(AuthService);
  private readonly avisos = inject(AvisosService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroyRef = inject(DestroyRef);
  private readonly documento = signal<{ id: number; token: string | null; clave: string | null; url: string; segura: SafeResourceUrl } | null>(null);
  protected readonly cargando = signal(false);
  protected readonly vista = computed(() => {
    const d = this.documento();
    return d?.id === this.id() && d.token === this.auth.token() && d.clave === (this.auth.claveDatos?.() ?? this.auth.token()) ? d.segura : null;
  });
  constructor() {
    effect((cleanup) => {
      this.id(); this.auth.token(); this.auth.claveDatos?.();
      untracked(() => this.cerrar()); this.cargando.set(false);
      cleanup(() => this.cerrar());
    });
  }
  protected cerrar(): void {
    const d = this.documento();
    if (d) URL.revokeObjectURL(d.url);
    this.documento.set(null);
  }
  protected abrir(): void { this.obtener(false); }
  protected descargar(): void { this.obtener(true); }
  private obtener(descarga: boolean): void {
    const id = this.id(), token = this.auth.token(), clave = this.auth.claveDatos?.() ?? token, numero = this.numero();
    this.cargando.set(true);
    this.servicio.descargarPdf(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        if (id !== this.id() || token !== this.auth.token() || clave !== (this.auth.claveDatos?.() ?? this.auth.token())) return;
        this.cargando.set(false);
        if (blob.type !== 'application/pdf') { this.avisos.error('El servidor no devolvió un PDF válido.'); return; }
        if (descarga) { descargarArchivo(blob, `factura-${numero}.pdf`); return; }
        this.cerrar();
        // Únicamente confiamos en una URL blob creada aquí a partir del PDF autenticado del servidor.
        const url = URL.createObjectURL(blob);
        this.documento.set({ id, token, clave, url, segura: this.sanitizer.bypassSecurityTrustResourceUrl(url) });
      },
      error: (e) => { if (id === this.id() && token === this.auth.token()) { this.cargando.set(false); this.avisos.error(e); } },
    });
  }
}
