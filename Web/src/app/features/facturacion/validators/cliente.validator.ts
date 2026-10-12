import { cedulaValida, rucValido } from '@core/perfil';
export function etiquetaIdentificacion(tipo: string): string {
  return (
    ({ '04': 'RUC', '05': 'Cédula', '06': 'Pasaporte' } as Record<string, string>)[tipo] ??
    'Identificación'
  );
}

/** Validación progresiva del campo, antes de enviar el formulario completo. */
export function identificacionInvalida(tipo: string, id: string): boolean {
  if (!id || tipo === '06') return false;
  return tipo === '04' ? id.length >= 13 && !rucValido(id) : id.length >= 10 && !cedulaValida(id);
}
