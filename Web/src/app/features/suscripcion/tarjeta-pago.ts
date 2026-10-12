import {
  afterRenderEffect,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import {
  LucideCircleAlert,
  LucideCreditCard,
  LucideLoader,
  LucideLockKeyhole,
} from '@lucide/angular';
import { CatalogoPagos, TarjetaKushki } from '@core/suscripcion';
import { MensajeError } from '@shared/components';
/** Encapsula los campos seguros y la carga diferida del SDK; no realiza cobros. */
@Component({
  selector: 'app-tarjeta-pago',
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensajeError, LucideCreditCard, LucideCircleAlert, LucideLoader, LucideLockKeyhole],
  templateUrl: './tarjeta-pago.html',
})
export class TarjetaPago {
  readonly catalogo = input.required<CatalogoPagos>();
  readonly lista = output<TarjetaKushki>();
  protected readonly tarjetaLista = signal(false);
  protected readonly errorTarjeta = signal<string | null>(null);
  private readonly destroyRef = inject(DestroyRef);
  private iniciando = false;

  constructor() {
    afterRenderEffect(() => {
      const c = this.catalogo();
      if (!c.pagosDisponibles || !c.clavePublicaKushki || this.iniciando) return;
      this.iniciando = true;
      void this.montarTarjeta(c.clavePublicaKushki, c.kushkiPruebas);
    });
  }

  private async montarTarjeta(clavePublica: string, pruebas: boolean): Promise<void> {
    try {
      const [{ init }, { initCardToken }] = await Promise.all([
        import('@kushki/js-sdk'),
        import('@kushki/js-sdk/Card'),
      ]);
      if (this.destroyRef.destroyed) return;
      const kushki = await init({ publicCredentialId: clavePublica, inTest: pruebas });
      if (this.destroyRef.destroyed) return;
      const tarjeta = (await initCardToken(kushki, {
        currency: 'USD',
        isSubscription: true,
        fullResponse: true,
        fields: {
          cardholderName: { selector: 'kushki-titular' },
          cardNumber: { selector: 'kushki-numero' },
          expirationDate: { selector: 'kushki-vencimiento' },
          cvv: { selector: 'kushki-cvv' },
        },
        styles: {
          // Kushki resuelve estas clases de Tailwind antes de enviarlas a sus iframes.
          input: 'campo-tarjeta',
          focus: 'campo-tarjeta-focus',
          invalid: 'campo-tarjeta-invalid',
        },
      })) as unknown as TarjetaKushki;
      if (this.destroyRef.destroyed) return;
      this.tarjetaLista.set(true);
      this.lista.emit(tarjeta);
    } catch (e) {
      if (this.destroyRef.destroyed) return;
      this.errorTarjeta.set(
        `No se pudo cargar el formulario de pago. ${(e as { message?: string }).message ?? ''}`.trim(),
      );
    }
  }
}
