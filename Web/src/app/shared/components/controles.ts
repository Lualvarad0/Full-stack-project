import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { LucideSearch } from '@lucide/angular';
import { OpcionSegmento } from '@core/models';
import { MensajeError } from './mensaje-error';

/** Selector de opciones en píldora (tema, tipo de período, pestañas…). */
@Component({
  selector: 'app-segmentado',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="inline-flex rounded-full bg-muted p-1 text-sm" role="radiogroup" [attr.aria-label]="etiqueta()">
      @for (o of opciones(); track o.valor) {
        <button
          type="button"
          role="radio"
          class="min-h-8 rounded-full px-3 font-medium transition-colors"
          [class]="o.valor === valor() ? 'bg-card text-fg shadow-sm' : 'text-muted-fg hover:text-fg'"
          [attr.aria-checked]="o.valor === valor()"
          (click)="valor.set(o.valor)"
        >{{ o.etiqueta }}</button>
      }
    </div>
  `,
})
export class Segmentado<T extends string | boolean> {
  readonly opciones = input.required<readonly OpcionSegmento<T>[]>();
  readonly valor = model.required<T>();
  readonly etiqueta = input('');
}

/** Campo de búsqueda con ícono. */
@Component({
  selector: 'app-buscador',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideSearch],
  host: { class: 'relative block min-w-0 flex-1 basis-64' },
  template: `
    <svg lucideSearch class="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-fg" aria-hidden="true"></svg>
    <input type="search" class="input pl-9" [placeholder]="placeholder()" [attr.aria-label]="placeholder()"
      [value]="valor()" (input)="valor.set($any($event.target).value)" />
  `,
})
export class Buscador {
  readonly valor = model('');
  readonly placeholder = input('Buscar');
}

/** Bloque de error de carga de una página. */
@Component({
  selector: 'app-error-carga',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensajeError],
  template: `
    <app-mensaje-error class="mt-6 block" [mensaje]="mensaje()" />
  `,
})
export class ErrorCarga {
  readonly mensaje = input<string | null>(null);
}

/** Etiqueta "Pro" de las secciones del plan Profesional. */
@Component({
  selector: 'app-insignia-pro',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="rounded-full bg-primary-soft px-2 py-0.5 text-[11px] font-medium text-primary">Pro</span>`,
})
export class InsigniaPro {}
