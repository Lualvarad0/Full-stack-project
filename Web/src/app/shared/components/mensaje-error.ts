import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LucideCircleAlert } from '@lucide/angular';
/** Mensaje de error con ícono; no muestra nada si no hay mensaje. El margen lo pone quien lo usa (class="mt-4 block"). */
@Component({
  selector: 'app-mensaje-error',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideCircleAlert],
  template: `
    @if (mensaje()) {
      <p class="aviso-error py-3" role="alert">
        <svg lucideCircleAlert class="mt-0.5 size-4 shrink-0 text-danger" aria-hidden="true"></svg>
        <span>{{ mensaje() }}</span>
      </p>
    }
  `,
})
export class MensajeError {
  readonly mensaje = input<string | null | undefined>(null);
}
