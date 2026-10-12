import { effect, inject, Injectable, signal, untracked } from '@angular/core';
import { Tema } from '../models/ui.model';
import { escribir } from '@core/storage';
import { AuthService } from '@core/auth';
/** Preferencias separadas por cuenta; los datos tributarios viven en el servidor. */
@Injectable({ providedIn: 'root' })
export class PreferenciasService {
  private readonly auth = inject(AuthService);
  readonly tema = signal<Tema>((document.documentElement.dataset['theme'] as Tema) ?? 'light');

  constructor() {
    effect(() => {
      const clave = this.clave(this.auth.usuario()?.id);
      let guardado: string | null = null;
      try {
        guardado = localStorage.getItem(clave);
      } catch {
        /* Solo en memoria. */
      }
      untracked(() => this.tema.set(guardado === 'dark' ? 'dark' : 'light'));
    });
    effect(() => {
      const tema = this.tema();
      document.documentElement.dataset['theme'] = tema;
      escribir(this.clave(this.auth.usuario()?.id), tema);
    });
  }

  private clave(usuarioId: number | undefined): string {
    return usuarioId ? `tema:usuario:${usuarioId}` : 'tema:publico';
  }

  alternarTema(): void {
    this.tema.update((t) => (t === 'dark' ? 'light' : 'dark'));
  }
}
