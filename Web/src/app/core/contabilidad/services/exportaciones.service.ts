import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '@env/environment';
export interface ValidacionAts {
  valido: boolean;
  esquema: string;
  errores: string[];
  alcance: string;
}
@Injectable({ providedIn: 'root' })
export class ExportacionesService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/contabilidad/exportaciones`;
  descargar(
    tipo: 'comprobantes' | 'balance' | 'paquete',
    mes: number,
    meses: number,
    formato = 'xlsx',
  ) {
    return this.http.get(`${this.url}/${tipo}`, {
      params: { anio: 2026, mes, meses, formato },
      responseType: 'blob',
    });
  }
  validarAts(archivo: File) {
    const body = new FormData();
    body.append('archivo', archivo);
    return this.http.post<ValidacionAts>(`${this.url}/validar-ats`, body);
  }
}
