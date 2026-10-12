import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { SeguridadCuentaService } from '@core/auth/services/seguridad-cuenta.service';
import { mensajeDeError } from '@core/http';
import { Marca, MensajeError } from '@shared/components';

@Component({
  selector: 'app-recuperar-clave', imports: [ReactiveFormsModule, RouterLink, Marca, MensajeError],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="grid min-h-dvh place-items-center bg-bg px-4 py-8">
      <section class="card w-full max-w-md p-6 sm:p-8">
        <app-marca /><h1 class="mt-6 text-2xl font-semibold">Recupera tu contraseña</h1>
        <p class="mt-2 text-sm text-muted-fg">Te enviaremos un enlace para elegir una nueva contraseña.</p>
        @if (mensaje()) { <p class="aviso-atencion mt-5 text-sm" role="status">{{ mensaje() }}</p> }
        <form class="mt-5 space-y-4" [formGroup]="form" (ngSubmit)="enviar()">
          <div><label for="correo-recuperacion" class="field-label">Correo de tu cuenta</label>
          <input id="correo-recuperacion" class="input" type="email" autocomplete="email" maxlength="150" formControlName="email" required />
          @if (form.controls.email.touched && form.controls.email.invalid) { <p class="field-error">Escribe un correo válido.</p> }</div>
          <app-mensaje-error [mensaje]="error()" />
          <button class="btn btn-primary w-full" type="submit" [disabled]="enviando()">{{ enviando() ? 'Enviando…' : 'Enviar enlace' }}</button>
        </form><a class="mt-5 block text-sm text-accent hover:underline" routerLink="/login">Volver a iniciar sesión</a>
      </section>
    </main>`,
})
export class RecuperarClave {
  private readonly servicio = inject(SeguridadCuentaService);
  protected readonly form = inject(NonNullableFormBuilder).group({ email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]] });
  protected readonly enviando = signal(false); protected readonly mensaje = signal(''); protected readonly error = signal('');
  protected enviar(): void {
    if (this.enviando()) return; this.form.markAllAsTouched(); if (this.form.invalid) return;
    this.error.set(''); this.enviando.set(true);
    this.servicio.recuperar(this.form.getRawValue().email).subscribe({
      next: r => { this.enviando.set(false); this.mensaje.set(r.mensaje); },
      error: e => { this.enviando.set(false); this.error.set(mensajeDeError(e)); },
    });
  }
}
