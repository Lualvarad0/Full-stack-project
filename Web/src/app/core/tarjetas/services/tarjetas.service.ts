import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import {
  EstadoCuenta,
  EstadoCuentaRequest,
  Tarjeta,
  TarjetaRequest,
  Tarjetas,
} from '../models/tarjetas.model';

/** API de tarjetas de crédito y estados de cuenta (/api/tarjetas). */
@Injectable({ providedIn: 'root' })
export class TarjetasService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/tarjetas`;

  listar(): Observable<Tarjetas> {
    return this.http.get<Tarjetas>(this.url);
  }

  crear(tarjeta: TarjetaRequest): Observable<Tarjeta> {
    return this.http.post<Tarjeta>(this.url, tarjeta);
  }

  eliminar(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }

  /** Sube el PDF del estado de cuenta (y su clave, si tiene); lo leído queda por confirmar. */
  subirEstadoCuenta(tarjetaId: number, archivo: File, clave: string | null): Observable<EstadoCuenta> {
    const form = new FormData();
    form.append('archivo', archivo);
    if (clave) form.append('clave', clave);
    return this.http.post<EstadoCuenta>(`${this.url}/${tarjetaId}/estado-cuenta`, form);
  }

  registrarManual(tarjetaId: number, estado: EstadoCuentaRequest): Observable<EstadoCuenta> {
    return this.http.post<EstadoCuenta>(`${this.url}/${tarjetaId}/estados`, estado);
  }

  confirmar(estadoId: number, estado: EstadoCuentaRequest): Observable<EstadoCuenta> {
    return this.http.put<EstadoCuenta>(`${this.url}/estados/${estadoId}`, estado);
  }

  descartar(estadoId: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/estados/${estadoId}`);
  }
}
