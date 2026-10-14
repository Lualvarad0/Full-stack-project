import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Location } from '@angular/common';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SeguridadCuentaService } from '@core/auth/services/seguridad-cuenta.service';
import { AuthService } from '@core/auth';
import { mensajeDeError } from '@core/http';
import { CampoClave, Marca, MensajeError } from '@shared/components';
import { claveCuenta, clavesIguales } from './validators/clave.validator';

@Component({ selector: 'app-restablecer-clave', imports: [ReactiveFormsModule, RouterLink, CampoClave, Marca, MensajeError],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="grid min-h-dvh place-items-center bg-bg px-4 py-8"><section class="card w-full max-w-md p-6 sm:p-8">
      <app-marca /><h1 class="mt-6 text-2xl font-semibold">Elige tu nueva contraseña</h1>
      @if (terminado()) { <p class="aviso-atencion mt-5 text-sm" role="status">Contraseña actualizada. Todas las sesiones anteriores se cerraron. Ingresa con tu nueva contraseña.</p> }
      @else if (!token) { <p class="aviso-error mt-5 text-sm">El enlace está incompleto. Solicita uno nuevo.</p> }
      @else {
        <form class="mt-5 space-y-4" [formGroup]="form" (ngSubmit)="guardar()">
          <div><label for="nueva-clave" class="field-label">Nueva contraseña</label><app-campo-clave idCampo="nueva-clave" autocomplete="new-password" formControlName="password" /></div>
          <p class="text-xs text-muted-fg">Usa al menos 8 caracteres, sin espacios. El máximo es 72 bytes; los acentos y emojis pueden ocupar más de uno.</p>
          <div><label for="confirmar-clave" class="field-label">Repite la contraseña</label><app-campo-clave idCampo="confirmar-clave" autocomplete="new-password" formControlName="confirmacion" /></div>
          @if (form.touched && form.invalid) { <p class="field-error">Revisa la longitud, quita los espacios y comprueba que las contraseñas coincidan.</p> }
          <app-mensaje-error [mensaje]="error()" />
          <button class="btn btn-primary w-full" type="submit" [disabled]="enviando()">{{ enviando() ? 'Guardando…' : 'Guardar contraseña' }}</button>
        </form>
      }
      <a class="mt-5 block text-sm text-accent hover:underline" [routerLink]="terminado() ? '/login' : '/recuperar-clave'">{{ terminado() ? 'Iniciar sesión' : 'Solicitar otro enlace' }}</a>
    </section></main>`,
})
export class RestablecerClave {
  private readonly servicio = inject(SeguridadCuentaService);
  private readonly auth = inject(AuthService);
  protected token = new URLSearchParams(inject(ActivatedRoute).snapshot.fragment ?? '').get('token') ?? '';
  protected readonly form = inject(NonNullableFormBuilder).group({ password: ['', [Validators.required, claveCuenta]], confirmacion: ['', Validators.required] }, { validators: clavesIguales });
  protected readonly enviando = signal(false); protected readonly terminado = signal(false); protected readonly error = signal('');
  constructor() { if (!/^[a-fA-F0-9]{64}$/.test(this.token)) this.token = ''; inject(Location).replaceState('/restablecer-clave'); }
  protected guardar(): void {
    if (this.enviando()) return; this.form.markAllAsTouched(); if (this.form.invalid || !this.token) return;
    this.error.set(''); this.enviando.set(true); const { password, confirmacion } = this.form.getRawValue();
    this.servicio.restablecer(this.token, password, confirmacion).subscribe({
      next: () => { this.auth.cerrarSesion(false); this.enviando.set(false); this.terminado.set(true); this.token = ''; this.form.reset(); },
      error: e => { this.enviando.set(false); this.error.set(mensajeDeError(e)); },
    });
  }
}
