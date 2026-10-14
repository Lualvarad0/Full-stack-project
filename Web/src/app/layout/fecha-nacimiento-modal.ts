import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { LucideCake, LucideLoader } from '@lucide/angular';
import { AuthService } from '@core/auth';
import { mensajeDeError } from '@core/http';
import { CuentaUsuarioService } from '@core/perfil';
import { haceAnios } from '@features/auth/validators/registro.validator';
import { MensajeError, SelectorFecha } from '@shared/components';

/**
 * Cuentas creadas antes de pedir la fecha de nacimiento: Nudo es solo para mayores de edad, así que se pide
 * una vez y no se puede cerrar sin responder (o salir de la cuenta).
 */
@Component({
  selector: 'app-fecha-nacimiento-modal',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensajeError, SelectorFecha, LucideCake, LucideLoader],
  template: `
    <dialog
      #dialogo
      class="m-auto w-[min(26rem,calc(100vw-2rem))] rounded-2xl border border-border bg-bg-2 p-0 text-fg shadow-xl backdrop:bg-black/50"
      aria-labelledby="titulo-nacimiento"
      (cancel)="$event.preventDefault()"
    >
      <form class="p-6" (submit)="$event.preventDefault(); guardar()" novalidate>
        <span class="grid size-11 place-items-center rounded-xl bg-primary-soft text-primary">
          <svg lucideCake class="size-5" aria-hidden="true"></svg>
        </span>
        <h2 id="titulo-nacimiento" class="mt-4 text-lg font-semibold">Confirma tu fecha de nacimiento</h2>
        <p class="mt-1 text-sm text-muted-fg">
          Nudo es solo para mayores de edad. Tu cuenta es anterior a este requisito: confírmala una vez para seguir.
        </p>
        <label class="field-label mt-5" for="fecha-nacimiento-completar">Fecha de nacimiento</label>
        <app-selector-fecha
          idCampo="fecha-nacimiento-completar"
          min="1900-01-01"
          [max]="fechaMaxima"
          [(valor)]="fecha"
        />
        <app-mensaje-error class="mt-3 block" [mensaje]="error()" />
        <div class="mt-6 flex flex-wrap justify-end gap-2">
          <button type="button" class="btn btn-ghost" (click)="auth.salir()">Salir de mi cuenta</button>
          <button type="submit" class="btn btn-primary" [disabled]="guardando()">
            @if (guardando()) {
              <svg lucideLoader class="size-4 animate-spin" aria-hidden="true"></svg>
            }
            Continuar
          </button>
        </div>
      </form>
    </dialog>
  `,
})
export class FechaNacimientoModal {
  readonly abierto = input(false);
  protected readonly auth = inject(AuthService);
  private readonly cuenta = inject(CuentaUsuarioService);
  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');

  protected readonly fechaMaxima = haceAnios(18);
  protected readonly fecha = signal('');
  protected readonly error = signal<string | null>(null);
  protected readonly guardando = signal(false);

  constructor() {
    effect(() => {
      const d = this.dialogo().nativeElement;
      if (this.abierto() && !d.open) d.showModal();
      else if (!this.abierto() && d.open) d.close();
    });
  }

  protected guardar(): void {
    const fecha = this.fecha();
    if (!fecha) return this.error.set('Indica tu fecha de nacimiento.');
    if (fecha > this.fechaMaxima) return this.error.set('Debes ser mayor de edad para usar Nudo.');
    this.error.set(null);
    this.guardando.set(true);
    this.cuenta.completarFechaNacimiento(fecha).subscribe({
      next: () => this.guardando.set(false),
      error: (e) => {
        this.guardando.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }
}
