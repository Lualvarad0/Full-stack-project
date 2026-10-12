import { DestroyRef, inject, signal } from '@angular/core';
/** Búsqueda diferida compartida; limpiar o destruir cancela el texto pendiente. */
export function crearBusqueda(demora = 300) {
  const valor = signal('');
  const aplicada = signal('');
  let temporizador: ReturnType<typeof setTimeout> | undefined;
  const cancelar = () => clearTimeout(temporizador);
  inject(DestroyRef).onDestroy(cancelar);
  return {
    valor,
    aplicada,
    buscar(texto: string): void {
      valor.set(texto);
      cancelar();
      temporizador = setTimeout(() => aplicada.set(texto.trim()), demora);
    },
    limpiar(): void {
      cancelar();
      valor.set('');
      aplicada.set('');
    },
  };
}
