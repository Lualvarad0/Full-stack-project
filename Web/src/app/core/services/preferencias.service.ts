import { effect, Injectable, signal } from '@angular/core';
import { Tema } from '@core/models';
import { escribir } from '@core/utils';

/** Preferencias de este navegador (los datos tributarios viven en el perfil, en el servidor). */
@Injectable({ providedIn: 'root' })
export class PreferenciasService {
  readonly tema = signal<Tema>((document.documentElement.dataset['theme'] as Tema) ?? 'light');

  constructor() {
    effect(() => {
      const tema = this.tema();
      document.documentElement.dataset['theme'] = tema;
      escribir('tema', tema);
    });
  }

  alternarTema(): void {
    this.tema.update((t) => (t === 'dark' ? 'light' : 'dark'));
  }
}
