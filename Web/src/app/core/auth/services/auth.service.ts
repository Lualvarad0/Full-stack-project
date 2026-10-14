import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Observable, tap, timeout } from 'rxjs';
import { environment } from '@env/environment';
import { AuthResponse, RegistroRequest, Sesion, Usuario } from '../models/auth.model';
export const CLAVE_SESION = 'sesion';
/** Contribuyente elegido en esta pestaña ("usuario:contribuyente"), para que recargar o editar la URL no cambie de perfil. */
export const CLAVE_CONTRIBUYENTE = 'contribuyente';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly sesion = signal<Sesion | null>(leerSesion());
  private readonly contribuyente = signal<number | null>(leerContribuyente(this.sesion()?.usuario.id));

  readonly usuario = computed(() => this.sesion()?.usuario ?? null);
  readonly token = computed(() => this.sesion()?.token ?? null);
  readonly cambiosSesion = toObservable(this.token);
  readonly contribuyenteId = computed(() => this.contribuyente() ?? this.usuario()?.id ?? null);
  readonly claveDatos = computed(() => `${this.usuario()?.id ?? ''}:${this.contribuyenteId() ?? ''}`);
  readonly cambiosDatos = toObservable(this.claveDatos);
  seleccionarContribuyente(id: number | null): void {
    this.contribuyente.set(id);
    const usuario = this.usuario()?.id;
    try {
      if (id && usuario && id !== usuario) sessionStorage.setItem(CLAVE_CONTRIBUYENTE, `${usuario}:${id}`);
      else sessionStorage.removeItem(CLAVE_CONTRIBUYENTE);
    } catch {
      /* la elección sigue en memoria */
    }
  }
  /** Plan de pago (o administrador): ingresos, clientes y facturación electrónica. */
  readonly esProfesional = computed(() => {
    const u = this.usuario();
    return !!u && (u.rol === 'admin' || u.plan !== 'Personal');
  });
  /** Pantalla al entrar: el plan gratuito siempre va a Mis cuentas; el resto, la que eligió en Configuración. */
  readonly rutaInicio = computed(() =>
    !this.esProfesional() || this.usuario()?.paginaInicio === 'Cuentas' ? '/cuentas' : '/resumen',
  );

  estaAutenticado(): boolean {
    const sesion = this.sesion();
    if (!sesion) return false;
    const expiracion = Date.parse(sesion.expiraEn);
    if (!Number.isFinite(expiracion) || expiracion <= Date.now()) {
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
    if (sesion && sesion.usuario.id === usuario.id)
      this.guardar({ token: sesion.token, expiraEn: sesion.expiraEn, usuario });
  }

  cerrarSesion(redirigir = true): void {
    this.contribuyente.set(null);
    this.sesion.set(null);
    try {
      sessionStorage.removeItem(CLAVE_SESION);
      sessionStorage.removeItem(CLAVE_CONTRIBUYENTE);
      localStorage.removeItem(CLAVE_SESION);
    } catch {
      /* almacenamiento no disponible */
    }
    if (redirigir) void this.router.navigate(['/login']);
  }

  /** Cierre explícito: revoca el JWT en el servidor antes de limpiar esta pestaña. */
  salir(): void {
    const token = this.token();
    if (!token) { this.cerrarSesion(); return; }
    const terminar = () => { if (this.token() === token) this.cerrarSesion(); };
    this.http.delete(`${environment.apiUrl}/cuenta/seguridad/sesiones/actual`).pipe(timeout(5000)).subscribe({ next: terminar, error: terminar });
  }

  private guardar(r: AuthResponse): void {
    // Otra cuenta en la misma pestaña no hereda el contribuyente elegido por la anterior.
    if (this.usuario()?.id !== r.usuario.id) this.seleccionarContribuyente(null);
    const sesion: Sesion = { token: r.token, expiraEn: r.expiraEn, usuario: r.usuario };
    this.sesion.set(sesion);
    try {
      sessionStorage.setItem(CLAVE_SESION, JSON.stringify(sesion));
      localStorage.removeItem(CLAVE_SESION);
    } catch {
      /* la sesión sigue en memoria */
    }
  }
}

/** Solo vale para el mismo usuario; la cartera confirma después que siga autorizado. */
function leerContribuyente(usuario: number | undefined): number | null {
  try {
    const [dueno, id] = (sessionStorage.getItem(CLAVE_CONTRIBUYENTE) ?? '').split(':').map(Number);
    return usuario && dueno === usuario && Number.isInteger(id) && id > 0 ? id : null;
  } catch {
    return null;
  }
}

export function leerSesion(): Sesion | null {
  try {
    // El token anterior guardado para todo el navegador deja de reutilizarse entre pestañas.
    localStorage.removeItem(CLAVE_SESION);
    const crudo = sessionStorage.getItem(CLAVE_SESION);
    return crudo ? (JSON.parse(crudo) as Sesion) : null;
  } catch {
    return null;
  }
}
