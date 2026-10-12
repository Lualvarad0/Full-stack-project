import { EstadoSuscripcion } from '../models/suscripcion.model';
export const ESTADOS_SUSCRIPCION: Record<EstadoSuscripcion, string> = {
  EnPrueba: 'En prueba gratis',
  Activa: 'Activa',
  PagoPendiente: 'Pago pendiente',
  Cancelada: 'Cancelada',
};
