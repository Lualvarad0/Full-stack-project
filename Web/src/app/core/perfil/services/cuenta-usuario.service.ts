import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '@env/environment';
import { AuthService, Usuario } from '@core/auth';
import {
  ConfiguracionSistema,
  DatosPersonales,
  DatosPersonalesRequest,
} from '../models/cuenta-usuario.model';

/** Datos personales y configuración del sistema del propio usuario (/api/cuenta). */
@Injectable({ providedIn: 'root' })
export class CuentaUsuarioService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly url = `${environment.apiUrl}/cuenta`;

  datos(): Observable<DatosPersonales> {
    return this.http.get<DatosPersonales>(`${this.url}/datos`);
  }

  /** Guarda y actualiza el nombre que muestra la sesión. */
  guardarDatos(datos: DatosPersonalesRequest): Observable<{ datos: DatosPersonales; usuario: Usuario }> {
    return this.http
      .put<{ datos: DatosPersonales; usuario: Usuario }>(`${this.url}/datos`, datos)
      .pipe(tap((r) => this.auth.actualizarUsuario(r.usuario)));
  }

  configuracion(): Observable<ConfiguracionSistema> {
    return this.http.get<ConfiguracionSistema>(`${this.url}/configuracion`);
  }

  /** Guarda y la refleja en la sesión (la página de inicio y el mes se usan al entrar). */
  guardarConfiguracion(config: ConfiguracionSistema): Observable<ConfiguracionSistema> {
    return this.http.put<ConfiguracionSistema>(`${this.url}/configuracion`, config).pipe(
      tap((c) => {
        const u = this.auth.usuario();
        if (u) this.auth.actualizarUsuario({ ...u, paginaInicio: c.paginaInicio, mesAlEntrar: c.mesAlEntrar });
      }),
    );
  }
}
