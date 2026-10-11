import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, OnInit, signal } from '@angular/core';
import {
  LucideCircleAlert,
  LucideCircleCheck,
  LucideCopy,
  LucideExternalLink,
  LucideInbox,
  LucideLoader,
  LucideRefreshCw,
} from '@lucide/angular';
import { Buzon, CorreoRecibido } from '@core/models';
import { AvisosService, CorreoService } from '@core/services';
import { fechaHora, mensajeDeError } from '@core/utils';
import { MensajeError } from './mensaje-error';

/** Buzón de reenvío: dirección propia del usuario, guía para Gmail y últimos correos recibidos. */
@Component({
  selector: 'app-buzon-tarjeta',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensajeError, LucideInbox, LucideCopy, LucideCircleCheck, LucideCircleAlert, LucideExternalLink, LucideRefreshCw, LucideLoader],
  template: `
    <section class="card p-5 sm:p-6" [class.max-w-3xl]="!compacta()" aria-labelledby="titulo-buzon">
      <div class="flex items-start gap-3">
        <span class="grid size-10 shrink-0 place-items-center rounded-full bg-primary-soft text-primary" aria-hidden="true">
          <svg lucideInbox class="size-5"></svg>
        </span>
        <div class="min-w-0 flex-1">
          <h2 id="titulo-buzon" class="font-semibold">Tu buzón de facturas</h2>
          <p class="mt-1 text-sm leading-relaxed text-muted-fg">
            Reenvía a esta dirección los correos con facturas electrónicas. Leemos los XML, los verificamos con el SRI y
            los sumamos a tus gastos e ingresos. No necesitas darnos ninguna contraseña.
          </p>
        </div>
      </div>

      @if (cargando()) {
        <div class="mt-5 h-14 animate-pulse rounded-xl bg-muted"></div>
      } @else if (buzon(); as b) {
        <!-- Dirección -->
        @if (b.direccion) {
          <div class="mt-5 flex flex-wrap items-center gap-2 rounded-xl border border-border bg-muted/50 p-2 pl-4">
            <code class="min-w-0 flex-1 truncate font-mono text-sm text-fg">{{ b.direccion }}</code>
            <button type="button" class="btn btn-secondary min-h-9" (click)="copiar(b.direccion)">
              @if (copiado()) {
                <svg lucideCircleCheck class="size-4 text-success" aria-hidden="true"></svg> Copiada
              } @else {
                <svg lucideCopy class="size-4" aria-hidden="true"></svg> Copiar
              }
            </button>
          </div>
        } @else {
          <p class="aviso-atencion mt-5">
            <svg lucideCircleAlert class="mt-0.5 size-4 shrink-0 text-warning" aria-hidden="true"></svg>
            <span>El correo entrante aún no está activo en el servidor (falta el dominio). Tu código de buzón ya está reservado: <code class="font-mono">{{ b.codigo }}</code>.</span>
          </p>
        }

        <!-- Confirmación que pide Gmail -->
        @if (b.confirmacionGmailCodigo) {
          <div class="aviso-ok mt-4">
            <svg lucideCircleCheck class="mt-0.5 size-5 shrink-0 text-success" aria-hidden="true"></svg>
            <div>
              <p class="font-medium text-fg">Gmail pidió confirmar el reenvío</p>
              <p class="mt-1">Ingresa este código en Gmail: <span class="ml-1 rounded-md bg-card px-2 py-0.5 font-mono text-base font-semibold text-fg">{{ b.confirmacionGmailCodigo }}</span></p>
              @if (b.confirmacionGmailEnlace) {
                <a [href]="b.confirmacionGmailEnlace" target="_blank" rel="noopener" class="mt-2 inline-flex items-center gap-1 font-medium text-accent hover:underline">
                  O confírmalo con un clic <svg lucideExternalLink class="size-3.5" aria-hidden="true"></svg>
                </a>
              }
            </div>
          </div>
        }

        <!-- Guía -->
        <details class="mt-4 rounded-xl border border-border px-4 py-3 text-sm" [open]="!b.ultimoCorreoEn">
          <summary class="cursor-pointer font-medium">Cómo configurar el reenvío en Gmail</summary>
          <ol class="mt-3 list-decimal space-y-2 pl-5 leading-relaxed text-muted-fg">
            <li>En Gmail abre <span class="text-fg">Configuración → Ver toda la configuración → Reenvío y correo POP/IMAP</span> y toca <span class="text-fg">Agregar una dirección de reenvío</span>. Pega tu dirección de Nudo.</li>
            <li>Gmail enviará un código a Nudo: aparecerá aquí arriba en unos segundos. Ingrésalo en Gmail.</li>
            <li>Para reenviar solo las facturas, busca en Gmail <code class="rounded bg-muted px-1 font-mono text-fg">has:attachment filename:xml</code>, toca <span class="text-fg">Crear filtro</span> y marca <span class="text-fg">Reenviarlo a</span> tu dirección de Nudo.</li>
          </ol>
          <p class="mt-3 text-muted-fg">También puedes dar esta dirección directamente cuando te pidan un correo al facturar.</p>
        </details>

        <!-- Correos recibidos -->
        <div class="mt-5">
          <div class="flex items-center justify-between gap-3">
            <h3 class="text-sm font-semibold">Últimos correos recibidos</h3>
            <button type="button" class="btn btn-ghost min-h-8 px-2" aria-label="Actualizar" (click)="cargar()">
              <svg lucideRefreshCw class="size-4" aria-hidden="true"></svg>
            </button>
          </div>
          @if (b.recientes.length) {
            <ul class="mt-2 divide-y divide-border">
              @for (c of b.recientes; track c.id) {
                <li class="flex items-start justify-between gap-3 py-3 text-sm">
                  <div class="min-w-0">
                    <p class="truncate font-medium">{{ c.asunto || '(sin asunto)' }}</p>
                    <p class="truncate text-xs text-muted-fg">{{ c.remitente }} · {{ fecha(c.recibidoEn) }}</p>
                    @if (c.detalle) { <p class="mt-1 text-xs text-muted-fg">{{ c.detalle }}</p> }
                  </div>
                  <span class="pildora shrink-0" [class]="estado(c).clase">
                    @if (c.estado === 'Pendiente') { <svg lucideLoader class="size-3 animate-spin" aria-hidden="true"></svg> }
                    {{ estado(c).texto }}
                  </span>
                </li>
              }
            </ul>
          } @else {
            <p class="mt-2 rounded-xl bg-muted/50 px-4 py-6 text-center text-sm text-muted-fg">Aún no llega ningún correo a tu buzón.</p>
          }
        </div>

        <!-- Cambiar dirección -->
        @if (b.direccion) {
          <div class="mt-4 border-t border-border pt-4 text-sm">
            @if (!confirmarCambio()) {
              <button type="button" class="font-medium text-muted-fg hover:text-fg" (click)="confirmarCambio.set(true)">Cambiar mi dirección de reenvío</button>
            } @else {
              <p class="text-muted-fg">La dirección actual dejará de recibir y tendrás que actualizar el reenvío en Gmail.</p>
              <div class="mt-3 flex gap-2">
                <button type="button" class="btn btn-danger" [disabled]="trabajando()" (click)="regenerar()">Cambiar dirección</button>
                <button type="button" class="btn btn-secondary" (click)="confirmarCambio.set(false)">Cancelar</button>
              </div>
            }
          </div>
        }
      }

      <app-mensaje-error class="mt-4 block" [mensaje]="error()" />
    </section>
  `,
})
export class BuzonTarjeta implements OnInit {
  /** En la página de traer facturas ocupa todo el ancho. */
  readonly compacta = input(false);

  private readonly correo = inject(CorreoService);
  private readonly avisos = inject(AvisosService);

  protected readonly buzon = signal<Buzon | null>(null);
  protected readonly cargando = signal(true);
  protected readonly trabajando = signal(false);
  protected readonly copiado = signal(false);
  protected readonly confirmarCambio = signal(false);
  protected readonly error = signal<string | null>(null);
  private readonly hayPendientes = computed(() => this.buzon()?.recientes.some((c) => c.estado === 'Pendiente') ?? false);

  constructor() {
    // Mientras haya correos en proceso, refresca cada 5 s.
    const intervalo = setInterval(() => { if (this.hayPendientes()) this.cargar(); }, 5000);
    inject(DestroyRef).onDestroy(() => clearInterval(intervalo));
  }

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.correo.buzon().subscribe({
      next: (b) => {
        this.buzon.set(b);
        this.cargando.set(false);
      },
      error: (e) => {
        this.cargando.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }

  protected async copiar(direccion: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(direccion);
      this.copiado.set(true);
      setTimeout(() => this.copiado.set(false), 2000);
    } catch {
      this.error.set('No se pudo copiar. Selecciona la dirección y cópiala manualmente.');
    }
  }

  protected regenerar(): void {
    this.trabajando.set(true);
    this.correo.regenerar().subscribe({
      next: (b) => {
        this.trabajando.set(false);
        this.confirmarCambio.set(false);
        this.buzon.set(b);
        this.avisos.exito('Listo: tienes una nueva dirección. Actualiza el reenvío en Gmail.');
      },
      error: (e) => {
        this.trabajando.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }

  protected fecha(iso: string): string {
    return fechaHora(iso);
  }

  protected estado(c: CorreoRecibido): { texto: string; clase: string } {
    switch (c.estado) {
      case 'Procesado':
        return c.importados > 0
          ? { texto: `+${c.importados} ${c.importados === 1 ? 'comprobante' : 'comprobantes'}`, clase: 'bg-success-soft text-success' }
          : { texto: 'Sin novedades', clase: 'bg-muted text-muted-fg' };
      case 'Pendiente':
        return { texto: 'Procesando', clase: 'bg-primary-soft text-primary' };
      case 'ConfirmacionGmail':
        return { texto: 'Confirmación', clase: 'bg-primary-soft text-primary' };
      case 'SinComprobantes':
        return { texto: 'Sin facturas', clase: 'bg-muted text-muted-fg' };
      default:
        return { texto: 'Error', clase: 'bg-danger-soft text-danger' };
    }
  }
}
