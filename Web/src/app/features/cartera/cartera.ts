import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ReactiveFormsModule, NonNullableFormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '@core/auth';
import { CarteraService } from '@core/contabilidad/services/cartera.service';
import { AvisosService } from '@core/ui';
import { cargar } from '@core/http';
import { of } from 'rxjs';
import { CupoPlanContador, ErrorCarga, Titulo } from '@shared/components';
@Component({ selector: 'app-cartera', imports: [ReactiveFormsModule, Titulo, ErrorCarga, CupoPlanContador], changeDetection: ChangeDetectionStrategy.OnPush, templateUrl: './cartera.html' })
export class CarteraPagina {
  protected readonly servicio = inject(CarteraService); protected readonly auth = inject(AuthService);
  private readonly avisos = inject(AvisosService); private readonly router = inject(Router);
  private readonly recarga = signal(0); protected readonly ocupado = signal(false);
  protected readonly solicitudes = cargar(() => this.recarga(), () => this.servicio.solicitudes());
  protected readonly esContador = () => this.auth.usuario()?.plan === 'Contadores' || this.auth.usuario()?.rol === 'admin';
  /** Espacios usados de la cartera (solo plan Contadores). */
  protected readonly cupo = cargar(() => this.recarga(), () => (this.esContador() ? this.servicio.cupo() : of(null)));
  protected readonly lleno = computed(() => { const c = this.cupo().datos; return !!c && c.limite !== null && c.usados >= c.limite; });
  protected readonly form = inject(NonNullableFormBuilder).group({ correo: ['', [Validators.required, Validators.email, Validators.maxLength(150)]], permiso: ['Consulta' as 'Consulta' | 'Edicion'] });
  protected solicitar(): void {
    if (this.ocupado()) return; this.form.markAllAsTouched(); if (this.form.invalid) return; this.ocupado.set(true);
    const v = this.form.getRawValue(); this.servicio.solicitar(v.correo, v.permiso).subscribe({ next: r => { this.ocupado.set(false); this.form.reset(); this.recarga.update(n => n + 1); this.avisos.exito(r.mensaje); }, error: e => { this.ocupado.set(false); this.avisos.error(e); } });
  }
  protected responder(id: number, aceptar: boolean): void { this.ejecutar(aceptar ? this.servicio.aceptar(id) : this.servicio.rechazar(id)); }
  protected revocar(id: number): void { this.ejecutar(this.servicio.revocar(id)); }
  protected abrir(id: number): void { this.servicio.seleccionar(id); void this.router.navigate(['/resumen']); }
  private ejecutar(p: ReturnType<CarteraService['aceptar']>): void {
    if (this.ocupado()) return; this.ocupado.set(true);
    p.subscribe({ next: () => { this.ocupado.set(false); this.recarga.update(n => n + 1); this.servicio.recargar(); this.avisos.exito('Acceso actualizado.'); }, error: e => { this.ocupado.set(false); this.avisos.error(e); } });
  }
}
