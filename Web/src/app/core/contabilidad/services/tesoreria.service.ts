import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '@env/environment';
export interface SaldoComprobante {
  id: number;
  numero: string;
  tercero: string;
  direccion: 'Recibido' | 'Emitido';
  fecha: string;
  vencimiento: string | null;
  total: number;
  notas: number;
  aplicado: number;
  saldo: number;
  retenciones: number;
}
export interface Pago {
  id: number;
  comprobanteId: number;
  numero: string;
  fecha: string;
  importe: number;
  tipo: 'Pago' | 'Reembolso';
  medio: string;
  referencia: string | null;
  anulado: boolean;
  pendienteConciliar: number;
  entrada: boolean;
}
export interface CuentaBancaria {
  id: number;
  nombre: string;
  entidad: string;
  ultimosDigitos: string;
  saldoInicial: number;
  fechaInicial: string;
  saldo: number;
  activa: boolean;
}
export interface MovimientoBanco {
  id: number;
  fecha: string;
  concepto: string;
  referencia: string | null;
  importe: number;
  conciliado: number;
  revisadoSinFactura: boolean;
}
export const hoyEcuador = () => new Date(Date.now() - 5 * 3600000).toISOString().slice(0, 10);
@Injectable({ providedIn: 'root' })
export class TesoreriaService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/contabilidad/tesoreria`;
  saldos() {
    return this.http.get<SaldoComprobante[]>(`${this.url}/saldos`);
  }
  pagos() {
    return this.http.get<Pago[]>(`${this.url}/pagos`);
  }
  registrar(request: {
    comprobanteId: number;
    fecha: string;
    importe: number;
    tipo: string;
    medio: string;
    referencia: string | null;
    cuentaBancariaId: number | null;
  }) {
    return this.http.post<{ id: number }>(`${this.url}/pagos`, request);
  }
  anular(id: number, motivo: string) {
    return this.http.post<void>(`${this.url}/pagos/${id}/anular`, { motivo });
  }
  vencimiento(id: number, fecha: string) {
    return this.http.put<void>(`${this.url}/saldos/${id}/vencimiento`, { fecha });
  }
  cuentas() {
    return this.http.get<CuentaBancaria[]>(`${this.url}/cuentas`);
  }
  crearCuenta(request: {
    nombre: string;
    entidad: string;
    numero: string;
    saldoInicial: number;
    fechaInicial: string;
  }) {
    return this.http.post<{ id: number }>(`${this.url}/cuentas`, request);
  }
  movimientos(id: number, anio: number, mes: number) {
    return this.http.get<MovimientoBanco[]>(`${this.url}/cuentas/${id}/movimientos`, {
      params: { anio, mes },
    });
  }
  movimiento(
    id: number,
    request: {
      idExterno: string;
      fecha: string;
      concepto: string;
      referencia: string | null;
      importe: number;
    },
  ) {
    return this.http.post<{ id: number }>(`${this.url}/cuentas/${id}/movimientos`, request);
  }
  importar(id: number, archivo: File) {
    const form = new FormData();
    form.append('archivo', archivo, archivo.name);
    return this.http.post<{ importadas: number; duplicadas: number }>(
      `${this.url}/cuentas/${id}/importar`,
      form,
    );
  }
  conciliar(id: number, pagos: { pagoId: number; importe: number }[]) {
    return this.http.post<void>(`${this.url}/movimientos/${id}/conciliar`, { pagos });
  }
  desconciliar(id: number, motivo: string) {
    return this.http.post<void>(`${this.url}/movimientos/${id}/desconciliar`, { motivo });
  }
  revisarOtro(id: number, motivo: string) {
    return this.http.post<void>(`${this.url}/movimientos/${id}/revisar-otro`, { motivo });
  }
}
