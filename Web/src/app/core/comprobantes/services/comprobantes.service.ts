import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import {
  CategoriaGasto,
  CategoriaIngreso,
  FinalidadComprobante,
  TratamientoIva,
  ClienteFacturado,
  Comprobante,
  ComprobanteLista,
  FiltroComprobantes,
  ImportacionResponse,
  Proveedor,
  ResumenPeriodo,
} from '../models/comprobante.model';
@Injectable({ providedIn: 'root' })
export class ComprobantesService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/comprobantes`;

  listar(filtro: FiltroComprobantes): Observable<ComprobanteLista[]> {
    let params = new HttpParams();
    for (const [clave, valor] of Object.entries(filtro)) {
      if (valor !== undefined && valor !== null && valor !== '')
        params = params.set(clave, String(valor));
    }
    return this.http.get<ComprobanteLista[]>(this.url, { params });
  }

  obtener(id: number): Observable<Comprobante> {
    return this.http.get<Comprobante>(`${this.url}/${id}`);
  }

  descargarXml(id: number): Observable<Blob> {
    return this.http.get(`${this.url}/${id}/xml`, { responseType: 'blob' });
  }

  eliminar(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }

  cambiarCategoria(
    id: number,
    categoria: CategoriaGasto,
    aplicarAlProveedor: boolean,
  ): Observable<Comprobante> {
    return this.http.put<Comprobante>(`${this.url}/${id}/categoria`, {
      categoria,
      aplicarAlProveedor,
    });
  }

  cambiarCategoriaIngreso(id: number, categoria: CategoriaIngreso): Observable<Comprobante> {
    return this.http.put<Comprobante>(`${this.url}/${id}/categoria-ingreso`, { categoria });
  }
  clasificarPartidas(id: number, partidas: { id: number; finalidad: FinalidadComprobante; categoria: CategoriaGasto | null; categoriaIngreso: CategoriaIngreso | null; tratamientoIva: TratamientoIva }[]): Observable<Comprobante> {
    return this.http.put<Comprobante>(`${this.url}/${id}/partidas`, { partidas });
  }
  finalidad(id: number, finalidad: FinalidadComprobante, tratamientoIva: TratamientoIva): Observable<Comprobante> {
    return this.http.put<Comprobante>(`${this.url}/${id}/finalidad`, { finalidad, tratamientoIva });
  }

  importarArchivos(archivos: File[]): Observable<ImportacionResponse> {
    const form = new FormData();
    for (const archivo of archivos) form.append('archivos', archivo, archivo.name);
    return this.http.post<ImportacionResponse>(`${this.url}/importar`, form);
  }

  importarTexto(texto: string): Observable<ImportacionResponse> {
    return this.http.post<ImportacionResponse>(`${this.url}/importar/claves`, { texto });
  }

  resumen(anio: number, mes?: number): Observable<ResumenPeriodo> {
    let params = new HttpParams().set('anio', anio);
    if (mes) params = params.set('mes', mes);
    return this.http.get<ResumenPeriodo>(`${this.url}/resumen`, { params });
  }

  proveedores(): Observable<Proveedor[]> {
    return this.http.get<Proveedor[]>(`${this.url}/proveedores`);
  }

  /** Clientes de las facturas emitidas; sin año/mes devuelve el histórico. */
  clientes(anio?: number, mes?: number): Observable<ClienteFacturado[]> {
    let params = new HttpParams();
    if (anio) params = params.set('anio', anio);
    if (anio && mes) params = params.set('mes', mes);
    return this.http.get<ClienteFacturado[]>(`${this.url}/clientes`, { params });
  }

  categorizarProveedor(ruc: string, categoria: CategoriaGasto): Observable<void> {
    return this.http.put<void>(`${this.url}/proveedores/${ruc}/categoria`, { categoria });
  }
}
