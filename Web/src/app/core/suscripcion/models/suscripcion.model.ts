import { Usuario } from '../../auth/models/auth.model';
// Planes y suscripciones de pago con Kushki (contratos de /api/suscripcion).

export type PlanSuscripcion = 'Personal' | 'Profesional' | 'Contadores';

export interface Plan {
  id: PlanSuscripcion;
  nombre: string;
  precio: string;
  periodo: string;
  descripcion: string;
  destacado: boolean;
  cta: string;
  incluye: string[];
  pronto: string[];
}

export type Periodicidad = 'Mensual' | 'Anual';

export type EstadoSuscripcion = 'EnPrueba' | 'Activa' | 'PagoPendiente' | 'Cancelada';

export interface PrecioPlan {
  plan: PlanSuscripcion;
  periodicidad: Periodicidad;
  subtotal: number;
  iva: number;
  total: number;
}

export interface CatalogoPagos {
  precios: PrecioPlan[];
  diasDePrueba: number;
  pagosDisponibles: boolean;
  clavePublicaKushki: string | null;
  kushkiPruebas: boolean;
}

export interface Suscripcion {
  plan: PlanSuscripcion;
  periodicidad: Periodicidad;
  estado: EstadoSuscripcion;
  total: number;
  proximoCobro: string;
  creadaEn: string;
  marcaTarjeta: string | null;
  ultimosDigitos: string | null;
}

export interface ResultadoSuscripcion {
  usuario: Usuario;
  suscripcion: Suscripcion | null;
}

/** Uso del plan: en Personal, comprobantes de este mes frente al límite. */
export interface UsoPlan {
  plan: PlanSuscripcion;
  profesional: boolean;
  limiteComprobantes: number | null;
  comprobantesEsteMes: number;
  mesesHistorial: number | null;
}

/** Lo mínimo que se usa de la tarjeta de Kushki (el SDK se carga solo en esta página). */
export interface TarjetaKushki {
  requestToken(): Promise<{ token: string; cardInfo?: { brand: string; lastFourDigits: string } }>;
}
