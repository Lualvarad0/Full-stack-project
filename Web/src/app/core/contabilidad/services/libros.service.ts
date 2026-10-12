import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@env/environment';
export interface CuentaContable {
  id: number;
  codigo: string;
  nombre: string;
  tipo: string;
  activa: boolean;
}
export interface LineaAsiento {
  cuentaId: number;
  codigo?: string;
  nombre?: string;
  debe: number;
  haber: number;
}
export interface Asiento {
  id: number;
  fecha: string;
  concepto: string;
  estado: string;
  comprobanteId: number | null;
  pagoComprobanteId: number | null;
  lineas: LineaAsiento[];
}
export interface SaldoCuenta extends CuentaContable {
  debe: number;
  haber: number;
  saldo: number;
}
export interface EstadosContables {
  activos: number;
  pasivos: number;
  patrimonio: number;
  ingresos: number;
  gastos: number;
  resultado: number;
  diferenciaBalance: number;
  cuentas: SaldoCuenta[];
}
export interface Mayor {
  asientoId: number;
  fecha: string;
  concepto: string;
  debe: number;
  haber: number;
  saldo: number;
}
@Injectable({ providedIn: 'root' })
export class LibrosService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/contabilidad/libros`;
  cuentas() {
    return this.http.get<CuentaContable[]>(`${this.url}/cuentas`);
  }
  inicializar() {
    return this.http.post<void>(`${this.url}/cuentas/base`, {});
  }
  cuenta(r: { codigo: string; nombre: string; tipo: string }) {
    return this.http.post<{ id: number }>(`${this.url}/cuentas`, r);
  }
  asientos(anio: number, mes: number) {
    return this.http.get<Asiento[]>(`${this.url}/asientos`, { params: { anio, mes } });
  }
  guardar(r: { fecha: string; concepto: string; lineas: LineaAsiento[] }, id?: number) {
    return id
      ? this.http.put<{ id: number }>(`${this.url}/asientos/${id}`, r)
      : this.http.post<{ id: number }>(`${this.url}/asientos`, r);
  }
  contabilizar(id: number) {
    return this.http.post<void>(`${this.url}/asientos/${id}/contabilizar`, {});
  }
  anular(id: number, motivo: string) {
    return this.http.post<void>(`${this.url}/asientos/${id}/anular`, { motivo });
  }
  desdeComprobante(id: number) {
    return this.http.post<{ id: number }>(`${this.url}/comprobantes/${id}`, {});
  }
  desdePago(id: number) {
    return this.http.post<{ id: number }>(`${this.url}/pagos/${id}`, {});
  }
  estados(desde: string, hasta: string) {
    return this.http.get<EstadosContables>(`${this.url}/estados`, { params: { desde, hasta } });
  }
  mayor(id: number, desde: string, hasta: string) {
    return this.http.get<Mayor[]>(`${this.url}/cuentas/${id}/mayor`, { params: { desde, hasta } });
  }
}
