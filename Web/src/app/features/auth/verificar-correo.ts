import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Location } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SeguridadCuentaService } from '@core/auth/services/seguridad-cuenta.service';
import { mensajeDeError } from '@core/http';
import { Marca, MensajeError } from '@shared/components';

@Component({ selector: 'app-verificar-correo', imports: [RouterLink, Marca, MensajeError], changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<main class="grid min-h-dvh place-items-center px-4"><section class="card max-w-md p-6"><app-marca /><h1 class="mt-6 text-2xl font-semibold">Verifica tu correo</h1>
    <p class="mt-3 text-sm">{{ listo() ? 'Correo verificado. Ya puedes volver a tu cuenta.' : 'Confirma que tienes acceso al correo de tu cuenta.' }}</p>
    <app-mensaje-error [mensaje]="error()" />
    @if (!listo()) { <button class="btn btn-primary mt-4" [disabled]="enviando() || !token" (click)="verificar()">Verificar mi correo</button> }
    <a routerLink="/configuracion" class="mt-4 block text-sm text-accent">Volver a mi cuenta</a></section></main>`,
})
export class VerificarCorreo {
  private readonly servicio = inject(SeguridadCuentaService);
  protected token = new URLSearchParams(inject(ActivatedRoute).snapshot.fragment ?? '').get('token') ?? '';
  protected readonly listo = signal(false); protected readonly enviando = signal(false); protected readonly error = signal('');
  constructor() { if (!/^[a-fA-F0-9]{64}$/.test(this.token)) { this.token = ''; this.error.set('El enlace está incompleto. Solicita otro desde tu cuenta.'); } inject(Location).replaceState('/verificar-correo'); }
  protected verificar(): void {
    if (!this.token || this.enviando()) return; this.enviando.set(true);
    this.servicio.verificar(this.token).subscribe({ next: () => { this.listo.set(true); this.enviando.set(false); this.token = ''; }, error: e => { this.enviando.set(false); this.error.set(mensajeDeError(e)); } });
  }
}
