export interface Carga<T> {
  cargando: boolean;
  datos: T | null;
  error: string | null;
}
