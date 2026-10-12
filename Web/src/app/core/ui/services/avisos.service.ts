import { effect, inject, Injectable, signal } from '@angular/core';
import { AuthService } from '@core/auth';
import { Aviso } from '../models/ui.model';
import { mensajeDeError } from '@core/http';
@Injectable({ providedIn: 'root' })
export class AvisosService {
  private siguiente = 1;
  readonly avisos = signal<Aviso[]>([]);

  constructor() {
    const auth = inject(AuthService);
    effect(() => {
      auth.usuario()?.id;
      auth.claveDatos?.();
      this.avisos.set([]);
    });
  }

  exito(texto: string): void {
    this.mostrar('exito', texto);
  }

  error(error: unknown): void {
    this.mostrar('error', mensajeDeError(error));
  }

  cerrar(id: number): void {
    this.avisos.update((lista) => lista.filter((a) => a.id !== id));
  }

  private mostrar(tipo: Aviso['tipo'], texto: string): void {
    const id = this.siguiente++;
    this.avisos.update((lista) => [...lista, { id, tipo, texto }]);
    setTimeout(() => this.cerrar(id), tipo === 'error' ? 8000 : 4500);
  }
}
