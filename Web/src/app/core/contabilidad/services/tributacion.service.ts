import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '@env/environment';
export interface PerfilObligaciones {
  iva: string;
  retenciones: string;
  ats: string;
  renta: boolean;
  gastosPersonales: boolean;
  sociedad: boolean;
  especial: boolean;
  galapagos: boolean;
  actividades: string;
  feriados: string;
  confirmadoEn: string | null;
}
export interface CargaFamiliar {
  id: number;
  anio: number;
  identificacion: string;
  nombre: string;
  parentesco: string;
  fechaNacimiento: string;
  ingresosGravados: number;
  discapacidad: boolean;
  enfermedadCatastrofica: boolean;
  dependenciaConfirmada: boolean;
}
export interface RubroProyeccion {
  categoria: string;
  importe: number;
}
export interface ParametrosRenta {
  sueldosGravados: number;
  iessPersonal: number;
  retencionesSueldo: number;
  otrosIngresosGeneral: number;
  otrosGastosGeneral: number;
  ingresosRimpeAjuste: number;
  exoneraciones: number;
  anticiposRenta: number;
  beneficioEspecial: boolean;
  galapagos: boolean;
  proyeccion: RubroProyeccion[];
  sustentoAjustes: string;
}
export interface GastosPersonales {
  anio: number;
  version: string;
  cargas: number;
  canasta: number;
  canastas: number;
  tope: number;
  real: number;
  proyectado: number;
  rebajaPotencial: number;
  rebajaProyectada: number;
  rubros: { categoria: string; real: number; proyectado: number }[];
  pendientes: string[];
}
export interface RentaPreparada {
  anio: number;
  version: string;
  regimen: string;
  ingresosFacturados: number;
  gastosNegocio: number;
  ingresosGeneral: number;
  baseGeneral: number;
  ingresosRimpe: number;
  impuestoGeneral: number;
  impuestoRimpe: number;
  rebaja: number;
  retenciones: number;
  anticipos: number;
  pagar: number;
  saldoFavor: number;
  gastosPersonales: GastosPersonales;
  parametros: ParametrosRenta;
  pendientes: string[];
}
export interface ParametrosIva {
  factorCredito: number;
  creditoAnterior: number;
  ajusteDebito: number;
  ajusteCredito: number;
  sustentoAjustes: string;
}
export interface IvaPreparado {
  anio: number;
  mes: number;
  meses: number;
  version: string;
  tarifas: {
    tarifa: string;
    ventasBase: number;
    ventasIva: number;
    comprasBase: number;
    comprasIva: number;
  }[];
  debito: number;
  creditoElegible: number;
  creditoAplicado: number;
  retencionesRecibidas: number;
  retencionesEmitidasIva: number;
  retencionesEmitidasRenta: number;
  pagar: number;
  saldoFavor: number;
  parametros: ParametrosIva;
  pendientes: string[];
}
export interface DocumentoFiscal {
  id: number;
  fecha: string;
  numero: string;
  tercero: string;
  direccion: string;
  finalidad: string;
  subtotal: number;
  iva: number;
  renta: string;
  codigoSustento: string;
  formaPago: string;
  parteRelacionada: boolean;
  observacion: string;
}
export interface RevisionTributo {
  id: number;
  tipo: string;
  anio: number;
  mes: number;
  meses: number;
  estado: string;
  creadoEn: string;
  revisadoEn: string;
  presentadoEn: string | null;
  referenciaPresentacion: string | null;
  desactualizada: boolean;
}
export interface CalendarioTributario {
  obligacion: string;
  periodo: string;
  fecha: string;
  estado: string;
  revisionId: number | null;
}
export interface RetencionesPreparadas {
  irEmitida: number;
  ivaEmitida: number;
  irRecibida: number;
  ivaRecibida: number;
  pendientes: string[];
}
export interface AtsPreparado {
  documentos: DocumentoFiscal[];
  pendientes: string[];
  version: string;
}
@Injectable({ providedIn: 'root' })
export class TributacionService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/contabilidad/tributacion`;
  perfil() {
    return this.http.get<PerfilObligaciones>(`${this.url}/perfil`);
  }
  guardarPerfil(
    r: Omit<PerfilObligaciones, 'feriados' | 'confirmadoEn'> & {
      feriados: string[];
      confirmado: boolean;
    },
  ) {
    return this.http.put<void>(`${this.url}/perfil`, r);
  }
  calendario(anio: number) {
    return this.http.get<CalendarioTributario[]>(`${this.url}/calendario`, { params: { anio } });
  }
  cargas(anio: number) {
    return this.http.get<CargaFamiliar[]>(`${this.url}/cargas`, { params: { anio } });
  }
  carga(r: Omit<CargaFamiliar, 'id'>) {
    return this.http.post<{ id: number }>(`${this.url}/cargas`, r);
  }
  quitarCarga(id: number) {
    return this.http.delete<void>(`${this.url}/cargas/${id}`);
  }
  gastos(anio: number) {
    return this.http.get<GastosPersonales>(`${this.url}/gastos-personales/${anio}`);
  }
  renta(anio: number) {
    return this.http.get<RentaPreparada>(`${this.url}/renta/${anio}`);
  }
  guardarRenta(anio: number, r: ParametrosRenta) {
    return this.http.put<void>(`${this.url}/renta/${anio}`, r);
  }
  iva(anio: number, mes: number, meses: number) {
    return this.http.get<IvaPreparado>(`${this.url}/iva/${anio}/${mes}/${meses}`);
  }
  guardarIva(anio: number, mes: number, meses: number, r: ParametrosIva) {
    return this.http.put<void>(`${this.url}/iva/${anio}/${mes}/${meses}`, r);
  }
  documentos(anio: number, mes: number, meses: number) {
    return this.http.get<DocumentoFiscal[]>(`${this.url}/documentos`, {
      params: { anio, mes, meses },
    });
  }
  revisarDocumento(d: DocumentoFiscal) {
    return this.http.put<void>(`${this.url}/documentos/${d.id}`, {
      ...d,
      codigoSustento: d.codigoSustento || null,
      formaPago: d.formaPago || null,
    });
  }
  retenciones(anio: number, mes: number, meses: number) {
    return this.http.get<RetencionesPreparadas>(`${this.url}/retenciones/${anio}/${mes}/${meses}`);
  }
  ats(anio: number, mes: number, meses: number) {
    return this.http.get<AtsPreparado>(`${this.url}/ats/${anio}/${mes}/${meses}`);
  }
  revisiones(anio: number) {
    return this.http.get<RevisionTributo[]>(`${this.url}/revisiones`, { params: { anio } });
  }
  revisar(tipo: string, anio: number, mes: number, meses: number, confirmado: boolean) {
    return this.http.post<{ id: number }>(`${this.url}/revisiones`, {
      tipo,
      anio,
      mes,
      meses,
      confirmado,
    });
  }
  presentar(id: number, referencia: string, fecha: string) {
    return this.http.post<void>(`${this.url}/revisiones/${id}/presentacion`, { referencia, fecha });
  }
}
