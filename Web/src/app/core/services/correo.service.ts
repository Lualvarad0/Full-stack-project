import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { AvisoCuenta, Buzon } from '@core/models';

@Injectable({ providedIn: 'root' })
export class CorreoService {
  private readonly http = inject(HttpClient);

  buzon(): Observable<Buzon> {
    return this.http.get<Buzon>(`${environment.apiUrl}/correo`);
  }

  regenerar(): Observable<Buzon> {
    return this.http.post<Buzon>(`${environment.apiUrl}/correo/regenerar`, null);
  }

  avisos(): Observable<AvisoCuenta[]> {
    return this.http.get<AvisoCuenta[]>(`${environment.apiUrl}/avisos`);
  }
}
