import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '@env/environment';
export interface RevisionPeriodo { anio: number; mes: number; estado: 'Abierto' | 'Cerrado'; comprobantes: number; pendientes: number; facturasSriPendientes: number; cerradoEn: string | null; }
export interface AuditoriaContable { id: number; actorId: number; fecha: string; accion: string; entidad: string; referencia: string; detalle: string; }
@Injectable({ providedIn: 'root' })
export class PeriodosContablesService {
  private readonly http = inject(HttpClient); private readonly url = `${environment.apiUrl}/contabilidad`;
  revisar(anio: number, mes: number) { return this.http.get<RevisionPeriodo>(`${this.url}/periodos/${anio}/${mes}`); }
  cerrar(anio: number, mes: number) { return this.http.post<void>(`${this.url}/periodos/${anio}/${mes}/cerrar`, {}); }
  reabrir(anio: number, mes: number, motivo: string) { return this.http.post<void>(`${this.url}/periodos/${anio}/${mes}/reabrir`, { motivo }); }
  auditoria() { return this.http.get<AuditoriaContable[]>(`${this.url}/auditoria`); }
}
