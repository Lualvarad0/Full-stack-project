import { haceAnios, mayorDeEdad } from './validators/registro.validator';
import { claveCuenta } from './validators/clave.validator';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  OnInit,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { LucideCircleCheck, LucideLoader } from '@lucide/angular';
import { AVISO_LANZAMIENTO, esDePago, esPlan, PLANES, PlanSuscripcion } from '@core/suscripcion';
import { ModoAcceso, AuthService } from '@core/auth';
import { mensajeDeError } from '@core/http';
import { CampoClave, MensajeError } from '@shared/components';
/** Formulario de inicio de sesión o de registro; la pantalla de acceso muestra uno de cada lado. */
@Component({
  selector: 'app-formulario-acceso',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CampoClave,
    MensajeError,
    ReactiveFormsModule,
    RouterLink,
    LucideLoader,
    LucideCircleCheck,
  ],
  templateUrl: './formulario-acceso.html',
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
    email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
    password: ['', [Validators.required, Validators.maxLength(100)]],
  });

  private readonly clave = toSignal(
    this.form.controls.password.valueChanges.pipe(map((v) => v ?? '')),
    { initialValue: '' },
  );

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
  protected readonly etiquetaFuerza = computed(
    () => ['Muy débil', 'Débil', 'Aceptable', 'Buena', 'Fuerte'][this.fuerza()],
  );

  ngOnInit(): void {
    if (this.registro()) {
      this.form.controls.nombre.addValidators([
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(100),
      ]);
      this.form.controls.apellido.addValidators([
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(100),
      ]);
      this.form.controls.fechaNacimiento.addValidators([Validators.required, mayorDeEdad]);
      const sugerido = this.planInicial();
      if (esPlan(sugerido)) this.plan.set(sugerido);
      this.form.controls.password.addValidators(claveCuenta);
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

  protected invalido(
    campo: 'nombre' | 'apellido' | 'fechaNacimiento' | 'email' | 'password',
  ): boolean {
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
      ? this.auth.registrar({
          nombre: nombre.trim(),
          apellido: apellido.trim(),
          fechaNacimiento,
          plan: this.plan(),
          email: email.trim(),
          password,
        })
      : this.auth.login(email.trim(), password);

    this.enviando.set(true);
    peticion.subscribe({
      next: () => {
        const destino = this.volver()?.startsWith('/') ? this.volver()! : this.auth.rutaInicio();
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
