import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '@env/environment';
import { FacturaEmitida } from '../models/facturacion.model';
@Injectable({ providedIn: 'root' })
export class ComplementariosService {
  private readonly http = inject(HttpClient); private readonly url = `${environment.apiUrl}/facturacion/complementarios`;
  credito(facturaId: number, motivo: string, detalles: { detalleId: number; cantidad: number }[]) { return this.http.post<FacturaEmitida>(`${this.url}/nota-credito`, { facturaId, motivo, detalles }); }
  retencion(comprobanteId: number, codigoSustento: string, detalles: { codigoImpuesto: string; codigoRetencion: string; baseImponible: number; porcentaje: number }[]) { return this.http.post<FacturaEmitida>(`${this.url}/retencion`, { comprobanteId, codigoSustento, detalles }); }
}
