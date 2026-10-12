import { EstadoSuscripcion, PlanSuscripcion } from '../../suscripcion/models/suscripcion.model';
// Panel de superadministrador (contratos de /api/admin).

export interface MetricasAdmin {
  usuariosTotales: number;
  usuariosSuspendidos: number;
  nuevosUltimos30Dias: number;
  activosUltimos30Dias: number;
  porPlan: Record<PlanSuscripcion, number>;
  suscripcionesEnPrueba: number;
  suscripcionesActivas: number;
  suscripcionesConPagoPendiente: number;
  ingresoMensualRecurrente: number;
  cobradoUltimos30Dias: number;
  facturasEmitidas: number;
  facturasAutorizadas: number;
  comprobantesImportados: number;
  comprobantesEsteMes: number;
  registrosPorMes: { anio: number; mes: number; usuarios: number }[];
}

export interface UsuarioAdmin {
  id: number;
  nombre: string;
  correo: string;
  rol: string;
  plan: PlanSuscripcion;
  activo: boolean;
  fechaCreacion: string;
  ultimoIngresoEn: string | null;
  ruc: string | null;
  estadoSuscripcion: EstadoSuscripcion | null;
  comprobantes: number;
  facturasEmitidas: number;
}

export interface PagoAdmin {
  id: number;
  fecha: string;
  usuario: string;
  correo: string;
  plan: PlanSuscripcion;
  monto: number;
  aprobado: boolean;
  ticket: string | null;
  detalle: string | null;
}

export interface EstadoSistema {
  entorno: string;
  kushkiConfigurado: boolean;
  kushkiPruebas: boolean;
  kushkiWebhookConfigurado: boolean;
  smtpConfigurado: boolean;
  dominioBuzon: string | null;
  dataProtectionPersistente: boolean;
  migracionesPendientes: number;
  horaServidorUtc: string;
}

export type PestanaAdmin = 'resumen' | 'usuarios' | 'pagos' | 'sistema';
