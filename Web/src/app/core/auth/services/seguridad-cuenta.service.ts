import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';

export interface SesionCuenta { id: string; creadaEn: string; expiraEn: string; dispositivo: string | null; actual: boolean; }
export interface SeguridadCuenta { correoVerificado: boolean; correo: string; }
@Injectable({ providedIn: 'root' })
export class SeguridadCuentaService {
  private readonly http = inject(HttpClient);
  private readonly auth = `${environment.apiUrl}/auth`;
  private readonly cuenta = `${environment.apiUrl}/cuenta/seguridad`;
  recuperar(email: string) { return this.http.post<{ mensaje: string }>(`${this.auth}/recuperar-clave`, { email }); }
  restablecer(token: string, password: string, confirmacion: string) { return this.http.post<void>(`${this.auth}/restablecer-clave`, { token, password, confirmacion }); }
  verificar(token: string) { return this.http.post<void>(`${this.auth}/verificar-correo`, { token }); }
  cambiarClave(actual: string, password: string, confirmacion: string) { return this.http.put<void>(`${this.cuenta}/clave`, { actual, password, confirmacion }); }
  estado() { return this.http.get<SeguridadCuenta>(this.cuenta); }
  enviarVerificacion() { return this.http.post<void>(`${this.cuenta}/verificacion`, {}); }
  sesiones() { return this.http.get<SesionCuenta[]>(`${this.cuenta}/sesiones`); }
  revocar(id: string) { return this.http.delete<void>(`${this.cuenta}/sesiones/${id}`); }
  cerrarOtras() { return this.http.delete<void>(`${this.cuenta}/sesiones/otras`); }
}
