import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { CuentaPersonal, CuentaPersonalRequest, MesCuentas } from '../models/cuentas.model';

/** API de Mis cuentas (/api/cuentas). */
@Injectable({ providedIn: 'root' })
export class CuentasService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/cuentas`;

  mes(anio: number, mes: number): Observable<MesCuentas> {
    return this.http.get<MesCuentas>(this.url, { params: { anio, mes } });
  }

  guardar(cuenta: CuentaPersonalRequest, id?: number): Observable<CuentaPersonal> {
    return id
      ? this.http.put<CuentaPersonal>(`${this.url}/${id}`, cuenta)
      : this.http.post<CuentaPersonal>(this.url, cuenta);
  }

  eliminar(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }

  /** Marca la cuenta como pagada el día indicado (yyyy-MM-dd). */
  pagar(id: number, fecha: string): Observable<CuentaPersonal> {
    return this.http.put<CuentaPersonal>(`${this.url}/${id}/pago`, { fecha });
  }

  quitarPago(id: number): Observable<CuentaPersonal> {
    return this.http.delete<CuentaPersonal>(`${this.url}/${id}/pago`);
  }

  guardarIngreso(anio: number, mes: number, monto: number): Observable<void> {
    return this.http.put<void>(`${this.url}/ingreso`, { anio, mes, monto });
  }

  copiarMesAnterior(anio: number, mes: number): Observable<{ copiadas: number }> {
    return this.http.post<{ copiadas: number }>(`${this.url}/copiar-mes-anterior`, { anio, mes });
  }
}
