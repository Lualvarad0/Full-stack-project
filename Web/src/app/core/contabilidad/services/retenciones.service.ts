import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '@env/environment';
export interface RetencionResumen { id: number; numero: string; fechaEmision: string; periodoFiscal: string; direccion: 'Recibido' | 'Emitido'; emisor: string; retenido: string; renta: number; iva: number; revisada: boolean; }
export interface ResultadoRetenciones { importadas: number; existentes: number; problemas: string[]; }
export interface RetencionDetalle { id: number; codigoImpuesto: string; codigoRetencion: string; baseImponible: number; porcentaje: number; valor: number; documentoSustento: string | null; comprobanteId: number | null; }
export interface Retencion { id: number; numero: string; fechaEmision: string; periodoFiscal: string; razonSocialEmisor: string; razonSocialRetenido: string; detalles: RetencionDetalle[]; revisadoEn: string | null; }
@Injectable({ providedIn: 'root' })
export class RetencionesService {
  private readonly http = inject(HttpClient); private readonly url = `${environment.apiUrl}/contabilidad/retenciones`;
  listar(anio: number, mes: number) { return this.http.get<RetencionResumen[]>(this.url, { params: new HttpParams().set('anio', anio).set('mes', mes) }); }
  obtener(id: number) { return this.http.get<Retencion>(`${this.url}/${id}`); }
  importar(texto: string) { return this.http.post<ResultadoRetenciones>(`${this.url}/importar`, { texto }); }
  archivos(archivos: File[]) { const form = new FormData(); archivos.forEach(a => form.append('archivos', a, a.name)); return this.http.post<ResultadoRetenciones>(`${this.url}/importar/archivos`, form); }
  revisar(id: number) { return this.http.post<void>(`${this.url}/${id}/revisar`, {}); }
  xml(id: number) { return this.http.get(`${this.url}/${id}/xml`, { responseType: 'blob' }); }
  pdf(id: number) { return this.http.get(`${this.url}/${id}/pdf`, { responseType: 'blob' }); }
}
