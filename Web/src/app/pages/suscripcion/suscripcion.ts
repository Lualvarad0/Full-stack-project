import { afterRenderEffect, ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { LucideCheck, LucideCircleAlert, LucideCreditCard, LucideLoader, LucideLockKeyhole, LucideShieldCheck } from '@lucide/angular';
import { esPlan, ESTADOS_SUSCRIPCION, PLANES } from '@core/constants';
import { CatalogoPagos, Periodicidad, PlanSuscripcion, Suscripcion, TarjetaKushki } from '@core/models';
import { AuthService, AvisosService, SuscripcionService } from '@core/services';
import { mensajeDeError } from '@core/utils';
import { MensajeError, Segmentado, Titulo } from '@shared/components';
import { DineroPipe } from '@shared/pipes';

/**
 * Suscripción a un plan de pago: elige plan y periodicidad, ingresa la tarjeta en los campos
 * seguros de Kushki (la tarjeta no pasa por Nudo) y empieza la prueba gratis.
 */
@Component({
  selector: 'app-suscripcion',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensajeError, RouterLink, DatePipe, Titulo, Segmentado, DineroPipe, LucideCheck, LucideCreditCard, LucideLoader, LucideLockKeyhole, LucideShieldCheck, LucideCircleAlert],
  template: `
    <app-titulo linea1="Tu plan de" linea2="Nudo" subtitulo="Paga con tarjeta de crédito o débito. Puedes cancelar cuando quieras desde Configuración." />

    @if (mensajeMotivo() && !actual()) {
      <p class="aviso-atencion mt-6">
        <svg lucideLockKeyhole class="mt-0.5 size-4 shrink-0 text-warning" aria-hidden="true"></svg>
        {{ mensajeMotivo() }} Pruébalo {{ catalogo()?.diasDePrueba ?? 14 }} días gratis.
      </p>
    }

    @if (cargando()) {
      <div class="card mt-6 h-64 animate-pulse"></div>
    } @else if (actual(); as s) {
      <!-- Ya tiene un plan de pago: se muestra su estado y se puede cancelar -->
      <section class="card mt-6 max-w-2xl p-5 sm:p-6">
        <p class="text-sm text-muted-fg">Plan actual</p>
        <p class="titular mt-1 text-4xl">{{ s.plan }}</p>
        <dl class="mt-5 grid gap-4 text-sm sm:grid-cols-3">
          <div><dt class="text-muted-fg">Estado</dt><dd class="font-medium" [class.text-warning]="s.estado === 'PagoPendiente'">{{ estados[s.estado] }}</dd></div>
          <div><dt class="text-muted-fg">{{ s.estado === 'EnPrueba' ? 'Primer cobro' : 'Próximo cobro' }}</dt><dd class="font-medium">{{ s.proximoCobro | date: 'dd/MM/yyyy' }} · {{ s.total | dinero }}</dd></div>
          <div><dt class="text-muted-fg">Tarjeta</dt><dd class="font-medium">{{ s.marcaTarjeta ?? 'Tarjeta' }} •••• {{ s.ultimosDigitos ?? '····' }}</dd></div>
        </dl>
        @if (s.estado === 'PagoPendiente') {
          <p class="aviso-atencion mt-5">
            <svg lucideCircleAlert class="mt-0.5 size-4 shrink-0 text-warning" aria-hidden="true"></svg>
            No pudimos cobrar tu tarjeta. Kushki lo intentará de nuevo; si sigue fallando, volverás al plan Personal.
          </p>
        }
        <div class="mt-6 flex flex-wrap items-center gap-3">
          @if (confirmarCancelar()) {
            <span class="text-sm">¿Cancelar el plan? No habrá más cobros y vuelves al plan Personal ahora mismo.</span>
            <button type="button" class="btn btn-danger" [disabled]="trabajando()" (click)="cancelar()">Sí, cancelar</button>
            <button type="button" class="btn btn-ghost" (click)="confirmarCancelar.set(false)">No</button>
          } @else {
            <button type="button" class="btn btn-secondary" (click)="confirmarCancelar.set(true)">Cancelar plan</button>
          }
        </div>
      </section>
    } @else if (catalogo(); as c) {
      <div class="mt-6 grid gap-6 lg:grid-cols-[1fr_22rem]">
        <div class="min-w-0 space-y-6">
          <section class="card p-5 sm:p-6" aria-labelledby="titulo-elegir">
            <div class="flex flex-wrap items-center justify-between gap-3">
              <h2 id="titulo-elegir" class="font-semibold">1. Elige tu plan</h2>
              <app-segmentado [opciones]="periodos" [valor]="periodicidad()" (valorChange)="periodicidad.set($event)" etiqueta="Periodicidad del pago" />
            </div>
            <div class="mt-4 grid gap-3 sm:grid-cols-2" role="radiogroup" aria-label="Plan">
              @for (p of planesDePago; track p.id) {
                <label class="flex cursor-pointer flex-col rounded-xl border p-4 transition-colors"
                  [class]="plan() === p.id ? 'border-accent bg-primary-soft' : 'border-border hover:bg-muted'">
                  <span class="flex items-center gap-2">
                    <input type="radio" name="plan" class="size-4 accent-[var(--accent)]" [checked]="plan() === p.id" (change)="plan.set(p.id)" />
                    <span class="font-semibold">{{ p.nombre }}</span>
                    @if (p.destacado) { <span class="pildora ml-auto bg-brand text-[11px] text-brand-ink">Recomendado</span> }
                  </span>
                  <span class="mt-2 text-sm"><span class="text-lg font-semibold">{{ subtotalDe(c, p.id) | dinero }}</span>
                    <span class="text-muted-fg"> {{ periodicidad() === 'Anual' ? '/año' : '/mes' }} + IVA</span></span>
                  <span class="mt-1 text-xs text-muted-fg">{{ p.descripcion }}</span>
                </label>
              }
            </div>
          </section>

          <section class="card p-5 sm:p-6" aria-labelledby="titulo-tarjeta">
            <h2 id="titulo-tarjeta" class="flex items-center gap-2 font-semibold">
              <svg lucideCreditCard class="size-5 text-muted-fg" aria-hidden="true"></svg> 2. Tu tarjeta
            </h2>
            @if (!c.pagosDisponibles) {
              <p class="aviso-atencion mt-4">
                <svg lucideCircleAlert class="mt-0.5 size-4 shrink-0 text-warning" aria-hidden="true"></svg>
                Los pagos aún no están habilitados en este servidor. Vuelve pronto.
              </p>
            } @else {
              <!-- Kushki dibuja aquí sus campos seguros (iframes) -->
              <div class="mt-4 grid gap-4 sm:grid-cols-2">
                <div class="sm:col-span-2"><span class="field-label">Nombre en la tarjeta</span><div id="kushki-titular" class="min-h-11"></div></div>
                <div class="sm:col-span-2"><span class="field-label">Número de tarjeta</span><div id="kushki-numero" class="min-h-11"></div></div>
                <div><span class="field-label">Vencimiento</span><div id="kushki-vencimiento" class="min-h-11"></div></div>
                <div><span class="field-label">CVV</span><div id="kushki-cvv" class="min-h-11"></div></div>
              </div>
              @if (!tarjetaLista() && !errorTarjeta()) {
                <p class="mt-3 flex items-center gap-2 text-sm text-muted-fg"><svg lucideLoader class="size-4 animate-spin" aria-hidden="true"></svg> Cargando el formulario seguro…</p>
              }
              @if (c.kushkiPruebas) {
                <p class="mt-3 text-xs text-muted-fg">Ambiente de pruebas de Kushki: usa la tarjeta 5451 9515 7492 5480, cualquier CVV y una fecha futura.</p>
              }
              <p class="mt-4 flex items-start gap-2 text-xs text-muted-fg">
                <svg lucideLockKeyhole class="mt-0.5 size-3.5 shrink-0" aria-hidden="true"></svg>
                Pagos procesados por Kushki. Nudo no ve ni guarda los datos de tu tarjeta.
              </p>
            }
            <app-mensaje-error class="mt-4 block" [mensaje]="errorTarjeta()" />
          </section>
        </div>

        <aside class="card h-fit p-5 lg:sticky lg:top-24" aria-labelledby="titulo-resumen">
          <h2 id="titulo-resumen" class="font-semibold">Resumen</h2>
          @if (precio(); as pr) {
            <dl class="mt-4 space-y-2 text-sm">
              <div class="flex justify-between"><dt class="text-muted-fg">Plan {{ plan() }} {{ periodicidad() === 'Anual' ? 'anual' : 'mensual' }}</dt><dd class="money">{{ pr.subtotal | dinero }}</dd></div>
              <div class="flex justify-between"><dt class="text-muted-fg">IVA 15 %</dt><dd class="money">{{ pr.iva | dinero }}</dd></div>
              <div class="flex justify-between border-t border-border pt-2 font-semibold"><dt>Total {{ periodicidad() === 'Anual' ? 'por año' : 'por mes' }}</dt><dd class="money">{{ pr.total | dinero }}</dd></div>
            </dl>
            <div class="mt-4 rounded-lg bg-success-soft p-3 text-sm">
              <p class="font-semibold text-success">Hoy pagas $0,00</p>
              <p class="mt-0.5 text-muted-fg">{{ c.diasDePrueba }} días gratis. Primer cobro el {{ primerCobro() | date: 'dd/MM/yyyy' }}.</p>
            </div>
          }
          <ul class="mt-4 space-y-1.5 text-sm text-muted-fg">
            <li class="flex gap-2"><svg lucideCheck class="mt-0.5 size-4 shrink-0 text-success" aria-hidden="true"></svg> Cancela cuando quieras</li>
            <li class="flex gap-2"><svg lucideCheck class="mt-0.5 size-4 shrink-0 text-success" aria-hidden="true"></svg> Te avisamos cada vez que se cobra</li>
          </ul>

          <app-mensaje-error class="mt-4 block" [mensaje]="error()" />

          <button type="button" class="btn btn-primary mt-5 min-h-11 w-full" [disabled]="!tarjetaLista() || trabajando()" (click)="pagar()">
            @if (trabajando()) { <svg lucideLoader class="size-4 animate-spin" aria-hidden="true"></svg> Procesando… }
            @else { <svg lucideShieldCheck class="size-4" aria-hidden="true"></svg> Empezar {{ c.diasDePrueba }} días gratis }
          </button>
          <a routerLink="/resumen" class="btn btn-ghost mt-2 w-full">Seguir con el plan Personal</a>
        </aside>
      </div>
    }
  `,
})
export class SuscripcionPagina implements OnInit {
  /** Query param ?plan= (desde el registro o la landing). */
  readonly planInicial = input<string | undefined>(undefined, { alias: 'plan' });
  /** Sección que el usuario intentó abrir sin el plan Profesional (?motivo=ingresos). */
  readonly motivo = input<string | undefined>();
  protected readonly mensajeMotivo = computed(() => ({
    ingresos: 'Tus ingresos y clientes son parte del plan Profesional.',
    clientes: 'Tus ingresos y clientes son parte del plan Profesional.',
    facturacion: 'La facturación electrónica es parte del plan Profesional.',
    filtros: 'Los filtros por fecha y monto son parte del plan Profesional.',
  } as Record<string, string>)[this.motivo() ?? ''] ?? null);

  private readonly servicio = inject(SuscripcionService);
  private readonly avisos = inject(AvisosService);
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);

  protected readonly estados = ESTADOS_SUSCRIPCION;
  protected readonly planesDePago = PLANES.filter((p) => p.id !== 'Personal');
  protected readonly periodos = [
    { valor: 'Mensual', etiqueta: 'Mensual' },
    { valor: 'Anual', etiqueta: 'Anual · 2 meses gratis' },
  ] as const;

  protected readonly cargando = signal(true);
  protected readonly catalogo = signal<CatalogoPagos | null>(null);
  protected readonly actual = signal<Suscripcion | null>(null);
  protected readonly plan = signal<PlanSuscripcion>('Profesional');
  protected readonly periodicidad = signal<Periodicidad>('Mensual');
  protected readonly tarjetaLista = signal(false);
  protected readonly errorTarjeta = signal<string | null>(null);
  protected readonly trabajando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly confirmarCancelar = signal(false);

  protected readonly precio = computed(() =>
    this.catalogo()?.precios.find((p) => p.plan === this.plan() && p.periodicidad === this.periodicidad()) ?? null);
  protected readonly primerCobro = computed(() => {
    const fecha = new Date();
    fecha.setDate(fecha.getDate() + (this.catalogo()?.diasDePrueba ?? 14));
    return fecha;
  });

  private tarjeta: TarjetaKushki | null = null;
  private iniciando = false;

  constructor() {
    this.servicio.catalogo().subscribe({
      next: (c) => {
        this.catalogo.set(c);
        this.servicio.actual().subscribe({
          next: (s) => {
            this.actual.set(s);
            this.cargando.set(false);
          },
          error: (e) => this.fallo(e),
        });
      },
      error: (e) => this.fallo(e),
    });

    // Los campos de Kushki se montan cuando sus contenedores ya están en el DOM.
    afterRenderEffect(() => {
      const c = this.catalogo();
      if (!c?.pagosDisponibles || !c.clavePublicaKushki || this.actual() || this.cargando() || this.iniciando) return;
      this.iniciando = true;
      void this.montarTarjeta(c.clavePublicaKushki, c.kushkiPruebas);
    });
  }

  ngOnInit(): void {
    const sugerido = this.planInicial();
    if (esPlan(sugerido) && sugerido !== 'Personal') this.plan.set(sugerido);
  }

  protected subtotalDe(c: CatalogoPagos, plan: PlanSuscripcion): number {
    return c.precios.find((p) => p.plan === plan && p.periodicidad === this.periodicidad())?.subtotal ?? 0;
  }

  protected async pagar(): Promise<void> {
    if (!this.tarjeta) return;
    this.error.set(null);
    this.trabajando.set(true);
    try {
      const respuesta = await this.tarjeta.requestToken();
      this.servicio.suscribir(this.plan(), this.periodicidad(), respuesta.token, respuesta.cardInfo?.brand, respuesta.cardInfo?.lastFourDigits).subscribe({
        next: () => {
          this.trabajando.set(false);
          this.avisos.exito(`¡Listo! Ya tienes el plan ${this.plan()}. Disfruta tus días gratis.`);
          void this.router.navigateByUrl(this.plan() === 'Personal' ? '/resumen' : '/facturacion');
        },
        error: (e) => {
          this.trabajando.set(false);
          this.error.set(mensajeDeError(e));
        },
      });
    } catch (e) {
      // Errores de Kushki al validar la tarjeta (campos incompletos, rechazo, 3DS…).
      this.trabajando.set(false);
      this.error.set((e as { message?: string }).message ?? 'Revisa los datos de tu tarjeta.');
    }
  }

  protected cancelar(): void {
    this.trabajando.set(true);
    this.servicio.cancelar().subscribe({
      next: () => {
        this.trabajando.set(false);
        this.confirmarCancelar.set(false);
        this.actual.set(null);
        this.iniciando = false;
        this.tarjetaLista.set(false);
        this.avisos.exito('Cancelaste tu plan. Volviste al plan Personal.');
      },
      error: (e) => {
        this.trabajando.set(false);
        this.avisos.error(e);
      },
    });
  }

  private async montarTarjeta(clavePublica: string, pruebas: boolean): Promise<void> {
    try {
      const [{ init }, { initCardToken }] = await Promise.all([import('@kushki/js-sdk'), import('@kushki/js-sdk/Card')]);
      const kushki = await init({ publicCredentialId: clavePublica, inTest: pruebas });
      // Los campos viven en iframes de Kushki: no heredan los tokens CSS, así que se pasan los colores del tema claro.
      const estiloCampo = {
        width: '100%', height: '44px', padding: '0 12px', 'border-radius': '10px', border: '1px solid #e2e5e9',
        'font-size': '15px', 'font-family': 'Inter, system-ui, sans-serif', color: '#252b31', 'background-color': '#ffffff',
      };
      this.tarjeta = await initCardToken(kushki, {
        currency: 'USD',
        isSubscription: true,
        fullResponse: true,
        fields: {
          cardholderName: { selector: 'kushki-titular' },
          cardNumber: { selector: 'kushki-numero' },
          expirationDate: { selector: 'kushki-vencimiento' },
          cvv: { selector: 'kushki-cvv' },
        },
        styles: { input: estiloCampo, focus: { border: '1px solid #6467f1', outline: 'none' }, invalid: { border: '1px solid #dc2626' } },
      }) as unknown as TarjetaKushki;
      this.tarjetaLista.set(true);
    } catch (e) {
      this.errorTarjeta.set(`No se pudo cargar el formulario de pago. ${(e as { message?: string }).message ?? ''}`.trim());
    }
  }

  private fallo(e: unknown): void {
    this.cargando.set(false);
    this.error.set(mensajeDeError(e));
  }
}
