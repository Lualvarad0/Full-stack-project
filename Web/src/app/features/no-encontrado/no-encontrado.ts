import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Location } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { LucideArrowLeft, LucideHouse } from '@lucide/angular';
import { AuthService } from '@core/auth';
import { Marca } from '@shared/components';

interface Sugerencia {
  texto: string;
  ruta: string;
  fragmento?: string;
}

/**
 * Página 404: cualquier dirección que no existe en la app. La ilustración (un hilo enredado que forma el
 * "0", una lupa que busca y una factura perdida) es animada; con "reducir movimiento" queda quieta.
 */
@Component({
  selector: 'app-no-encontrado',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Marca, LucideHouse, LucideArrowLeft],
  styleUrl: './no-encontrado.css',
  templateUrl: './no-encontrado.html',
})
export class NoEncontrado {
  private readonly auth = inject(AuthService);
  private readonly location = inject(Location);
  /** La dirección que se escribió, para mostrarla tal cual (sin parámetros). */
  protected readonly ruta = inject(Router).url.split(/[?#]/)[0];

  private readonly conSesion = computed(() => !!this.auth.usuario());
  protected readonly inicio = computed(() => (this.conSesion() ? this.auth.rutaInicio() : '/'));
  /** Volver solo si se llegó desde otra página de la app (no al abrir el enlace en una pestaña nueva). */
  protected readonly puedeVolver = typeof history !== 'undefined' && history.length > 1;

  protected readonly sugerencias = computed<Sugerencia[]>(() => {
    if (!this.conSesion())
      return [
        { texto: 'Planes', ruta: '/', fragmento: 'planes' },
        { texto: 'Iniciar sesión', ruta: '/login' },
        { texto: 'Crear cuenta', ruta: '/registro' },
      ];
    return [
      ...(this.auth.esProfesional() ? [{ texto: 'Resumen', ruta: '/resumen' }] : []),
      { texto: 'Mis cuentas', ruta: '/cuentas' },
      { texto: 'Configuración', ruta: '/configuracion' },
    ];
  });

  protected volver(): void {
    this.location.back();
  }
}
