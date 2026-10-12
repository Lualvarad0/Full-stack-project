import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { LucideLoader, LucideSlidersHorizontal } from '@lucide/angular';
import { AuthService } from '@core/auth';
import { ConfiguracionSistema, CuentaUsuarioService, MesAlEntrar, PaginaInicio } from '@core/perfil';
import { mensajeDeError } from '@core/http';
import { AvisosService } from '@core/ui';
import { InsigniaPro, MensajeError, Segmentado } from '@shared/components';

/** Configuración → Sistema: cómo se comporta Nudo en todos tus dispositivos (se guarda en el servidor). */
@Component({
  selector: 'app-configuracion-sistema',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  imports: [Segmentado, MensajeError, InsigniaPro, LucideLoader, LucideSlidersHorizontal],
  template: `
    <section class="card p-5 sm:p-6" aria-labelledby="titulo-config-sistema">
      <div class="flex items-start gap-3">
        <span class="grid size-10 shrink-0 place-items-center rounded-xl bg-primary-soft text-primary">
          <svg lucideSlidersHorizontal class="size-5" aria-hidden="true"></svg>
        </span>
        <div class="min-w-0">
          <h2 id="titulo-config-sistema" class="font-semibold">Al usar Nudo</h2>
          <p class="mt-1 text-sm text-muted-fg">Se guarda en tu cuenta y vale en todos tus dispositivos.</p>
        </div>
      </div>

      @if (config(); as c) {
        <dl class="mt-5 divide-y divide-border">
          <div class="flex flex-wrap items-center justify-between gap-3 py-4">
            <div class="min-w-0 max-w-md">
              <dt class="flex items-center gap-2 font-medium">
                Pantalla al entrar
                @if (!auth.esProfesional()) {
                  <app-insignia-pro />
                }
              </dt>
              <dd class="mt-0.5 text-sm text-muted-fg">
                @if (auth.esProfesional()) {
                  Lo primero que ves al iniciar sesión.
                } @else {
                  En el plan gratuito entras a Mis cuentas. Con Profesional puedes empezar en el Resumen.
                }
              </dd>
            </div>
            @if (auth.esProfesional()) {
              <app-segmentado
                etiqueta="Pantalla al entrar"
                [opciones]="paginas"
                [valor]="c.paginaInicio"
                (valorChange)="cambiar({ paginaInicio: $event })"
              />
            }
          </div>

          <div class="flex flex-wrap items-center justify-between gap-3 py-4">
            <div class="min-w-0 max-w-md">
              <dt class="font-medium">Mes que se abre</dt>
              <dd class="mt-0.5 text-sm text-muted-fg">
                Elige el anterior si sueles entrar a cerrar o declarar el mes que terminó.
              </dd>
            </div>
            <app-segmentado
              etiqueta="Mes que se abre"
              [opciones]="meses"
              [valor]="c.mesAlEntrar"
              (valorChange)="cambiar({ mesAlEntrar: $event })"
            />
          </div>

          <div class="flex flex-wrap items-center justify-between gap-3 py-4">
            <div class="min-w-0 max-w-md">
              <dt><label class="font-medium" for="avisar-ingresos">Correo en cada ingreso a tu cuenta</label></dt>
              <dd class="mt-0.5 text-sm text-muted-fg">
                Te escribimos cuando alguien inicia sesión. Si lo apagas, igual queda en tu actividad
                y los cambios de seguridad se avisan siempre.
              </dd>
            </div>
            <input
              id="avisar-ingresos"
              type="checkbox"
              role="switch"
              class="size-5 cursor-pointer accent-[var(--accent)]"
              [checked]="c.avisarIngresosPorCorreo"
              (change)="cambiar({ avisarIngresosPorCorreo: $any($event.target).checked })"
            />
          </div>
        </dl>
        <p class="mt-1 flex min-h-5 items-center gap-2 text-xs text-muted-fg" aria-live="polite">
          @if (guardando()) {
            <svg lucideLoader class="size-3.5 animate-spin" aria-hidden="true"></svg> Guardando…
          }
        </p>
      } @else if (!error()) {
        <div class="mt-5 h-40 animate-pulse rounded-lg bg-muted"></div>
      }
      <app-mensaje-error class="mt-2 block" [mensaje]="error()" />
    </section>
  `,
})
export class ConfiguracionSistemaTarjeta {
  private readonly servicio = inject(CuentaUsuarioService);
  private readonly avisos = inject(AvisosService);
  protected readonly auth = inject(AuthService);

  protected readonly paginas: readonly { valor: PaginaInicio; etiqueta: string }[] = [
    { valor: 'Resumen', etiqueta: 'Resumen' },
    { valor: 'Cuentas', etiqueta: 'Mis cuentas' },
  ];
  protected readonly meses: readonly { valor: MesAlEntrar; etiqueta: string }[] = [
    { valor: 'Actual', etiqueta: 'El actual' },
    { valor: 'Anterior', etiqueta: 'El anterior' },
  ];

  protected readonly config = signal<ConfiguracionSistema | null>(null);
  protected readonly guardando = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.servicio.configuracion().subscribe({
      next: (c) => this.config.set(c),
      error: (e) => this.error.set(mensajeDeError(e)),
    });
  }

  /** Cada cambio se guarda al instante; si falla, vuelve al valor anterior. */
  protected cambiar(cambio: Partial<ConfiguracionSistema>): void {
    const anterior = this.config();
    if (!anterior) return;
    const nueva = { ...anterior, ...cambio };
    this.config.set(nueva);
    this.error.set(null);
    this.guardando.set(true);
    this.servicio.guardarConfiguracion(nueva).subscribe({
      next: (c) => {
        this.guardando.set(false);
        this.config.set(c);
        this.avisos.exito('Configuración guardada.');
      },
      error: (e) => {
        this.guardando.set(false);
        this.config.set(anterior);
        this.error.set(mensajeDeError(e));
      },
    });
  }
}
