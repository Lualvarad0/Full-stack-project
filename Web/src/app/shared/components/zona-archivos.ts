import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { LucideUpload } from '@lucide/angular';
/**
 * Zona para soltar o elegir archivos. El texto va como contenido:
 * <app-zona-archivos idCampo="archivos" accept=".xml" (archivos)="agregar($event)">Arrastra…</app-zona-archivos>
 */
@Component({
  selector: 'app-zona-archivos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideUpload],
  host: { class: 'block' },
  template: `
    <label
      [attr.for]="idCampo()"
      class="flex cursor-pointer flex-col items-center justify-center gap-1 rounded-xl border-2 border-dashed px-6 py-8 text-center transition-colors"
      [class]="
        arrastrando()
          ? 'border-accent bg-primary-soft'
          : 'border-border hover:border-accent hover:bg-muted/50'
      "
      (dragover)="$event.preventDefault(); arrastrando.set(true)"
      (dragleave)="arrastrando.set(false)"
      (drop)="soltar($event)"
    >
      <svg lucideUpload class="mb-1 size-8 text-muted-fg" aria-hidden="true"></svg>
      <ng-content />
    </label>
    <input
      [id]="idCampo()"
      type="file"
      class="sr-only"
      [multiple]="multiple()"
      [accept]="accept()"
      (change)="emitir($any($event.target).files); $any($event.target).value = ''"
    />
  `,
})
export class ZonaArchivos {
  readonly idCampo = input.required<string>();
  readonly accept = input('');
  readonly multiple = input(false);
  /** Archivos elegidos o soltados (nunca vacío). */
  readonly archivos = output<File[]>();

  protected readonly arrastrando = signal(false);

  protected soltar(evento: DragEvent): void {
    evento.preventDefault();
    this.arrastrando.set(false);
    this.emitir(evento.dataTransfer?.files ?? null);
  }

  protected emitir(lista: FileList | null): void {
    const archivos = Array.from(lista ?? []);
    if (archivos.length) this.archivos.emit(this.multiple() ? archivos : archivos.slice(0, 1));
  }
}
