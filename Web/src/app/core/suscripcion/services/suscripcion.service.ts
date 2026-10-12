import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable, tap } from 'rxjs';
import { environment } from '@env/environment';
import {
  CatalogoPagos,
  Periodicidad,
  PlanSuscripcion,
  ResultadoSuscripcion,
  Suscripcion,
  UsoPlan,
} from '../models/suscripcion.model';
import { AuthService } from '../../auth/services/auth.service';
/** Planes de pago con Kushki: precios, suscribirse con el token de la tarjeta y cancelar. */
@Injectable({ providedIn: 'root' })
export class SuscripcionService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly url = `${environment.apiUrl}/suscripcion`;

  catalogo(): Observable<CatalogoPagos> {
    return this.http.get<CatalogoPagos>(`${this.url}/planes`);
  }

  uso(): Observable<UsoPlan> {
    return this.http.get<UsoPlan>(`${this.url}/uso`);
  }

  /** null si está en el plan Personal. */
  actual(): Observable<Suscripcion | null> {
    return this.http
      .get<Suscripcion | null>(this.url, { observe: 'response' })
      .pipe(map((r) => r.body ?? null));
  }

  suscribir(
    plan: PlanSuscripcion,
    periodicidad: Periodicidad,
    token: string,
    marcaTarjeta?: string,
    ultimosDigitos?: string,
  ): Observable<Suscripcion | null> {
    return this.http
      .post<ResultadoSuscripcion>(this.url, {
        plan,
        periodicidad,
        token,
        marcaTarjeta,
        ultimosDigitos,
      })
      .pipe(
        tap((r) => this.auth.actualizarUsuario(r.usuario)),
        map((r) => r.suscripcion),
      );
  }

  cancelar(): Observable<void> {
    return this.http.delete<ResultadoSuscripcion>(this.url).pipe(
      tap((r) => this.auth.actualizarUsuario(r.usuario)),
      map(() => undefined),
    );
  }
}
