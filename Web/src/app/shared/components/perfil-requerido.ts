import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideIdCard } from '@lucide/angular';
@Component({
  selector: 'app-perfil-requerido',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideIdCard],
  template: `
    <section class="card mt-6 p-6 sm:p-10">
      <svg lucideIdCard class="size-8 text-accent" aria-hidden="true"></svg>
      <h2 class="mt-3 text-lg font-semibold">{{ titulo() }}</h2>
      <p class="mt-2 max-w-2xl text-sm text-muted-fg">{{ descripcion() }}</p>
      <a routerLink="/configuracion" class="btn btn-primary mt-6">Completar mi perfil</a>
    </section>
  `,
})
export class PerfilRequerido {
  readonly titulo = input.required<string>();
  readonly descripcion = input.required<string>();
}
