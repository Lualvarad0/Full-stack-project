import { PlanSuscripcion } from '../../suscripcion/models/suscripcion.model';
// Sesión, usuario y registro (contratos de /api/auth).

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
  /** Pantalla al iniciar sesión (Configuración → Sistema). */
  paginaInicio?: 'Resumen' | 'Cuentas';
  /** Mes que se muestra al entrar. */
  mesAlEntrar?: 'Actual' | 'Anterior';
  /** Cuenta antigua sin fecha de nacimiento: debe registrarla (y ser mayor de edad) antes de usar la app. */
  faltaFechaNacimiento?: boolean;
}

export interface AuthResponse {
  token: string;
  expiraEn: string;
  usuario: Usuario;
}

export type ModoAcceso = 'login' | 'registro';
