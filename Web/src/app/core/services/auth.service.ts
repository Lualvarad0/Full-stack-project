import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '@env/environment';
import { AuthResponse, RegistroRequest, Sesion, Usuario } from '@core/models';

export const CLAVE_SESION = 'sesion';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly sesion = signal<Sesion | null>(leerSesion());

  readonly usuario = computed(() => this.sesion()?.usuario ?? null);
  readonly token = computed(() => this.sesion()?.token ?? null);
  /** Plan de pago (o administrador): ingresos, clientes y facturación electrónica. */
  readonly esProfesional = computed(() => {
    const u = this.usuario();
    return !!u && (u.rol === 'admin' || u.plan !== 'Personal');
  });

  estaAutenticado(): boolean {
    const sesion = this.sesion();
    if (!sesion) return false;
    if (new Date(sesion.expiraEn).getTime() <= Date.now()) {
      this.cerrarSesion(false);
      return false;
    }
    return true;
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/login`, { email, password })
      .pipe(tap((r) => this.guardar(r)));
  }

  registrar(datos: RegistroRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/register`, datos)
      .pipe(tap((r) => this.guardar(r)));
  }

  /** Actualiza los datos del usuario en la sesión (por ejemplo, tras cambiar de plan). */
  actualizarUsuario(usuario: Usuario): void {
    const sesion = this.sesion();
    if (sesion) this.guardar({ token: sesion.token, expiraEn: sesion.expiraEn, usuario });
  }

  cerrarSesion(redirigir = true): void {
    this.sesion.set(null);
    try {
      localStorage.removeItem(CLAVE_SESION);
    } catch {
      /* almacenamiento no disponible */
    }
    if (redirigir) void this.router.navigate(['/login']);
  }

  private guardar(r: AuthResponse): void {
    const sesion: Sesion = { token: r.token, expiraEn: r.expiraEn, usuario: r.usuario };
    this.sesion.set(sesion);
    try {
      localStorage.setItem(CLAVE_SESION, JSON.stringify(sesion));
    } catch {
      /* la sesión sigue en memoria */
    }
  }
}

export function leerSesion(): Sesion | null {
  try {
    const crudo = localStorage.getItem(CLAVE_SESION);
    return crudo ? (JSON.parse(crudo) as Sesion) : null;
  } catch {
    return null;
  }
}
