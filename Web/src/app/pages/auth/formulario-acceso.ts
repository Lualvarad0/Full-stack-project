import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { LucideCircleCheck, LucideLoader } from '@lucide/angular';
import { AVISO_LANZAMIENTO, esDePago, esPlan, PLANES } from '@core/constants';
import { ModoAcceso, PlanSuscripcion } from '@core/models';
import { AuthService } from '@core/services';
import { mensajeDeError } from '@core/utils';
import { CampoClave, MensajeError } from '@shared/components';

/** Formulario de inicio de sesión o de registro; la pantalla de acceso muestra uno de cada lado. */
@Component({
  selector: 'app-formulario-acceso',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CampoClave, MensajeError, ReactiveFormsModule, RouterLink, LucideLoader, LucideCircleCheck],
  template: `
    <h1 class="titular text-[clamp(34px,5vw,44px)]">
      {{ registro() ? 'Crea tu' : 'Inicia' }} <em class="italic">{{ registro() ? 'cuenta' : 'sesión' }}</em>
    </h1>
    <p class="mt-2 text-sm leading-relaxed text-muted-fg">
      {{ registro() ? 'Gratis para empezar. Sin pedirte tu clave del SRI.' : 'Revisa tus ingresos, gastos y tu situación tributaria del mes.' }}
    </p>

    <app-mensaje-error class="mt-5 block" [mensaje]="error()" />

    @if (registro()) {
      <ol class="mt-5 flex items-center gap-2 text-xs font-medium" aria-label="Pasos del registro">
        @for (p of [1, 2]; track p) {
          <li class="flex items-center gap-2" [class.text-fg]="paso() >= p" [class.text-muted-fg]="paso() < p" [attr.aria-current]="paso() === p ? 'step' : null">
            <span class="grid size-6 place-items-center rounded-full" [class]="paso() >= p ? 'bg-ink text-ink-fg' : 'bg-muted'">{{ p }}</span>
            {{ p === 1 ? 'Tus datos' : 'Tu plan' }}
          </li>
          @if (p === 1) { <li class="h-px w-6 bg-border" aria-hidden="true"></li> }
        }
      </ol>
    }

    <form class="mt-6 space-y-4" [formGroup]="form" (ngSubmit)="enviar()" novalidate>
      @if (registro() && paso() === 2) {
        <fieldset>
          <legend class="field-label">Elige tu plan</legend>
          <div class="space-y-2.5">
            @for (p of planes; track p.id) {
              <label class="flex cursor-pointer items-start gap-3 rounded-xl border p-3.5 transition-colors"
                [class]="plan() === p.id ? 'border-accent bg-primary-soft' : 'border-border hover:bg-muted'">
                <input type="radio" class="mt-1 size-4 accent-[var(--accent)]" [name]="id('plan')" [value]="p.id" [checked]="plan() === p.id" (change)="plan.set(p.id)" />
                <span class="min-w-0 flex-1">
                  <span class="flex items-baseline justify-between gap-2">
                    <span class="font-semibold">{{ p.nombre }}</span>
                    <span class="text-sm"><span class="font-semibold">{{ p.precio }}</span><span class="text-muted-fg">{{ p.periodo }}</span></span>
                  </span>
                  <span class="mt-0.5 block text-sm text-muted-fg">{{ p.descripcion }}</span>
                  @if (p.destacado) {
                    <span class="pildora mt-2 bg-brand text-[11px] text-brand-ink">Recomendado si facturas</span>
                  }
                </span>
              </label>
            }
          </div>
          <p class="mt-3 text-xs text-muted-fg">{{ avisoLanzamiento }}</p>
        </fieldset>
      } @else {
      @if (registro()) {
        <div class="grid gap-4 sm:grid-cols-2">
          <div>
            <label class="field-label" [attr.for]="id('nombre')">Nombre</label>
            <input [id]="id('nombre')" class="input min-h-11" formControlName="nombre" autocomplete="given-name"
              [attr.aria-invalid]="invalido('nombre')" [attr.aria-describedby]="id('nombre-error')" />
            @if (invalido('nombre')) {
              <p [id]="id('nombre-error')" class="field-error">Escribe tu nombre.</p>
            }
          </div>
          <div>
            <label class="field-label" [attr.for]="id('apellido')">Apellido</label>
            <input [id]="id('apellido')" class="input min-h-11" formControlName="apellido" autocomplete="family-name"
              [attr.aria-invalid]="invalido('apellido')" [attr.aria-describedby]="id('apellido-error')" />
            @if (invalido('apellido')) {
              <p [id]="id('apellido-error')" class="field-error">Escribe tu apellido.</p>
            }
          </div>
        </div>
        <div>
          <label class="field-label" [attr.for]="id('nacimiento')">Fecha de nacimiento</label>
          <input [id]="id('nacimiento')" type="date" class="input min-h-11" formControlName="fechaNacimiento" autocomplete="bday"
            [max]="fechaMaxima" min="1900-01-01" [attr.aria-invalid]="invalido('fechaNacimiento')" [attr.aria-describedby]="id('nacimiento-error')" />
          @if (invalido('fechaNacimiento')) {
            <p [id]="id('nacimiento-error')" class="field-error">Debes ser mayor de edad para crear una cuenta.</p>
          }
        </div>
      }

      <div>
        <label class="field-label" [attr.for]="id('email')">Correo electrónico</label>
        <input [id]="id('email')" type="email" class="input min-h-11" formControlName="email" autocomplete="email" inputmode="email"
          [attr.aria-invalid]="invalido('email')" [attr.aria-describedby]="id('email-error')" />
        @if (invalido('email')) {
          <p [id]="id('email-error')" class="field-error">Escribe un correo válido.</p>
        }
      </div>

      <div>
        <label class="field-label" [attr.for]="id('password')">Contraseña</label>
        <app-campo-clave formControlName="password" [idCampo]="id('password')"
          [autocomplete]="registro() ? 'new-password' : 'current-password'" [invalido]="invalido('password')"
          [describedby]="id('password-error') + ' ' + id('password-fuerza')" />
        @if (invalido('password')) {
          <p [id]="id('password-error')" class="field-error">{{ registro() ? 'Usa al menos 8 caracteres.' : 'Escribe tu contraseña.' }}</p>
        }

        @if (registro()) {
          <div [id]="id('password-fuerza')" class="mt-3" aria-live="polite">
            <div class="flex gap-1.5" aria-hidden="true">
              @for (i of [1, 2, 3, 4]; track i) {
                <span class="h-1.5 flex-1 rounded-full transition-colors duration-300"
                  [class]="i > fuerza() ? 'bg-muted' : fuerza() <= 1 ? 'bg-danger' : fuerza() === 2 ? 'bg-warning' : 'bg-success'"></span>
              }
            </div>
            <p class="mt-2 text-xs text-muted-fg">Seguridad: <span class="font-medium text-fg">{{ etiquetaFuerza() }}</span></p>
            <ul class="mt-1.5 grid grid-cols-2 gap-1 text-xs">
              @for (r of requisitos(); track r.texto) {
                <li class="flex items-center gap-1.5" [class.text-success]="r.ok" [class.text-muted-fg]="!r.ok">
                  <svg lucideCircleCheck class="size-3.5" aria-hidden="true"></svg> {{ r.texto }}
                </li>
              }
            </ul>
          </div>
        }
      </div>
      }

      <div class="flex gap-3">
        @if (registro() && paso() === 2) {
          <button type="button" class="btn btn-secondary min-h-11" (click)="paso.set(1)">Atrás</button>
        }
        <button type="submit" class="btn btn-primary min-h-11 flex-1" [disabled]="enviando()">
          @if (enviando()) {
            <svg lucideLoader class="size-4 animate-spin" aria-hidden="true"></svg>
          }
          {{ !registro() ? 'Ingresar' : paso() === 1 ? 'Continuar' : 'Crear cuenta' }}
        </button>
      </div>
    </form>

    <!-- En pantallas grandes el cambio de modo lo hace el panel deslizante -->
    <p class="mt-5 text-sm text-muted-fg sm:hidden">
      @if (registro()) {
        ¿Ya tienes cuenta? <a routerLink="/login" class="font-medium text-accent hover:underline">Inicia sesión</a>
      } @else {
        ¿Aún no tienes cuenta? <a routerLink="/registro" class="font-medium text-accent hover:underline">Regístrate gratis</a>
      }
    </p>
  `,
})
export class FormularioAcceso implements OnInit {
  readonly modo = input.required<ModoAcceso>();
  /** Ruta a la que volver después de iniciar sesión (query param ?volver= del guard). */
  readonly volver = input<string | undefined>();
  /** Plan sugerido desde la landing (?plan=Profesional). */
  readonly planInicial = input<string | undefined>();

  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly registro = computed(() => this.modo() === 'registro');
  protected readonly enviando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly enviado = signal(false);
  /** Registro en dos pasos: datos personales y elección del plan. */
  protected readonly paso = signal<1 | 2>(1);
  protected readonly plan = signal<PlanSuscripcion>('Personal');
  protected readonly planes = PLANES;
  protected readonly avisoLanzamiento = AVISO_LANZAMIENTO;
  /** Fecha máxima de nacimiento: 18 años atrás. */
  protected readonly fechaMaxima = haceAnios(18);

  protected readonly form = this.fb.group({
    nombre: [''],
    apellido: [''],
    fechaNacimiento: [''],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  private readonly clave = toSignal(this.form.controls.password.valueChanges.pipe(map((v) => v ?? '')), { initialValue: '' });

  /** Requisitos visibles al registrarse; la fuerza es cuántos se cumplen (0–4). */
  protected readonly requisitos = computed(() => {
    const c = this.clave();
    return [
      { texto: '8 caracteres o más', ok: c.length >= 8 },
      { texto: 'Una mayúscula', ok: /[A-ZÁÉÍÓÚÑ]/.test(c) },
      { texto: 'Un número', ok: /\d/.test(c) },
      { texto: 'Un símbolo', ok: /[^A-Za-z0-9ÁÉÍÓÚÑáéíóúñ]/.test(c) },
    ];
  });
  protected readonly fuerza = computed(() => this.requisitos().filter((r) => r.ok).length);
  protected readonly etiquetaFuerza = computed(() => ['Muy débil', 'Débil', 'Aceptable', 'Buena', 'Fuerte'][this.fuerza()]);

  ngOnInit(): void {
    if (this.registro()) {
      this.form.controls.nombre.addValidators([Validators.required, Validators.minLength(2)]);
      this.form.controls.apellido.addValidators([Validators.required, Validators.minLength(2)]);
      this.form.controls.fechaNacimiento.addValidators([Validators.required, mayorDeEdad]);
      const sugerido = this.planInicial();
      if (esPlan(sugerido)) this.plan.set(sugerido);
      this.form.controls.password.addValidators(Validators.minLength(8));
      this.form.controls.nombre.updateValueAndValidity();
      this.form.controls.apellido.updateValueAndValidity();
      this.form.controls.fechaNacimiento.updateValueAndValidity();
      this.form.controls.password.updateValueAndValidity();
    }
  }

  /** Ids únicos: los dos formularios están en la página a la vez. */
  protected id(campo: string): string {
    return `${this.modo()}-${campo}`;
  }

  protected invalido(campo: 'nombre' | 'apellido' | 'fechaNacimiento' | 'email' | 'password'): boolean {
    const control = this.form.controls[campo];
    return control.invalid && (control.touched || this.enviado());
  }

  protected enviar(): void {
    this.enviado.set(true);
    this.error.set(null);
    if (this.form.invalid) return;
    if (this.registro() && this.paso() === 1) {
      this.paso.set(2);
      return;
    }

    const { nombre, apellido, fechaNacimiento, email, password } = this.form.getRawValue();
    const peticion = this.registro()
      ? this.auth.registrar({ nombre: nombre.trim(), apellido: apellido.trim(), fechaNacimiento, plan: this.plan(), email: email.trim(), password })
      : this.auth.login(email.trim(), password);

    this.enviando.set(true);
    peticion.subscribe({
      next: () => {
        const destino = this.volver()?.startsWith('/') ? this.volver()! : '/resumen';
        if (this.registro() && esDePago(this.plan())) {
          // Eligió un plan de pago: pasa a ingresar la tarjeta (empieza en Personal hasta pagar).
          void this.router.navigate(['/suscripcion'], { queryParams: { plan: this.plan() } });
          return;
        }
        void this.router.navigateByUrl(this.registro() ? '/importar' : destino);
      },
      error: (e) => {
        this.enviando.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }
}

/** 'yyyy-MM-dd' de hoy menos `anios` años (fecha local). */
function haceAnios(anios: number): string {
  const d = new Date();
  d.setFullYear(d.getFullYear() - anios);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function mayorDeEdad(control: AbstractControl<string>): ValidationErrors | null {
  const valor = control.value;
  return !valor || (valor <= haceAnios(18) && valor >= '1900-01-01') ? null : { mayorDeEdad: true };
}
