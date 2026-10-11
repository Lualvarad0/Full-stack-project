import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { AvisoCuenta, EstadoSistema, MetricasAdmin, PagoAdmin, PlanSuscripcion, UsuarioAdmin } from '@core/models';

/** API del panel de superadministrador (rol admin). */
@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/admin`;

  metricas(): Observable<MetricasAdmin> {
    return this.http.get<MetricasAdmin>(`${this.url}/metricas`);
  }

  usuarios(q: string, plan: PlanSuscripcion | ''): Observable<UsuarioAdmin[]> {
    let params = new HttpParams();
    if (q.trim()) params = params.set('q', q.trim());
    if (plan) params = params.set('plan', plan);
    return this.http.get<UsuarioAdmin[]>(`${this.url}/usuarios`, { params });
  }

  actividad(id: number): Observable<AvisoCuenta[]> {
    return this.http.get<AvisoCuenta[]>(`${this.url}/usuarios/${id}/actividad`);
  }

  cambiarPlan(id: number, plan: PlanSuscripcion): Observable<void> {
    return this.http.put<void>(`${this.url}/usuarios/${id}/plan`, { plan });
  }

  cambiarEstado(id: number, activo: boolean): Observable<void> {
    return this.http.put<void>(`${this.url}/usuarios/${id}/estado`, { activo });
  }

  cancelarSuscripcion(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/usuarios/${id}/suscripcion`);
  }

  eliminar(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/usuarios/${id}`);
  }

  pagos(): Observable<PagoAdmin[]> {
    return this.http.get<PagoAdmin[]>(`${this.url}/pagos`);
  }

  sistema(): Observable<EstadoSistema> {
    return this.http.get<EstadoSistema>(`${this.url}/sistema`);
  }
}
