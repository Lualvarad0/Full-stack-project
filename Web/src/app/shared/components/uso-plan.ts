import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';
import { LucideSparkles } from '@lucide/angular';
import { AuthService, SuscripcionService } from '@core/services';

/** Plan Personal: cuánto lleva del límite mensual y qué gana pasando a Profesional. No se muestra en planes de pago. */
@Component({
  selector: 'app-uso-plan',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideSparkles],
  template: `
    @if (uso(); as u) {
      @if (!u.profesional && u.limiteComprobantes) {
        <section class="card flex flex-wrap items-center gap-x-6 gap-y-3 p-4 sm:p-5" aria-label="Uso de tu plan">
          <div class="min-w-48 flex-1">
            <p class="text-sm">
              <span class="font-medium">Plan Personal:</span>
              {{ u.comprobantesEsteMes }} de {{ u.limiteComprobantes }} comprobantes este mes · historial de {{ u.mesesHistorial }} meses
            </p>
            <div class="mt-2 h-2 overflow-hidden rounded-full bg-muted" role="progressbar" [attr.aria-valuenow]="u.comprobantesEsteMes"
              aria-valuemin="0" [attr.aria-valuemax]="u.limiteComprobantes" aria-label="Comprobantes usados este mes">
              <div class="h-full rounded-full transition-[width]" [class]="lleno() ? 'bg-danger' : casiLleno() ? 'bg-warning' : 'bg-brand'"
                [style.width.%]="porcentaje()"></div>
            </div>
            @if (lleno()) {
              <p class="mt-1.5 text-xs text-danger">Llegaste al límite: los comprobantes nuevos se importarán el próximo mes o al pasar a Profesional.</p>
            }
          </div>
          <a routerLink="/suscripcion" [queryParams]="{ plan: 'Profesional' }" class="btn btn-primary">
            <svg lucideSparkles class="size-4" aria-hidden="true"></svg> Sin límites con Profesional
          </a>
        </section>
      }
    }
  `,
})
export class UsoPlanAviso {
  private readonly auth = inject(AuthService);
  protected readonly uso = toSignal(
    this.auth.esProfesional() ? of(null) : inject(SuscripcionService).uso().pipe(catchError(() => of(null))),
    { initialValue: null },
  );
  protected readonly porcentaje = computed(() => {
    const u = this.uso();
    return u?.limiteComprobantes ? Math.min(100, (u.comprobantesEsteMes / u.limiteComprobantes) * 100) : 0;
  });
  protected readonly casiLleno = computed(() => this.porcentaje() >= 80);
  protected readonly lleno = computed(() => this.porcentaje() >= 100);
}
