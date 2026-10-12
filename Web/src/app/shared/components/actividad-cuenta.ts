import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import {
  LucideLogIn,
  LucideMail,
  LucideSettings,
  LucideShieldCheck,
  LucideUserPlus,
} from '@lucide/angular';
import { CorreoService } from '@core/correo';
import { cargar } from '@core/http';
import { fechaHora } from '@core/utils';
/** Actividad de seguridad: ingresos y cambios de configuración de la cuenta. */
@Component({
  selector: 'app-actividad-cuenta',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideLogIn, LucideSettings, LucideUserPlus, LucideMail, LucideShieldCheck],
  template: `
    <section class="card p-5 sm:p-6" aria-labelledby="titulo-actividad">
      <div class="flex items-start gap-3">
        <span
          class="grid size-10 shrink-0 place-items-center rounded-full estado-ok"
          aria-hidden="true"
        >
          <svg lucideShieldCheck class="size-5"></svg>
        </span>
        <div>
          <h2 id="titulo-actividad" class="font-semibold">Actividad de tu cuenta</h2>
          <p class="mt-1 text-sm text-muted-fg">
            Cada ingreso y cada cambio de configuración queda registrado y te lo avisamos por
            correo.
          </p>
        </div>
      </div>

      @if (carga().cargando && !carga().datos) {
        <div class="mt-5 h-24 animate-pulse rounded-xl bg-muted"></div>
      } @else {
        <ul class="mt-4 divide-y divide-border">
          @for (a of carga().datos ?? []; track a.id) {
            <li class="flex items-start gap-3 py-3 text-sm">
              <span class="mt-0.5 text-muted-fg" aria-hidden="true">
                @switch (a.tipo) {
                  @case ('Ingreso') {
                    <svg lucideLogIn class="size-4"></svg>
                  }
                  @case ('Registro') {
                    <svg lucideUserPlus class="size-4"></svg>
                  }
                  @default {
                    <svg lucideSettings class="size-4"></svg>
                  }
                }
              </span>
              <div class="min-w-0 flex-1">
                <p class="font-medium">{{ a.titulo }}</p>
                <p class="text-muted-fg">{{ a.detalle }}</p>
                <p class="mt-0.5 text-xs text-muted-fg">
                  {{ fecha(a.fecha) }}{{ a.dispositivo ? ' · ' + a.dispositivo : ''
                  }}{{ a.ip ? ' · IP ' + a.ip : '' }}
                </p>
              </div>
              @if (a.enviadoPorCorreo) {
                <span
                  class="pildora shrink-0 bg-muted text-muted-fg"
                  title="Aviso enviado por correo"
                >
                  <svg lucideMail class="size-3" aria-hidden="true"></svg> Enviado
                </span>
              }
            </li>
          } @empty {
            <li class="py-6 text-center text-sm text-muted-fg">Sin actividad registrada.</li>
          }
        </ul>
      }
    </section>
  `,
})
export class ActividadCuenta {
  private readonly correo = inject(CorreoService);
  protected readonly carga = cargar(
    () => 0,
    () => this.correo.avisos(),
  );

  protected fecha(iso: string): string {
    return fechaHora(iso);
  }
}
