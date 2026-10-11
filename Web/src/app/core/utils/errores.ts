import { HttpErrorResponse } from '@angular/common/http';

/** Extrae un mensaje legible de un ProblemDetails / ValidationProblemDetails de la API. */
export function mensajeDeError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return 'No se pudo conectar con el servidor. Revisa tu conexión.';
    const cuerpo = error.error as { detail?: string; title?: string; errors?: Record<string, string[]> } | null;
    if (cuerpo?.errors) return Object.values(cuerpo.errors).flat().join(' ');
    if (cuerpo?.detail) return cuerpo.detail;
    if (error.status >= 500) return 'Ocurrió un error en el servidor. Inténtalo de nuevo.';
    return cuerpo?.title ?? 'No se pudo completar la operación.';
  }
  return 'Ocurrió un error inesperado.';
}
