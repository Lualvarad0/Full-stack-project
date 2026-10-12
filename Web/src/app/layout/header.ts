import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import {
  LucideMenu,
  LucideMoon,
  LucideSun,
  LucideChevronLeft,
  LucideChevronRight,
} from '@lucide/angular';
import { AuthService } from '@core/auth';
import { PeriodoService } from '@core/comprobantes';
import { PreferenciasService } from '@core/ui';
import { CarteraService } from '@core/contabilidad/services/cartera.service';
import { Router } from '@angular/router';
@Component({
  selector: 'app-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'contents' },
  imports: [LucideMenu, LucideMoon, LucideSun, LucideChevronLeft, LucideChevronRight],
  templateUrl: './header.html',
})
export class Header {
  readonly menuAbierto = input(false);
  readonly abrir = output<void>();
  protected readonly auth = inject(AuthService);
  protected readonly cartera = inject(CarteraService);
  private readonly router = inject(Router);
  protected seleccionar(id: string): void { this.cartera.seleccionar(Number(id)); void this.router.navigate(['/resumen']); }
  protected readonly periodo = inject(PeriodoService);
  protected readonly preferencias = inject(PreferenciasService);
  /** En el plan Personal solo se ven el mes actual y los dos anteriores. */
  protected readonly puedeRetroceder = computed(() => {
    if (this.auth.esProfesional()) return true;
    const hoy = new Date();
    const limite = hoy.getFullYear() * 12 + hoy.getMonth() - 2;
    return this.periodo.anio() * 12 + this.periodo.mes() - 1 > limite;
  });
}
