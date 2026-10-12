import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { AuthService } from '@core/auth';
import { SeguridadCuentaService } from '@core/auth/services/seguridad-cuenta.service';
import { AvisosService } from '@core/ui';
import { cargar } from '@core/http';
import { CampoClave, ErrorCarga } from '@shared/components';
import { claveCuenta, clavesIguales } from '../auth/validators/clave.validator';

@Component({ selector: 'app-seguridad-cuenta', imports: [ReactiveFormsModule, CampoClave, DatePipe, ErrorCarga], changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './seguridad-cuenta.html',
})
export class SeguridadCuentaTarjeta {
  private readonly servicio = inject(SeguridadCuentaService); private readonly auth = inject(AuthService); private readonly avisos = inject(AvisosService);
  private readonly recarga = signal(0);
  protected readonly estado = cargar(() => this.recarga(), () => this.servicio.estado());
  protected readonly sesiones = cargar(() => this.recarga(), () => this.servicio.sesiones());
  protected readonly ocupado = signal(false);
  protected readonly confirmando = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({ actual: ['', Validators.required], password: ['', [Validators.required, claveCuenta]], confirmacion: ['', Validators.required] }, { validators: clavesIguales });
  protected verificar(): void {
    if (this.ocupado()) return; this.ocupado.set(true);
    this.servicio.enviarVerificacion().subscribe({ next: () => { this.ocupado.set(false); this.avisos.exito('Si todavía no está verificado y no solicitaste otro enlace recientemente, recibirás un correo. Revisa tu bandeja.'); }, error: e => { this.ocupado.set(false); this.avisos.error(e); } });
  }
  protected cambiar(): void {
    if (this.ocupado()) return; this.form.markAllAsTouched(); if (this.form.invalid) return;
    this.ocupado.set(true); const v = this.form.getRawValue();
    this.servicio.cambiarClave(v.actual, v.password, v.confirmacion).subscribe({ next: () => { this.form.reset(); this.auth.cerrarSesion(); this.avisos.exito('Contraseña actualizada. Ingresa con tu nueva contraseña.'); }, error: e => { this.ocupado.set(false); this.avisos.error(e); } });
  }
  protected revocar(id: string): void { this.ejecutar(this.servicio.revocar(id)); }
  protected cerrarOtras(): void { this.ejecutar(this.servicio.cerrarOtras()); }
  private ejecutar(peticion: ReturnType<SeguridadCuentaService['cerrarOtras']>): void {
    if (this.ocupado()) return; this.ocupado.set(true);
    peticion.subscribe({ next: () => { this.ocupado.set(false); this.recarga.update(x => x + 1); this.avisos.exito('Sesiones actualizadas.'); }, error: e => { this.ocupado.set(false); this.avisos.error(e); } });
  }
}
