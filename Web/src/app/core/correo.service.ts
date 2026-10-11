import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type EstadoCorreo = 'Pendiente' | 'Procesado' | 'SinComprobantes' | 'ConfirmacionGmail' | 'Error';

export interface CorreoRecibido {
  id: number;
  recibidoEn: string;
  remitente: string;
  asunto: string;
  estado: EstadoCorreo;
  archivosXml: number;
  clavesEncontradas: number;
  importados: number;
  ingresos: number;
  detalle: string | null;
}

/** Buzón de reenvío del usuario: su dirección propia y los últimos correos recibidos. */
export interface Buzon {
  codigo: string;
  /** Null si el servidor aún no tiene dominio de correo entrante configurado. */
  direccion: string | null;
  configurado: boolean;
  ultimoCorreoEn: string | null;
  confirmacionGmailCodigo: string | null;
  confirmacionGmailEnlace: string | null;
  confirmacionGmailEn: string | null;
  recientes: CorreoRecibido[];
}

export interface AvisoCuenta {
  id: number;
  fecha: string;
  tipo: 'Registro' | 'Ingreso' | 'PerfilActualizado' | 'BuzonRegenerado';
  titulo: string;
  detalle: string;
  ip: string | null;
  dispositivo: string | null;
  enviadoPorCorreo: boolean;
}

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

/** "14 oct, 09:30" en hora local. */
export function fechaHora(iso: string): string {
  return new Intl.DateTimeFormat('es-EC', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }).format(new Date(iso));
}
