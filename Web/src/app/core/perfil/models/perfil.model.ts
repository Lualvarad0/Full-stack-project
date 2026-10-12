// Perfil tributario del usuario (contratos de /api/perfil).
export type RegimenTributario = 'General' | 'RimpeEmprendedor' | 'RimpeNegocioPopular';

export interface Perfil {
  nombre: string;
  email: string;
  cedula: string | null;
  ruc: string | null;
  razonSocial: string | null;
  regimen: RegimenTributario | null;
  cargasFamiliares: number;
  enfermedadCatastrofica: boolean;
  completo: boolean;
}

export interface PerfilRequest {
  cedula: string | null;
  ruc: string | null;
  razonSocial: string | null;
  regimen: RegimenTributario | null;
  cargasFamiliares: number;
  enfermedadCatastrofica: boolean;
}

export interface PerfilActualizado {
  perfil: Perfil;
  /** -1 cuando el RUC no cambió y no hubo reclasificación. */
  comprobantesEmitidos: number;
  comprobantesRecibidos: number;
}
