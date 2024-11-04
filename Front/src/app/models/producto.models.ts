export interface Producto {
  id?: number; // opcional si se genera automáticamente
  nombre: string;
  descripcion: string;
  precio: number;
  stock: number;
}
