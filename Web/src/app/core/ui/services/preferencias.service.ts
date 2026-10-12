import { computed, effect, inject, Injectable, signal, untracked } from '@angular/core';
import { ModoTema, TamanoTexto, Tema } from '../models/ui.model';
import { escribir } from '@core/storage';
import { AuthService } from '@core/auth';

const MEDIA_OSCURO = '(prefers-color-scheme: dark)';

/**
 * Preferencias de este dispositivo, separadas por cuenta (tema, tamaño del texto y animaciones).
 * La configuración que vale en todos los dispositivos vive en el servidor (Configuración → Sistema).
 */
@Injectable({ providedIn: 'root' })
export class PreferenciasService {
  private readonly auth = inject(AuthService);
  private readonly media = typeof matchMedia === 'function' ? matchMedia(MEDIA_OSCURO) : null;
  private readonly sistemaOscuro = signal(this.media?.matches ?? false);

  readonly modoTema = signal<ModoTema>('sistema');
  readonly texto = signal<TamanoTexto>('normal');
  readonly reducirMovimiento = signal(false);
  /** Tema que se ve (resuelve "sistema"). */
  readonly tema = computed<Tema>(() => {
    const modo = this.modoTema();
    return modo === 'sistema' ? (this.sistemaOscuro() ? 'dark' : 'light') : modo;
  });

  constructor() {
    this.media?.addEventListener?.('change', (e) => this.sistemaOscuro.set(e.matches));

    // Al cambiar de cuenta, carga sus preferencias.
    effect(() => {
      const id = this.auth.usuario()?.id;
      const tema = leer(this.clave('tema', id));
      const texto = leer(this.clave('texto', id));
      const movimiento = leer(this.clave('movimiento', id));
      untracked(() => {
        this.modoTema.set(tema === 'dark' || tema === 'light' ? tema : 'sistema');
        this.texto.set(texto === 'grande' ? 'grande' : 'normal');
        this.reducirMovimiento.set(movimiento === 'reducido');
      });
    });

    effect(() => {
      const raiz = document.documentElement.dataset;
      raiz['theme'] = this.tema();
      raiz['texto'] = this.texto();
      raiz['movimiento'] = this.reducirMovimiento() ? 'reducido' : 'normal';
      const id = untracked(() => this.auth.usuario()?.id);
      escribir(this.clave('tema', id), this.modoTema());
      escribir(this.clave('texto', id), this.texto());
      escribir(this.clave('movimiento', id), this.reducirMovimiento() ? 'reducido' : 'normal');
    });
  }

  alternarTema(): void {
    this.modoTema.set(this.tema() === 'dark' ? 'light' : 'dark');
  }

  /** Vuelve a lo predeterminado: tema del sistema, texto normal y animaciones. */
  restablecer(): void {
    this.modoTema.set('sistema');
    this.texto.set('normal');
    this.reducirMovimiento.set(false);
  }

  private clave(tipo: 'tema' | 'texto' | 'movimiento', usuarioId: number | undefined): string {
    return usuarioId ? `${tipo}:usuario:${usuarioId}` : `${tipo}:publico`;
  }
}

function leer(clave: string): string | null {
  try {
    return localStorage.getItem(clave);
  } catch {
    return null;
  }
}
