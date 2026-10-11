import { ChangeDetectionStrategy, Component, computed, inject, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { LucideFileKey, LucideLoader, LucideLockKeyhole, LucideShieldCheck, LucideTriangleAlert } from '@lucide/angular';
import { FirmaElectronica } from '@core/models';
import { AvisosService, FacturacionService } from '@core/services';
import { mensajeDeError } from '@core/utils';
import { CampoClave } from './campo-clave';
import { MensajeError } from './mensaje-error';
import { ZonaArchivos } from './zona-archivos';

/** Configuración → firma electrónica (.p12): subir, ver su vigencia, reemplazar o eliminar. */
@Component({
  selector: 'app-firma-tarjeta',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensajeError, CampoClave, ZonaArchivos, ReactiveFormsModule, DatePipe, LucideFileKey, LucideLoader, LucideShieldCheck, LucideTriangleAlert, LucideLockKeyhole],
  template: `
    <section class="card p-5 sm:p-6" aria-labelledby="titulo-firma">
      <div class="flex items-start gap-3">
        <span class="grid size-10 shrink-0 place-items-center rounded-xl bg-primary-soft text-primary">
          <svg lucideFileKey class="size-5" aria-hidden="true"></svg>
        </span>
        <div class="min-w-0">
          <h2 id="titulo-firma" class="font-semibold">Firma electrónica</h2>
          <p class="mt-1 text-sm text-muted-fg">
            Con tu archivo <span class="font-mono">.p12</span> firmamos las facturas que emites. Se guarda cifrado junto con su contraseña
            y solo se usa al emitir.
          </p>
        </div>
      </div>

      @if (cargando()) {
        <div class="mt-5 h-24 animate-pulse rounded-lg bg-muted"></div>
      } @else if (firma() && !reemplazando()) {
        @let f = firma()!;
        <dl class="mt-5 grid gap-4 rounded-lg bg-muted/60 p-4 text-sm sm:grid-cols-2">
          <div><dt class="text-muted-fg">Titular</dt><dd class="font-medium">{{ f.titular }}</dd></div>
          <div><dt class="text-muted-fg">Cédula o RUC del certificado</dt><dd class="font-mono">{{ f.identificacion ?? 'No indicado' }}</dd></div>
          <div><dt class="text-muted-fg">Entidad certificadora</dt><dd>{{ f.emisor }}</dd></div>
          <div>
            <dt class="text-muted-fg">Vigencia</dt>
            <dd [class.text-danger]="!f.vigente" [class.text-warning]="f.vigente && f.diasRestantes <= 30">
              Hasta el {{ f.validaHasta | date: 'dd/MM/yyyy' }}
              @if (f.vigente) { · faltan {{ f.diasRestantes }} días } @else { · vencida }
            </dd>
          </div>
        </dl>

        @if (f.coincideConPerfil === false) {
          <p class="aviso-atencion mt-4">
            <svg lucideTriangleAlert class="mt-0.5 size-4 shrink-0 text-warning" aria-hidden="true"></svg>
            La cédula o RUC de la firma no coincide con tu perfil. El SRI rechaza facturas firmadas por otra persona.
          </p>
        }
        @if (f.vigente && f.diasRestantes <= 30) {
          <p class="aviso-atencion mt-4">
            <svg lucideTriangleAlert class="mt-0.5 size-4 shrink-0 text-warning" aria-hidden="true"></svg>
            Tu firma vence pronto. Renuévala con tu entidad certificadora y súbela aquí.
          </p>
        }

        <div class="mt-5 flex flex-wrap items-center gap-3">
          <button type="button" class="btn btn-secondary" (click)="reemplazando.set(true)">Reemplazar firma</button>
          @if (confirmarEliminar()) {
            <span class="text-sm">¿Eliminarla? No podrás facturar hasta subir otra.</span>
            <button type="button" class="btn btn-danger" [disabled]="trabajando()" (click)="eliminar()">Sí, eliminar</button>
            <button type="button" class="btn btn-ghost" (click)="confirmarEliminar.set(false)">Cancelar</button>
          } @else {
            <button type="button" class="btn btn-ghost text-danger" (click)="confirmarEliminar.set(true)">Eliminar</button>
          }
        </div>
      } @else {
        <form class="mt-5 space-y-4" (submit)="$event.preventDefault(); subir()">
          <app-zona-archivos idCampo="archivo-firma" accept=".p12,.pfx,application/x-pkcs12" (archivos)="elegir($event[0])">
            @if (archivo(); as a) {
              <span class="font-medium">{{ a.name }}</span>
              <span class="text-sm text-accent underline">Elegir otro archivo</span>
            } @else {
              <span class="font-medium">Arrastra tu archivo .p12 o <span class="text-accent underline">elígelo</span></span>
              <span class="text-sm text-muted-fg">Lo entrega tu entidad certificadora (Banco Central, Security Data, Uanataca, ANF…)</span>
            }
          </app-zona-archivos>

          <div>
            <label class="field-label" for="clave-firma">Contraseña de la firma</label>
            <app-campo-clave class="max-w-sm" idCampo="clave-firma" autocomplete="off" [formControl]="claveControl" />
            <p class="mt-1.5 flex items-center gap-1.5 text-xs text-muted-fg">
              <svg lucideLockKeyhole class="size-3.5" aria-hidden="true"></svg> Es la clave que elegiste al obtener la firma, no la del SRI.
            </p>
          </div>

          <app-mensaje-error class="block" [mensaje]="error()" />

          <div class="flex flex-wrap items-center gap-3">
            <button type="submit" class="btn btn-primary" [disabled]="!puedeSubir()">
              @if (trabajando()) { <svg lucideLoader class="size-4 animate-spin" aria-hidden="true"></svg> } @else { <svg lucideShieldCheck class="size-4" aria-hidden="true"></svg> }
              Verificar y guardar
            </button>
            @if (firma()) {
              <button type="button" class="btn btn-ghost" (click)="cancelar()">Cancelar</button>
            }
          </div>
        </form>
      }
    </section>
  `,
})
export class FirmaTarjeta {
  private readonly servicio = inject(FacturacionService);
  private readonly avisos = inject(AvisosService);

  /** Avisa al padre (por ejemplo, para recalcular lo que falta para facturar). */
  readonly cambio = output<FirmaElectronica | null>();

  protected readonly firma = signal<FirmaElectronica | null>(null);
  protected readonly cargando = signal(true);
  protected readonly reemplazando = signal(false);
  protected readonly confirmarEliminar = signal(false);
  protected readonly archivo = signal<File | null>(null);
  protected readonly claveControl = new FormControl('', { nonNullable: true });
  private readonly clave = toSignal(this.claveControl.valueChanges, { initialValue: '' });
  protected readonly trabajando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly puedeSubir = computed(() => !!this.archivo() && this.clave().length > 0 && !this.trabajando());

  constructor() {
    this.servicio.firma().subscribe({
      next: (f) => {
        this.firma.set(f);
        this.cargando.set(false);
      },
      error: (e) => {
        this.cargando.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }

  protected elegir(archivo: File): void {
    if (!/\.(p12|pfx)$/i.test(archivo.name)) {
      this.error.set('Elige el archivo de tu firma: termina en .p12 (o .pfx).');
      return;
    }
    this.error.set(null);
    this.archivo.set(archivo);
  }

  protected subir(): void {
    const archivo = this.archivo();
    if (!archivo || !this.puedeSubir()) return;
    this.trabajando.set(true);
    this.error.set(null);
    this.servicio.subirFirma(archivo, this.clave()).subscribe({
      next: (f) => {
        this.trabajando.set(false);
        this.firma.set(f);
        this.cancelar();
        this.avisos.exito('Firma verificada y guardada. Ya puedes firmar tus facturas.');
        this.cambio.emit(f);
      },
      error: (e) => {
        this.trabajando.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }

  protected eliminar(): void {
    this.trabajando.set(true);
    this.servicio.eliminarFirma().subscribe({
      next: () => {
        this.trabajando.set(false);
        this.confirmarEliminar.set(false);
        this.firma.set(null);
        this.avisos.exito('Firma eliminada.');
        this.cambio.emit(null);
      },
      error: (e) => {
        this.trabajando.set(false);
        this.avisos.error(e);
      },
    });
  }

  protected cancelar(): void {
    this.reemplazando.set(false);
    this.archivo.set(null);
    this.claveControl.reset('');
    this.error.set(null);
  }
}
