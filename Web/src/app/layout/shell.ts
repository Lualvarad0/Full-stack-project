import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import {
  LucideCalculator,
  LucideChevronLeft,
  LucideChevronRight,
  LucideFileText,
  LucideFileUp,
  LucideHandCoins,
  LucideIdCard,
  LucideLayoutDashboard,
  LucideLogOut,
  LucideMenu,
  LucideMoon,
  LucideReceipt,
  LucideSettings,
  LucideStore,
  LucideSun,
  LucideUsers,
  LucideX,
} from '@lucide/angular';
import { AuthService } from '../core/auth';
import { PeriodoService, PreferenciasService } from '../core/estado';
import { PerfilService } from '../core/perfil.service';
import { Avisos } from '../shared/avisos';
import { Marca } from '../shared/marca';

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, Avisos, Marca,
    LucideLayoutDashboard, LucideReceipt, LucideFileUp, LucideStore, LucideFileText, LucideHandCoins,
    LucideUsers, LucideCalculator, LucideIdCard, LucideSettings, LucideLogOut, LucideMenu, LucideX,
    LucideMoon, LucideSun, LucideChevronLeft, LucideChevronRight,
  ],
  templateUrl: './shell.html',
})
export class Shell {
  protected readonly auth = inject(AuthService);
  protected readonly periodo = inject(PeriodoService);
  protected readonly preferencias = inject(PreferenciasService);
  protected readonly perfil = inject(PerfilService);
  protected readonly menuAbierto = signal(false);

  protected readonly proximos = [
    { ruta: '/facturacion', nombre: 'Emitir facturas', icono: 'factura' },
    { ruta: '/declaraciones', nombre: 'Declarar IVA', icono: 'declaracion' },
    { ruta: '/mi-ruc', nombre: 'Mi RUC', icono: 'ruc' },
  ] as const;

  constructor() {
    this.perfil.cargar();

    // Cierra el menú lateral en móvil al navegar.
    inject(Router).events
      .pipe(filter((e) => e instanceof NavigationEnd), takeUntilDestroyed())
      .subscribe(() => this.menuAbierto.set(false));
  }

  protected iniciales(): string {
    const nombre = this.auth.usuario()?.nombre ?? '';
    return nombre.split(/\s+/).filter(Boolean).slice(0, 2).map((p) => p[0]!.toUpperCase()).join('');
  }
}
