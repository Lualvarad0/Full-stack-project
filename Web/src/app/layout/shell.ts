import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { AuthService } from '@core/auth';
import { Avisos } from '@shared/components';
import { FechaNacimientoModal } from './fecha-nacimiento-modal';
import { Sidebar } from './sidebar';
import { Header } from './header';
@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, Avisos, Sidebar, Header, FechaNacimientoModal],
  templateUrl: './shell.html',
})
export class Shell {
  protected readonly menuAbierto = signal(false);
  protected readonly auth = inject(AuthService);
  constructor() {
    inject(Router)
      .events.pipe(
        filter((e) => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.menuAbierto.set(false));
  }
}
