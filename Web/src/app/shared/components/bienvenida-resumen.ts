import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { AuthService } from '@core/auth';
import { CorreoService } from '@core/correo';
import { cargar } from '@core/http';

/** Bienvenida y actividad real de la cuenta, independientes de las métricas del período. */
@Component({
  selector: 'app-bienvenida-resumen',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './bienvenida-resumen.html',
})
export class BienvenidaResumen {
  readonly administrador = input(false);
  private readonly auth = inject(AuthService);
  private readonly correo = inject(CorreoService);
  private readonly ahora = signal(new Date());
  protected readonly usuario = this.auth.usuario;
  protected readonly fecha = computed(() =>
    new Intl.DateTimeFormat('es-EC', {
      timeZone: 'America/Guayaquil',
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    }).format(this.ahora()),
  );
  protected readonly saludo = computed(() => {
    const hora = Number(
      new Intl.DateTimeFormat('es-EC', {
        timeZone: 'America/Guayaquil',
        hour: 'numeric',
        hourCycle: 'h23',
      }).format(this.ahora()),
    );
    return hora < 12 ? 'Buenos días' : hora < 19 ? 'Buenas tardes' : 'Buenas noches';
  });
  protected readonly actividad = cargar(
    () => 0,
    () => this.correo.avisos(),
  );
  protected readonly ultima = computed(
    () =>
      [...(this.actividad().datos ?? [])]
        .filter((a) => Number.isFinite(Date.parse(a.fecha)))
        .sort((a, b) => Date.parse(b.fecha) - Date.parse(a.fecha) || b.id - a.id)[0] ?? null,
  );
  protected fechaHora(iso: string): string {
    return new Intl.DateTimeFormat('es-EC', {
      timeZone: 'America/Guayaquil',
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(iso));
  }

  constructor() {
    const reloj = setInterval(() => this.ahora.set(new Date()), 60_000);
    inject(DestroyRef).onDestroy(() => clearInterval(reloj));
  }
}
