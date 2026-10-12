// Configuración → Mi perfil y Sistema (contratos de /api/cuenta).

export type PaginaInicio = 'Resumen' | 'Cuentas';
export type MesAlEntrar = 'Actual' | 'Anterior';

export interface DatosPersonales {
  nombre: string;
  apellido: string;
  /** yyyy-MM-dd */
  fechaNacimiento: string | null;
  correo: string;
  correoVerificado: boolean;
  telefono: string | null;
  ciudad: string | null;
  direccion: string | null;
  ocupacion: string | null;
  miembroDesde: string;
}

export interface DatosPersonalesRequest {
  nombre: string;
  apellido: string;
  fechaNacimiento: string;
  telefono: string | null;
  ciudad: string | null;
  direccion: string | null;
  ocupacion: string | null;
}

/** Guardada en el servidor: vale en todos los dispositivos. */
export interface ConfiguracionSistema {
  paginaInicio: PaginaInicio;
  mesAlEntrar: MesAlEntrar;
  avisarIngresosPorCorreo: boolean;
}
