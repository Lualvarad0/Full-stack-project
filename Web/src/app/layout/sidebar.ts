import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import {
  LucideCalculator,
  LucideFileText,
  LucideFileUp,
  LucideHandCoins,
  LucideIdCard,
  LucideLayoutDashboard,
  LucideLogOut,
  LucideReceipt,
  LucideSettings,
  LucideShieldUser,
  LucideStore,
  LucideUsers,
  LucideWallet,
  LucideX,
} from '@lucide/angular';
import { AuthService } from '@core/auth';
import { PerfilService } from '@core/perfil';
import { Credito, InsigniaPro, Marca } from '@shared/components';
@Component({
  selector: 'app-sidebar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'contents' },
  imports: [
    RouterLink,
    RouterLinkActive,
    InsigniaPro,
    Marca,
    Credito,
    LucideLayoutDashboard,
    LucideFileUp,
    LucideFileText,
    LucideHandCoins,
    LucideUsers,
    LucideWallet,
    LucideReceipt,
    LucideStore,
    LucideCalculator,
    LucideIdCard,
    LucideShieldUser,
    LucideSettings,
    LucideLogOut,
    LucideX,
  ],
  templateUrl: './sidebar.html',
})
export class Sidebar {
  readonly menuAbierto = input(false);
  readonly cerrar = output<void>();
  protected readonly auth = inject(AuthService);
  protected readonly perfil = inject(PerfilService);
  protected readonly proximos = [
    { ruta: '/declaraciones', nombre: 'Preparación tributaria', icono: 'declaracion' },
    { ruta: '/mi-ruc', nombre: 'Mi RUC', icono: 'ruc' },
    { ruta: '/gastos-personales', nombre: 'Gastos personales', icono: 'declaracion' },
    { ruta: '/exportaciones', nombre: 'Exportar y entregar', icono: 'declaracion' },
  ] as const;

  protected iniciales(): string {
    const nombre = this.auth.usuario()?.nombre ?? '';
    return nombre
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((p) => p[0]!.toUpperCase())
      .join('');
  }
}
