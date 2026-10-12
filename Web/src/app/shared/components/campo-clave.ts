import { ChangeDetectionStrategy, Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { LucideEye, LucideEyeOff } from '@lucide/angular';
/**
 * Campo de contraseña con botón para mostrar u ocultar lo escrito.
 * Funciona con formularios reactivos: <app-campo-clave formControlName="password" idCampo="password" />.
 */
@Component({
  selector: 'app-campo-clave',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideEye, LucideEyeOff],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => CampoClave), multi: true },
  ],
  host: { class: 'relative block' },
  template: `
    <input
      [id]="idCampo()"
      maxlength="200"
      class="input min-h-11 pr-12"
      [type]="visible() ? 'text' : 'password'"
      [attr.autocomplete]="autocomplete()"
      [attr.aria-invalid]="invalido()"
      [attr.aria-describedby]="describedby()"
      [value]="valor()"
      [disabled]="deshabilitado()"
      (input)="escribir($any($event.target).value)"
      (blur)="alTocar()"
    />
    <button
      type="button"
      class="absolute inset-y-0 right-0 grid w-11 cursor-pointer place-items-center rounded-r-lg text-muted-fg transition-colors hover:text-fg"
      [attr.aria-label]="visible() ? 'Ocultar contraseña' : 'Mostrar contraseña'"
      [attr.aria-pressed]="visible()"
      (click)="visible.set(!visible())"
    >
      @if (visible()) {
        <svg lucideEyeOff class="size-[18px]" aria-hidden="true"></svg>
      } @else {
        <svg lucideEye class="size-[18px]" aria-hidden="true"></svg>
      }
    </button>
  `,
})
export class CampoClave implements ControlValueAccessor {
  /** Id del input (para el <label for>). */
  readonly idCampo = input.required<string>();
  readonly autocomplete = input('current-password');
  readonly invalido = input(false);
  readonly describedby = input<string | null>(null);

  protected readonly visible = signal(false);
  protected readonly valor = signal('');
  protected readonly deshabilitado = signal(false);
  private alCambiar: (valor: string) => void = () => {};
  protected alTocar: () => void = () => {};

  protected escribir(valor: string): void {
    this.valor.set(valor);
    this.alCambiar(valor);
  }

  /** Vuelve a ocultar la contraseña (por ejemplo, al limpiar el formulario). */
  ocultar(): void {
    this.visible.set(false);
  }

  writeValue(valor: string | null): void {
    this.valor.set(valor ?? '');
    if (!valor) this.visible.set(false);
  }

  registerOnChange(fn: (valor: string) => void): void {
    this.alCambiar = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.alTocar = fn;
  }

  setDisabledState(deshabilitado: boolean): void {
    this.deshabilitado.set(deshabilitado);
  }
}
