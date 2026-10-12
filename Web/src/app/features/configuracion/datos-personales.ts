import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LucideBadgeCheck, LucideLoader, LucideMailWarning, LucideUserRound } from '@lucide/angular';
import { CuentaUsuarioService, DatosPersonales } from '@core/perfil';
import { mensajeDeError } from '@core/http';
import { AvisosService } from '@core/ui';
import { fechaIso } from '@core/utils';
import { MensajeError } from '@shared/components';

/** Fecha más reciente aceptada: hay que ser mayor de edad (igual que al registrarse). */
function fechaMaxima(): string {
  const hoy = new Date();
  return fechaIso(new Date(hoy.getFullYear() - 18, hoy.getMonth(), hoy.getDate()));
}

/** Configuración → Mi perfil: nombre, nacimiento y datos de contacto. */
@Component({
  selector: 'app-datos-personales',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    MensajeError,
    LucideLoader,
    LucideUserRound,
    LucideBadgeCheck,
    LucideMailWarning,
  ],
  template: `
    <form
      class="card p-5 sm:p-6"
      [formGroup]="form"
      (ngSubmit)="guardar()"
      novalidate
      aria-labelledby="titulo-datos-personales"
    >
      <div class="flex items-start gap-3">
        <span class="grid size-10 shrink-0 place-items-center rounded-xl bg-primary-soft text-primary">
          <svg lucideUserRound class="size-5" aria-hidden="true"></svg>
        </span>
        <div class="min-w-0">
          <h2 id="titulo-datos-personales" class="font-semibold">Datos personales</h2>
          <p class="mt-1 text-sm text-muted-fg">
            Así te mostramos en Nudo y te contactamos si hace falta.
            @if (datos(); as d) {
              Miembro desde {{ d.miembroDesde | date: 'MMMM yyyy' }}.
            }
          </p>
        </div>
      </div>

      @if (cargando()) {
        <div class="mt-5 h-48 animate-pulse rounded-lg bg-muted"></div>
      } @else {
        <div class="mt-5 grid gap-5 sm:grid-cols-2">
          <div>
            <label class="field-label" for="dp-nombre">Nombre</label>
            <input id="dp-nombre" class="input" formControlName="nombre" autocomplete="given-name" maxlength="100"
              [attr.aria-invalid]="invalido('nombre')" />
          </div>
          <div>
            <label class="field-label" for="dp-apellido">Apellido</label>
            <input id="dp-apellido" class="input" formControlName="apellido" autocomplete="family-name" maxlength="100"
              [attr.aria-invalid]="invalido('apellido')" />
          </div>
          <div>
            <label class="field-label" for="dp-nacimiento">Fecha de nacimiento</label>
            <input id="dp-nacimiento" type="date" class="input" formControlName="fechaNacimiento" autocomplete="bday"
              [max]="maxNacimiento" min="1900-01-01" [attr.aria-invalid]="invalido('fechaNacimiento')" />
          </div>
          <div>
            <label class="field-label" for="dp-correo">Correo</label>
            <input id="dp-correo" class="input bg-muted/60" [value]="datos()?.correo ?? ''" readonly
              aria-describedby="dp-correo-ayuda" />
            <div id="dp-correo-ayuda" class="mt-1.5 space-y-0.5 text-xs text-muted-fg">
              <p class="flex flex-wrap items-center gap-1.5">
                @if (datos()?.correoVerificado) {
                  <svg lucideBadgeCheck class="size-3.5 text-success" aria-hidden="true"></svg> Correo verificado.
                } @else {
                  <svg lucideMailWarning class="size-3.5 text-warning" aria-hidden="true"></svg> Correo sin verificar.
                  <a routerLink="/configuracion" [queryParams]="{ seccion: 'sistema' }" fragment="seguridad" class="enlace"
                    >Verificarlo en Seguridad</a
                  >
                }
              </p>
              <p>Es tu usuario para ingresar; por seguridad no se cambia aquí.</p>
            </div>
          </div>
          <div>
            <label class="field-label" for="dp-telefono"
              >Teléfono <span class="font-normal text-muted-fg">(opcional)</span></label
            >
            <input id="dp-telefono" type="tel" class="input" formControlName="telefono" autocomplete="tel"
              inputmode="tel" maxlength="20" placeholder="099 123 4567" [attr.aria-invalid]="invalido('telefono')"
              aria-describedby="dp-telefono-ayuda" />
            <p id="dp-telefono-ayuda" class="mt-1.5 text-xs" [class.text-danger]="invalido('telefono')"
              [class.text-muted-fg]="!invalido('telefono')">
              {{ invalido('telefono') ? 'Solo números, espacios, guiones y + al inicio (7 a 20).' : 'Celular o fijo, con o sin +593.' }}
            </p>
          </div>
          <div>
            <label class="field-label" for="dp-ocupacion"
              >Ocupación <span class="font-normal text-muted-fg">(opcional)</span></label
            >
            <input id="dp-ocupacion" class="input" formControlName="ocupacion" autocomplete="organization-title"
              maxlength="120" placeholder="Diseñadora, médico, consultor…" />
          </div>
          <div>
            <label class="field-label" for="dp-ciudad"
              >Ciudad <span class="font-normal text-muted-fg">(opcional)</span></label
            >
            <input id="dp-ciudad" class="input" formControlName="ciudad" autocomplete="address-level2" maxlength="100" />
          </div>
          <div>
            <label class="field-label" for="dp-direccion"
              >Dirección <span class="font-normal text-muted-fg">(opcional)</span></label
            >
            <input id="dp-direccion" class="input" formControlName="direccion" autocomplete="street-address"
              maxlength="300" />
          </div>
        </div>

        <app-mensaje-error class="mt-4 block" [mensaje]="error()" />
        <div class="mt-5 flex flex-wrap items-center gap-3">
          <button type="submit" class="btn btn-primary" [disabled]="guardando() || form.pristine">
            @if (guardando()) {
              <svg lucideLoader class="size-4 animate-spin" aria-hidden="true"></svg>
            }
            Guardar datos
          </button>
          @if (!form.pristine) {
            <button type="button" class="btn btn-ghost" (click)="deshacer()">Deshacer cambios</button>
          }
        </div>
      }
    </form>
  `,
})
export class DatosPersonalesTarjeta {
  private readonly servicio = inject(CuentaUsuarioService);
  private readonly avisos = inject(AvisosService);

  protected readonly maxNacimiento = fechaMaxima();
  protected readonly datos = signal<DatosPersonales | null>(null);
  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    nombre: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    apellido: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    fechaNacimiento: ['', Validators.required],
    telefono: ['', Validators.pattern(/^\+?[0-9\s-]{7,20}$/)],
    ciudad: ['', Validators.maxLength(100)],
    direccion: ['', Validators.maxLength(300)],
    ocupacion: ['', Validators.maxLength(120)],
  });

  constructor() {
    this.servicio.datos().subscribe({
      next: (d) => {
        this.cargando.set(false);
        this.rellenar(d);
      },
      error: (e) => {
        this.cargando.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }

  protected invalido(campo: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[campo];
    return c.invalid && (c.dirty || c.touched);
  }

  protected deshacer(): void {
    const d = this.datos();
    if (d) this.rellenar(d);
    this.error.set(null);
  }

  protected guardar(): void {
    this.form.markAllAsTouched();
    const v = this.form.getRawValue();
    if (this.form.invalid) {
      this.error.set('Revisa los campos marcados.');
      return;
    }
    if (v.fechaNacimiento > this.maxNacimiento) {
      this.error.set('Debes ser mayor de edad para usar Nudo.');
      return;
    }
    this.error.set(null);
    this.guardando.set(true);
    const opcional = (t: string) => t.trim() || null;
    this.servicio
      .guardarDatos({
        nombre: v.nombre.trim(),
        apellido: v.apellido.trim(),
        fechaNacimiento: v.fechaNacimiento,
        telefono: opcional(v.telefono),
        ciudad: opcional(v.ciudad),
        direccion: opcional(v.direccion),
        ocupacion: opcional(v.ocupacion),
      })
      .subscribe({
        next: ({ datos }) => {
          this.guardando.set(false);
          this.rellenar(datos);
          this.avisos.exito('Tus datos personales quedaron guardados.');
        },
        error: (e) => {
          this.guardando.set(false);
          this.error.set(mensajeDeError(e));
        },
      });
  }

  private rellenar(d: DatosPersonales): void {
    this.datos.set(d);
    this.form.reset({
      nombre: d.nombre,
      apellido: d.apellido,
      fechaNacimiento: d.fechaNacimiento ?? '',
      telefono: d.telefono ?? '',
      ciudad: d.ciudad ?? '',
      direccion: d.direccion ?? '',
      ocupacion: d.ocupacion ?? '',
    });
  }
}
