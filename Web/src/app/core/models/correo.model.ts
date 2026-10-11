// Buzón de reenvío y avisos de seguridad (contratos de /api/correo y /api/avisos).
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
  tipo: 'Registro' | 'Ingreso' | 'PerfilActualizado' | 'BuzonRegenerado' | 'FirmaElectronica' | 'DatosFacturacion' | 'PlanCambiado';
  titulo: string;
  detalle: string;
  ip: string | null;
  dispositivo: string | null;
  enviadoPorCorreo: boolean;
}
