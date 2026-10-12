import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideMonitorSmartphone } from '@lucide/angular';
import { ModoTema, PreferenciasService, TamanoTexto } from '@core/ui';
import { Segmentado } from '@shared/components';

/** Configuración → Preferencias: apariencia en este dispositivo (se guarda en el navegador). */
@Component({
  selector: 'app-preferencias-dispositivo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  imports: [Segmentado, LucideMonitorSmartphone],
  template: `
    <section class="card p-5 sm:p-6" aria-labelledby="titulo-preferencias">
      <div class="flex items-start gap-3">
        <span class="grid size-10 shrink-0 place-items-center rounded-xl bg-primary-soft text-primary">
          <svg lucideMonitorSmartphone class="size-5" aria-hidden="true"></svg>
        </span>
        <div class="min-w-0">
          <h2 id="titulo-preferencias" class="font-semibold">Apariencia</h2>
          <p class="mt-1 text-sm text-muted-fg">
            Solo en este dispositivo: en tu celular puedes tener otra.
          </p>
        </div>
      </div>

      <dl class="mt-5 divide-y divide-border">
        <div class="flex flex-wrap items-center justify-between gap-3 py-4">
          <div class="min-w-0 max-w-md">
            <dt class="font-medium">Tema</dt>
            <dd class="mt-0.5 text-sm text-muted-fg">"Del sistema" sigue el modo claro u oscuro de tu equipo.</dd>
          </div>
          <app-segmentado etiqueta="Tema" [opciones]="temas" [(valor)]="preferencias.modoTema" />
        </div>

        <div class="flex flex-wrap items-center justify-between gap-3 py-4">
          <div class="min-w-0 max-w-md">
            <dt class="font-medium">Tamaño del texto</dt>
            <dd class="mt-0.5 text-sm text-muted-fg">Agranda textos, montos y botones en toda la app.</dd>
          </div>
          <app-segmentado etiqueta="Tamaño del texto" [opciones]="textos" [(valor)]="preferencias.texto" />
        </div>

        <div class="flex flex-wrap items-center justify-between gap-3 py-4">
          <div class="min-w-0 max-w-md">
            <dt><label class="font-medium" for="reducir-movimiento">Reducir animaciones</label></dt>
            <dd class="mt-0.5 text-sm text-muted-fg">
              Quita transiciones y movimientos. También se activa sola si tu equipo lo pide.
            </dd>
          </div>
          <input
            id="reducir-movimiento"
            type="checkbox"
            role="switch"
            class="size-5 cursor-pointer accent-[var(--accent)]"
            [checked]="preferencias.reducirMovimiento()"
            (change)="preferencias.reducirMovimiento.set($any($event.target).checked)"
          />
        </div>
      </dl>

      <button type="button" class="btn btn-ghost mt-2 -ml-3 text-sm" (click)="preferencias.restablecer()">
        Restablecer apariencia
      </button>
    </section>
  `,
})
export class PreferenciasDispositivo {
  protected readonly preferencias = inject(PreferenciasService);
  protected readonly temas: readonly { valor: ModoTema; etiqueta: string }[] = [
    { valor: 'light', etiqueta: 'Claro' },
    { valor: 'dark', etiqueta: 'Oscuro' },
    { valor: 'sistema', etiqueta: 'Del sistema' },
  ];
  protected readonly textos: readonly { valor: TamanoTexto; etiqueta: string }[] = [
    { valor: 'normal', etiqueta: 'Normal' },
    { valor: 'grande', etiqueta: 'Grande' },
  ];
}
