import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideLoader, LucideReceiptText } from '@lucide/angular';
import { AmbienteSri, DatosEmisor } from '@core/models';
import { AvisosService, FacturacionService } from '@core/services';
import { mensajeDeError } from '@core/utils';
import { Segmentado } from './controles';
import { MensajeError } from './mensaje-error';

/** Configuración → datos con los que se emiten las facturas (ambiente, punto de emisión, secuencial, dirección…). */
@Component({
  selector: 'app-datos-emisor-tarjeta',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MensajeError, ReactiveFormsModule, Segmentado, LucideLoader, LucideReceiptText],
  template: `
    <form class="card p-5 sm:p-6" [formGroup]="form" (ngSubmit)="guardar()" novalidate aria-labelledby="titulo-emision">
      <div class="flex items-start gap-3">
        <span class="grid size-10 shrink-0 place-items-center rounded-xl bg-primary-soft text-primary">
          <svg lucideReceiptText class="size-5" aria-hidden="true"></svg>
        </span>
        <div>
          <h2 id="titulo-emision" class="font-semibold">Datos de facturación</h2>
          <p class="mt-1 text-sm text-muted-fg">Así aparecerán tus facturas. Los encuentras en tu RUC (establecimiento y dirección).</p>
        </div>
      </div>

      <div class="mt-5">
        <span class="field-label">Ambiente del SRI</span>
        <app-segmentado [opciones]="ambientes" [valor]="form.controls.ambiente.value" (valorChange)="cambiarAmbiente($event)" etiqueta="Ambiente del SRI" />
        <p class="mt-2 text-xs" [class]="form.controls.ambiente.value === 'Produccion' ? 'text-warning' : 'text-muted-fg'">
          @if (form.controls.ambiente.value === 'Produccion') {
            Producción: las facturas tienen validez tributaria y se suman a tus ingresos.
          } @else {
            Pruebas: el SRI las valida pero no tienen validez tributaria. Úsalo para probar antes de facturar de verdad.
          }
        </p>
      </div>

      <div class="mt-5 grid gap-5 sm:grid-cols-3">
        <div>
          <label class="field-label" for="estab">Establecimiento</label>
          <input id="estab" class="input font-mono" formControlName="establecimiento" inputmode="numeric" maxlength="3" placeholder="001" />
        </div>
        <div>
          <label class="field-label" for="pto">Punto de emisión</label>
          <input id="pto" class="input font-mono" formControlName="puntoEmision" inputmode="numeric" maxlength="3" placeholder="001" />
        </div>
        <div>
          <label class="field-label" for="secuencial">Siguiente número</label>
          <input id="secuencial" type="number" min="1" class="input font-mono" formControlName="siguienteSecuencial" />
        </div>
        <p class="-mt-3 text-xs text-muted-fg sm:col-span-3">
          Si ya facturabas con otro sistema, continúa desde tu último número + 1 para no repetirlo.
        </p>

        <div class="sm:col-span-3">
          <label class="field-label" for="dirMatriz">Dirección matriz</label>
          <input id="dirMatriz" class="input" formControlName="direccionMatriz" autocomplete="street-address" maxlength="300" />
        </div>
        <div class="sm:col-span-3">
          <label class="field-label" for="dirEstab">Dirección del establecimiento <span class="font-normal text-muted-fg">(si es distinta)</span></label>
          <input id="dirEstab" class="input" formControlName="direccionEstablecimiento" maxlength="300" />
        </div>
        <div class="sm:col-span-2">
          <label class="field-label" for="comercial">Nombre comercial <span class="font-normal text-muted-fg">(opcional)</span></label>
          <input id="comercial" class="input" formControlName="nombreComercial" maxlength="300" />
        </div>
        <div>
          <label class="field-label" for="especial">Contribuyente especial N.º <span class="font-normal text-muted-fg">(opcional)</span></label>
          <input id="especial" class="input font-mono" formControlName="contribuyenteEspecial" inputmode="numeric" maxlength="13" />
        </div>
        <label class="flex items-center gap-2 text-sm sm:col-span-3">
          <input type="checkbox" class="size-4 accent-[var(--accent)]" formControlName="obligadoContabilidad" />
          Obligado a llevar contabilidad
        </label>
      </div>

      <app-mensaje-error class="mt-5 block" [mensaje]="error()" />

      <div class="mt-6 flex items-center gap-3">
        <button type="submit" class="btn btn-primary" [disabled]="guardando()">
          @if (guardando()) { <svg lucideLoader class="size-4 animate-spin" aria-hidden="true"></svg> }
          Guardar datos de facturación
        </button>
        @if (form.dirty) {
          <span class="text-sm text-muted-fg">Tienes cambios sin guardar.</span>
        }
      </div>
    </form>
  `,
})
export class DatosEmisorTarjeta {
  private readonly servicio = inject(FacturacionService);
  private readonly avisos = inject(AvisosService);
  private readonly fb = inject(NonNullableFormBuilder);

  readonly cambio = output<DatosEmisor>();

  protected readonly ambientes = [
    { valor: 'Pruebas', etiqueta: 'Pruebas' },
    { valor: 'Produccion', etiqueta: 'Producción' },
  ] as const;
  protected readonly guardando = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.group({
    ambiente: ['Pruebas' as AmbienteSri],
    establecimiento: ['001', [Validators.required, Validators.pattern(/^\d{3}$/)]],
    puntoEmision: ['001', [Validators.required, Validators.pattern(/^\d{3}$/)]],
    siguienteSecuencial: [1, [Validators.required, Validators.min(1)]],
    direccionMatriz: ['', [Validators.required, Validators.minLength(3)]],
    direccionEstablecimiento: [''],
    nombreComercial: [''],
    contribuyenteEspecial: [''],
    obligadoContabilidad: [false],
  });

  constructor() {
    this.servicio.datos().subscribe({
      next: (d) => this.form.reset({
        ambiente: d.ambiente,
        establecimiento: d.establecimiento,
        puntoEmision: d.puntoEmision,
        siguienteSecuencial: d.siguienteSecuencial,
        direccionMatriz: d.direccionMatriz,
        direccionEstablecimiento: d.direccionEstablecimiento ?? '',
        nombreComercial: d.nombreComercial ?? '',
        contribuyenteEspecial: d.contribuyenteEspecial ?? '',
        obligadoContabilidad: d.obligadoContabilidad,
      }),
      error: (e) => this.error.set(mensajeDeError(e)),
    });
  }

  protected cambiarAmbiente(ambiente: AmbienteSri): void {
    this.form.controls.ambiente.setValue(ambiente);
    this.form.markAsDirty();
  }

  protected guardar(): void {
    this.error.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Revisa los datos: establecimiento y punto de emisión son 3 dígitos, y la dirección matriz es obligatoria.');
      return;
    }
    const v = this.form.getRawValue();
    this.guardando.set(true);
    this.servicio.guardarDatos({
      ...v,
      siguienteSecuencial: Number(v.siguienteSecuencial),
      direccionMatriz: v.direccionMatriz.trim(),
      direccionEstablecimiento: v.direccionEstablecimiento.trim() || null,
      nombreComercial: v.nombreComercial.trim() || null,
      contribuyenteEspecial: v.contribuyenteEspecial.trim() || null,
    }).subscribe({
      next: (d) => {
        this.guardando.set(false);
        this.form.markAsPristine();
        this.avisos.exito('Datos de facturación guardados.');
        this.cambio.emit(d);
      },
      error: (e) => {
        this.guardando.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }
}
