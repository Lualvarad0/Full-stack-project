import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '@env/environment';
import {
  DatosEmisor,
  DatosEmisorRequest,
  EmitirFacturaRequest,
  FacturaEmitida,
  FirmaElectronica,
  ServicioFacturable,
  ServicioRequest,
} from '../models/facturacion.model';
@Injectable({ providedIn: 'root' })
export class FacturacionService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/facturacion`;

  /** null si aún no subió su firma. */
  firma(): Observable<FirmaElectronica | null> {
    return this.http
      .get<FirmaElectronica | null>(`${this.url}/firma`, { observe: 'response' })
      .pipe(map((r) => r.body ?? null));
  }

  subirFirma(archivo: File, clave: string): Observable<FirmaElectronica> {
    const form = new FormData();
    form.append('archivo', archivo, archivo.name);
    form.append('clave', clave);
    return this.http.post<FirmaElectronica>(`${this.url}/firma`, form);
  }

  eliminarFirma(): Observable<void> {
    return this.http.delete<void>(`${this.url}/firma`);
  }

  datos(): Observable<DatosEmisor> {
    return this.http.get<DatosEmisor>(`${this.url}/datos`);
  }

  guardarDatos(datos: DatosEmisorRequest): Observable<DatosEmisor> {
    return this.http.put<DatosEmisor>(`${this.url}/datos`, datos);
  }

  facturas(): Observable<FacturaEmitida[]> {
    return this.http.get<FacturaEmitida[]>(`${this.url}/facturas`);
  }

  factura(id: number): Observable<FacturaEmitida> {
    return this.http.get<FacturaEmitida>(`${this.url}/facturas/${id}`);
  }

  emitir(factura: EmitirFacturaRequest): Observable<FacturaEmitida> {
    return this.http.post<FacturaEmitida>(`${this.url}/facturas`, factura);
  }

  reintentar(id: number): Observable<FacturaEmitida> {
    return this.http.post<FacturaEmitida>(`${this.url}/facturas/${id}/reintentar`, null);
  }

  /** Solo facturas que nunca se autorizaron (sin enviar, devueltas o no autorizadas). */
  eliminar(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/facturas/${id}`);
  }

  /** Valida el acceso para gestionar el trámite en SRI en Línea; conserva el estado. */
  anular(id: number): Observable<FacturaEmitida> {
    return this.http.post<FacturaEmitida>(`${this.url}/facturas/${id}/anular`, null);
  }

  servicios(): Observable<ServicioFacturable[]> {
    return this.http.get<ServicioFacturable[]>(`${this.url}/servicios`);
  }

  guardarServicio(servicio: ServicioRequest, id?: number): Observable<ServicioFacturable> {
    return id
      ? this.http.put<ServicioFacturable>(`${this.url}/servicios/${id}`, servicio)
      : this.http.post<ServicioFacturable>(`${this.url}/servicios`, servicio);
  }

  eliminarServicio(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/servicios/${id}`);
  }

  descargarXml(id: number): Observable<Blob> {
    return this.http.get(`${this.url}/facturas/${id}/xml`, { responseType: 'blob' });
  }

  descargarPdf(id: number): Observable<Blob> {
    return this.http.get(`${this.url}/facturas/${id}/pdf`, { responseType: 'blob' });
  }
}
