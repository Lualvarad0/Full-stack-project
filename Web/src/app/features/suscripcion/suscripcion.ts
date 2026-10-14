import { TarjetaPago } from './tarjeta-pago';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  OnInit,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import {
  LucideCheck,
  LucideCircleAlert,
  LucideLoader,
  LucideLockKeyhole,
  LucideShieldCheck,
} from '@lucide/angular';
import {
  esPlan,
  ESTADOS_SUSCRIPCION,
  PLANES,
  CatalogoPagos,
  Periodicidad,
  PlanSuscripcion,
  Suscripcion,
  TarjetaKushki,
  SuscripcionService,
} from '@core/suscripcion';
import { AuthService } from '@core/auth';
import { AvisosService } from '@core/ui';
import { mensajeDeError } from '@core/http';
import { MensajeError, Segmentado, Titulo } from '@shared/components';
import { DineroPipe } from '@shared/pipes';
/**
 * Suscripción a un plan de pago: elige plan y periodicidad, ingresa la tarjeta en los campos
 * seguros de Kushki (la tarjeta no pasa por Nudo) y empieza la prueba gratis.
 */
@Component({
  selector: 'app-suscripcion',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    TarjetaPago,
    MensajeError,
    RouterLink,
    DatePipe,
    Titulo,
    Segmentado,
    DineroPipe,
    LucideCheck,
    LucideLoader,
    LucideLockKeyhole,
    LucideShieldCheck,
    LucideCircleAlert,
  ],
  templateUrl: './suscripcion.html',
})
export class SuscripcionPagina implements OnInit {
  /** Query param ?plan= (desde el registro o la landing). */
  readonly planInicial = input<string | undefined>(undefined, { alias: 'plan' });
  /** Query param ?periodicidad=Anual (desde los planes de la landing). */
  readonly periodicidadInicial = input<string | undefined>(undefined, { alias: 'periodicidad' });
  /** Sección que el usuario intentó abrir sin el plan Profesional (?motivo=ingresos). */
  readonly motivo = input<string | undefined>();
  protected readonly mensajeMotivo = computed(
    () =>
      (
        ({
          ingresos: 'Tus ingresos y clientes son parte del plan Profesional.',
          clientes: 'Tus ingresos y clientes son parte del plan Profesional.',
          facturacion: 'La facturación electrónica es parte del plan Profesional.',
          filtros: 'Los filtros por fecha y monto son parte del plan Profesional.',
          importar:
            'Subir tus facturas o traerlas del SRI es parte del plan Profesional. En el plan gratuito anotas tus cuentas a mano en Mis cuentas.',
          gastos: 'Tus gastos con facturas del SRI son parte del plan Profesional.',
          proveedores: 'Tus proveedores y sus categorías son parte del plan Profesional.',
        }) as Record<string, string>
      )[this.motivo() ?? ''] ?? null,
  );

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
  protected readonly trabajando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly confirmarCancelar = signal(false);

  protected readonly precio = computed(
    () =>
      this.catalogo()?.precios.find(
        (p) => p.plan === this.plan() && p.periodicidad === this.periodicidad(),
      ) ?? null,
  );
  protected readonly primerCobro = computed(() => {
    const fecha = new Date();
    fecha.setDate(fecha.getDate() + (this.catalogo()?.diasDePrueba ?? 14));
    return fecha;
  });

  private tarjeta: TarjetaKushki | null = null;

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
  }

  ngOnInit(): void {
    const sugerido = this.planInicial();
    if (esPlan(sugerido) && sugerido !== 'Personal') this.plan.set(sugerido);
    if (this.periodicidadInicial() === 'Anual') this.periodicidad.set('Anual');
  }

  protected subtotalDe(c: CatalogoPagos, plan: PlanSuscripcion): number {
    return (
      c.precios.find((p) => p.plan === plan && p.periodicidad === this.periodicidad())?.subtotal ??
      0
    );
  }

  protected async pagar(): Promise<void> {
    if (!this.tarjeta) return;
    this.error.set(null);
    this.trabajando.set(true);
    try {
      const respuesta = await this.tarjeta.requestToken();
      this.servicio
        .suscribir(
          this.plan(),
          this.periodicidad(),
          respuesta.token,
          respuesta.cardInfo?.brand,
          respuesta.cardInfo?.lastFourDigits,
        )
        .subscribe({
          next: () => {
            this.trabajando.set(false);
            this.avisos.exito(
              `¡Listo! Ya tienes el plan ${this.plan()}. Disfruta tus días gratis.`,
            );
            void this.router.navigateByUrl(
              this.plan() === 'Personal' ? '/resumen' : '/facturacion',
            );
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
        this.tarjeta = null;
        this.tarjetaLista.set(false);
        this.avisos.exito('Cancelaste tu plan. Volviste al plan Personal.');
      },
      error: (e) => {
        this.trabajando.set(false);
        this.avisos.error(e);
      },
    });
  }

  protected recibirTarjeta(tarjeta: TarjetaKushki): void {
    this.tarjeta = tarjeta;
    this.tarjetaLista.set(true);
  }

  private fallo(e: unknown): void {
    this.cargando.set(false);
    this.error.set(mensajeDeError(e));
  }
}
