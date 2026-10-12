import { crearBusqueda } from '@core/utils/busqueda';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Observable } from 'rxjs';
import { LucideBan, LucideHistory, LucideTrash2, LucideUserCheck } from '@lucide/angular';
import { ESTADOS_SUSCRIPCION, PLANES, PlanSuscripcion } from '@core/suscripcion';
import { AvisoCuenta } from '@core/correo';
import { UsuarioAdmin, AdminService } from '@core/admin';
import { AuthService } from '@core/auth';
import { AvisosService } from '@core/ui';
import { cargar } from '@core/http';
import { fechaHora } from '@core/utils';
import { Buscador, ErrorCarga } from '@shared/components';
/** Panel de superadministrador: el negocio de un vistazo, usuarios, pagos y estado del servidor. */
@Component({
  selector: 'app-admin-usuarios',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Buscador,
    ErrorCarga,
    DatePipe,
    LucideHistory,
    LucideBan,
    LucideUserCheck,
    LucideTrash2,
  ],
  templateUrl: './usuarios.html',
})
export class AdminUsuarios {
  private readonly servicio = inject(AdminService);
  private readonly avisos = inject(AvisosService);
  private readonly auth = inject(AuthService);

  protected readonly planes = PLANES;
  protected readonly estadosSuscripcion = ESTADOS_SUSCRIPCION;
  protected readonly miId = computed(() => this.auth.usuario()?.id);
  private readonly recargas = signal(0);
  private readonly buscador = crearBusqueda();
  protected readonly busqueda = this.buscador.valor;
  private readonly busquedaAplicada = this.buscador.aplicada;
  protected readonly filtroPlan = signal<PlanSuscripcion | ''>('');
  protected readonly ocupado = signal<number | null>(null);
  protected readonly confirmar = signal<{
    id: number;
    accion: 'suspender' | 'eliminar' | 'cancelar';
    texto: string;
  } | null>(null);
  protected readonly actividad = signal<{ id: number; avisos: AvisoCuenta[] } | null>(null);

  protected readonly usuarios = cargar(
    () => ({ q: this.busquedaAplicada(), plan: this.filtroPlan(), _: this.recargas() }),
    ({ q, plan }) => this.servicio.usuarios(q, plan),
  );
  protected fecha(iso: string): string {
    return fechaHora(iso);
  }

  protected buscar(texto: string): void {
    this.buscador.buscar(texto);
  }

  protected cambiarPlan(u: UsuarioAdmin, plan: PlanSuscripcion): void {
    this.accion(
      u.id,
      this.servicio.cambiarPlan(u.id, plan),
      `${u.nombre} ahora tiene el plan ${plan} (cortesía, sin cobro).`,
    );
  }

  protected cambiarEstado(u: UsuarioAdmin, activo: boolean): void {
    this.accion(
      u.id,
      this.servicio.cambiarEstado(u.id, activo),
      activo ? `Reactivaste la cuenta de ${u.nombre}.` : `Suspendiste la cuenta de ${u.nombre}.`,
    );
  }

  protected ejecutarConfirmado(u: UsuarioAdmin): void {
    const accion = this.confirmar()?.accion;
    this.confirmar.set(null);
    if (accion === 'suspender') this.cambiarEstado(u, false);
    else if (accion === 'cancelar')
      this.accion(
        u.id,
        this.servicio.cancelarSuscripcion(u.id),
        `Cancelaste la suscripción de ${u.nombre}.`,
      );
    else if (accion === 'eliminar')
      this.accion(u.id, this.servicio.eliminar(u.id), `Eliminaste la cuenta de ${u.nombre}.`);
  }

  protected verActividad(u: UsuarioAdmin): void {
    if (this.actividad()?.id === u.id) {
      this.actividad.set(null);
      return;
    }
    this.servicio.actividad(u.id).subscribe({
      next: (avisos) => this.actividad.set({ id: u.id, avisos }),
      error: (e) => this.avisos.error(e),
    });
  }

  private accion(id: number, peticion: Observable<void>, exito: string): void {
    this.ocupado.set(id);
    peticion.subscribe({
      next: () => {
        this.ocupado.set(null);
        this.avisos.exito(exito);
        this.recargas.update((n) => n + 1);
      },
      error: (e) => {
        this.ocupado.set(null);
        this.avisos.error(e);
        this.recargas.update((n) => n + 1);
      },
    });
  }
}
