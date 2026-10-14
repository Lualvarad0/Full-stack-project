import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideBadgeCheck,
  LucideCreditCard,
  LucideFileScan,
  LucideLoader,
  LucidePencil,
  LucidePlus,
  LucideShieldCheck,
  LucideTrash2,
} from '@lucide/angular';
import { CorreoService } from '@core/correo';
import { cargar, mensajeDeError } from '@core/http';
import {
  EstadoCuenta,
  EstadoCuentaRequest,
  formatearNumero,
  luhnValido,
  marcaPorNumero,
  partesPublicas,
  soloDigitos,
  Tarjeta,
  TarjetasService,
} from '@core/tarjetas';
import { AvisosService } from '@core/ui';
import { ErrorCarga, MensajeError, Titulo, ZonaArchivos } from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';
import { catchError, of } from 'rxjs';
import { EstadoCuentaForm } from './estado-cuenta-form';

/** Panel abierto en una tarjeta: subir el PDF del estado de cuenta o escribirlo a mano. */
type Panel = { tarjetaId: number; tipo: 'subir' | 'manual' } | null;

/**
 * Tarjetas de crédito (planes de pago): se registran verificando el banco emisor por el BIN y su deuda viene
 * del estado de cuenta mensual, que se confirma antes de anotarse en Mis cuentas.
 */
@Component({
  selector: 'app-tarjetas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    Titulo,
    ErrorCarga,
    MensajeError,
    ZonaArchivos,
    EstadoCuentaForm,
    DineroPipe,
    FechaCortaPipe,
    LucideCreditCard,
    LucidePlus,
    LucideTrash2,
    LucideFileScan,
    LucidePencil,
    LucideBadgeCheck,
    LucideShieldCheck,
    LucideLoader,
  ],
  templateUrl: './tarjetas.html',
})
export class TarjetasPagina {
  private readonly api = inject(TarjetasService);
  private readonly avisos = inject(AvisosService);
  private readonly correo = inject(CorreoService);
  private readonly recargas = signal(0);
  protected readonly carga = cargar(
    () => this.recargas(),
    () => this.api.listar(),
  );
  protected readonly datos = computed(() => this.carga().datos);
  /** Dirección del buzón para reenviar los estados de cuenta (si falla, solo no se muestra). */
  protected readonly buzon = cargar(
    () => 0,
    () => this.correo.buzon().pipe(catchError(() => of(null))),
  );

  // Nueva tarjeta: el número solo vive en este campo (se borra al guardar).
  protected readonly alias = signal('');
  protected readonly numero = signal('');
  protected readonly guardandoTarjeta = signal(false);
  protected readonly errorTarjeta = signal<string | null>(null);
  protected readonly digitos = computed(() => soloDigitos(this.numero()));
  protected readonly marca = computed(() => (this.digitos().length >= 2 ? marcaPorNumero(this.digitos()) : null));
  protected readonly numeroValido = computed(() => luhnValido(this.digitos()));

  // Panel por tarjeta
  protected readonly panel = signal<Panel>(null);
  protected readonly archivo = signal<File | null>(null);
  protected readonly clave = signal('');
  protected readonly procesando = signal(false);
  protected readonly errorPanel = signal<string | null>(null);

  // Confirmación de estados leídos
  protected readonly confirmando = signal<number | null>(null);
  protected readonly errorConfirmar = signal<string | null>(null);

  protected tarjetaDe(estado: EstadoCuenta): Tarjeta | undefined {
    return this.datos()?.tarjetas.find((t) => t.id === estado.tarjetaId);
  }

  protected escribirNumero(valor: string): void {
    this.numero.set(formatearNumero(valor));
    this.errorTarjeta.set(null);
  }

  protected agregar(): void {
    const alias = this.alias().trim();
    if (alias.length < 2) return this.errorTarjeta.set('Ponle un nombre a la tarjeta (ej.: Visa del trabajo).');
    if (!this.numeroValido())
      return this.errorTarjeta.set('Ese número no es válido. Revisa los dígitos de tu tarjeta.');
    this.errorTarjeta.set(null);
    this.guardandoTarjeta.set(true);
    this.api.crear({ alias, ...partesPublicas(this.digitos()) }).subscribe({
      next: (t) => {
        this.guardandoTarjeta.set(false);
        this.alias.set('');
        this.numero.set('');
        this.avisos.exito(`Tarjeta de ${t.banco ?? t.marca} terminada en ${t.ultimos4} verificada.`);
        this.recargar();
      },
      error: (e) => {
        this.guardandoTarjeta.set(false);
        this.errorTarjeta.set(mensajeDeError(e));
      },
    });
  }

  protected eliminar(t: Tarjeta): void {
    if (!confirm(`¿Quitar la tarjeta "${t.alias}"? Las deudas ya anotadas en Mis cuentas se conservan.`)) return;
    this.api.eliminar(t.id).subscribe({
      next: () => {
        this.avisos.exito(`Quitaste "${t.alias}".`);
        this.recargar();
      },
      error: (e) => this.avisos.error(e),
    });
  }

  protected abrir(tarjetaId: number, tipo: 'subir' | 'manual'): void {
    const actual = this.panel();
    this.panel.set(actual?.tarjetaId === tarjetaId && actual.tipo === tipo ? null : { tarjetaId, tipo });
    this.archivo.set(null);
    this.clave.set('');
    this.errorPanel.set(null);
  }

  protected elegirArchivo(archivos: File[]): void {
    const pdf = archivos[0] ?? null;
    if (pdf && !/\.pdf$/i.test(pdf.name) && pdf.type !== 'application/pdf')
      return this.errorPanel.set('Elige el estado de cuenta en PDF.');
    this.errorPanel.set(null);
    this.archivo.set(pdf);
  }

  protected subir(tarjetaId: number): void {
    const archivo = this.archivo();
    if (!archivo) return this.errorPanel.set('Elige el PDF del estado de cuenta.');
    this.procesando.set(true);
    this.errorPanel.set(null);
    this.api.subirEstadoCuenta(tarjetaId, archivo, this.clave() || null).subscribe({
      next: () => {
        this.procesando.set(false);
        this.panel.set(null);
        this.clave.set('');
        this.avisos.exito('Leímos tu estado de cuenta. Revisa los datos y confírmalos.');
        this.recargar();
      },
      error: (e) => {
        this.procesando.set(false);
        this.errorPanel.set(mensajeDeError(e));
      },
    });
  }

  protected registrarManual(tarjetaId: number, estado: EstadoCuentaRequest): void {
    this.procesando.set(true);
    this.errorPanel.set(null);
    this.api.registrarManual(tarjetaId, estado).subscribe({
      next: () => {
        this.procesando.set(false);
        this.panel.set(null);
        this.avisos.exito('Anotamos la deuda de la tarjeta en Mis cuentas.');
        this.recargar();
      },
      error: (e) => {
        this.procesando.set(false);
        this.errorPanel.set(mensajeDeError(e));
      },
    });
  }

  protected confirmar(e: EstadoCuenta, estado: EstadoCuentaRequest): void {
    this.confirmando.set(e.id);
    this.errorConfirmar.set(null);
    this.api.confirmar(e.id, estado).subscribe({
      next: () => {
        this.confirmando.set(null);
        this.avisos.exito('Listo: la deuda de la tarjeta quedó en Mis cuentas.');
        this.recargar();
      },
      error: (err) => {
        this.confirmando.set(null);
        this.errorConfirmar.set(mensajeDeError(err));
      },
    });
  }

  protected descartar(e: EstadoCuenta): void {
    this.api.descartar(e.id).subscribe({
      next: () => this.recargar(),
      error: (err) => this.avisos.error(err),
    });
  }

  private recargar(): void {
    this.recargas.update((n) => n + 1);
  }
}
