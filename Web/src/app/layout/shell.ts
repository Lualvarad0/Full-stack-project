import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
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
  LucideShieldUser,
  LucideStore,
  LucideSun,
  LucideUsers,
  LucideX,
} from '@lucide/angular';
import { AuthService, PerfilService, PeriodoService, PreferenciasService } from '@core/services';
import { Avisos, Credito, InsigniaPro, Marca } from '@shared/components';

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, Avisos, InsigniaPro, Marca, Credito,
    LucideLayoutDashboard, LucideReceipt, LucideFileUp, LucideStore, LucideFileText, LucideHandCoins,
    LucideUsers, LucideCalculator, LucideIdCard, LucideSettings, LucideShieldUser, LucideLogOut, LucideMenu, LucideX,
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
  /** En el plan Personal solo se ven el mes actual y los dos anteriores. */
  protected readonly puedeRetroceder = computed(() => {
    if (this.auth.esProfesional()) return true;
    const hoy = new Date();
    const limite = hoy.getFullYear() * 12 + hoy.getMonth() - 2;
    return this.periodo.anio() * 12 + this.periodo.mes() - 1 > limite;
  });

  protected readonly proximos = [
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
