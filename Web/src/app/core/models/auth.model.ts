// Sesión, usuario y registro (contratos de /api/auth).
import { PlanSuscripcion } from './suscripcion.model';

export interface RegistroRequest {
  nombre: string;
  apellido: string;
  fechaNacimiento: string;
  plan: PlanSuscripcion;
  email: string;
  password: string;
}

export interface Sesion {
  token: string;
  expiraEn: string;
  usuario: Usuario;
}

// Contratos de la API (Back/Dtos). Las fechas sin hora llegan como 'yyyy-MM-dd'.
export interface Usuario {
  id: number;
  nombre: string;
  email: string;
  rol: 'admin' | 'user';
  plan: PlanSuscripcion;
}

export interface AuthResponse {
  token: string;
  expiraEn: string;
  usuario: Usuario;
}

export type ModoAcceso = 'login' | 'registro';
