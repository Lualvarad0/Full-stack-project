import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';
import { LucideCheck } from '@lucide/angular';
import { AVISO_LANZAMIENTO, ESTADOS_SUSCRIPCION, PLANES } from '@core/constants';
import { AuthService, SuscripcionService } from '@core/services';
import { DineroPipe } from '@shared/pipes';

/** Configuración → plan de la cuenta: el actual, su estado de pago y acceso a cambiarlo o cancelarlo. */
@Component({
  selector: 'app-plan-tarjeta',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, DineroPipe, LucideCheck],
  template: `
    <section class="card p-5 sm:p-6" aria-labelledby="titulo-plan">
      <div class="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 id="titulo-plan" class="font-semibold">Tu plan</h2>
          @if (suscripcion(); as s) {
            <p class="mt-1 text-sm text-muted-fg">
              {{ estados[s.estado] }} · {{ s.estado === 'EnPrueba' ? 'primer cobro' : 'próximo cobro' }} el {{ s.proximoCobro | date: 'dd/MM/yyyy' }}
              ({{ s.total | dinero }}{{ s.periodicidad === 'Anual' ? ' al año' : ' al mes' }})
            </p>
          } @else {
            <p class="mt-1 text-sm text-muted-fg">{{ aviso }}</p>
          }
        </div>
        <a routerLink="/suscripcion" class="btn btn-secondary">{{ actual() === 'Personal' ? 'Mejorar mi plan' : 'Administrar plan' }}</a>
      </div>
      <div class="mt-4 grid gap-3 sm:grid-cols-3">
        @for (p of planes; track p.id) {
          <div class="rounded-xl border p-4" [class]="p.id === actual() ? 'border-accent bg-primary-soft' : 'border-border'">
            <p class="font-semibold">{{ p.nombre }}</p>
            <p class="text-sm"><span class="font-semibold">{{ p.precio }}</span><span class="text-muted-fg">{{ p.periodo }}</span></p>
            <p class="mt-1 text-xs text-muted-fg">{{ p.descripcion }}</p>
            @if (p.id === actual()) {
              <p class="mt-3 flex items-center gap-1.5 text-sm font-medium text-accent">
                <svg lucideCheck class="size-4" aria-hidden="true"></svg> Tu plan actual
              </p>
            }
          </div>
        }
      </div>
    </section>
  `,
})
export class PlanTarjeta {
  private readonly auth = inject(AuthService);

  protected readonly planes = PLANES;
  protected readonly aviso = AVISO_LANZAMIENTO;
  protected readonly estados = ESTADOS_SUSCRIPCION;
  protected readonly actual = computed(() => this.auth.usuario()?.plan ?? 'Personal');
  protected readonly suscripcion = toSignal(inject(SuscripcionService).actual().pipe(catchError(() => of(null))), { initialValue: null });
}
