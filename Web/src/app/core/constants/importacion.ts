import { ResultadoImportacion } from '@core/models';

/** Cómo se muestra cada resultado de importación del SRI. */
export const ESTADOS_IMPORTACION: Record<ResultadoImportacion, { texto: string; tono: 'exito' | 'neutro' | 'aviso' | 'error' }> = {
  Importado: { texto: 'Importado', tono: 'exito' },
  YaExistia: { texto: 'Ya estaba', tono: 'neutro' },
  NoSoportado: { texto: 'Omitido', tono: 'neutro' },
  LimiteDelPlan: { texto: 'Límite del plan', tono: 'aviso' },
  NoEncontrado: { texto: 'No encontrado', tono: 'aviso' },
  NoAutorizado: { texto: 'No autorizado', tono: 'error' },
  Error: { texto: 'Error', tono: 'error' },
};

/** Tamaño máximo de lo que se sube en una importación (reporte .txt y XML). */
export const MAX_BYTES_IMPORTACION = 10 * 1024 * 1024;
