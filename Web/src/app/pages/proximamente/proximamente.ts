import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideSparkles } from '@lucide/angular';

/** Módulos del roadmap: explica qué hará cada uno. Título y puntos vienen de la data de la ruta. */
@Component({
  selector: 'app-proximamente',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideSparkles],
  template: `
    <section class="card mx-auto max-w-2xl p-6 sm:p-10">
      <span class="inline-flex items-center gap-1.5 rounded-full bg-primary-soft px-3 py-1 text-xs font-medium text-primary">
        <svg lucideSparkles class="size-3.5" aria-hidden="true"></svg> En desarrollo
      </span>
      <h1 class="mt-4 text-2xl font-semibold tracking-tight">{{ titulo() }}</h1>
      <p class="mt-2 text-muted-fg">{{ descripcion() }}</p>
      <ul class="mt-6 space-y-2 text-sm">
        @for (p of puntos(); track p) {
          <li class="flex gap-2"><span class="mt-2 size-1.5 shrink-0 rounded-full bg-primary" aria-hidden="true"></span>{{ p }}</li>
        }
      </ul>
      <a routerLink="/resumen" class="btn btn-secondary mt-8">Volver al resumen</a>
    </section>
  `,
})
export class Proximamente {
  readonly titulo = input('');
  readonly descripcion = input('');
  readonly puntos = input<string[]>([]);
}
