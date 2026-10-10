import { ChangeDetectionStrategy, Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LucideLoader } from '@lucide/angular';
import { Marca } from '../../shared/marca';
import { AuthService } from '../../core/auth';
import { mensajeDeError } from '../../core/estado';

/** Pantalla de inicio de sesión y registro (misma tarjeta, cambia según la ruta). */
@Component({
  selector: 'app-acceso',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, LucideLoader, Marca],
  templateUrl: './acceso.html',
})
export class Acceso implements OnInit {
  /** Viene de la data de la ruta. */
  readonly modo = input<'login' | 'registro'>('login');
  /** Query param ?volver= que deja el guard. */
  readonly volver = input<string | undefined>();

  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly enviando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly enviado = signal(false);

  protected readonly form = this.fb.group({
    nombre: [''],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  ngOnInit(): void {
    if (this.modo() === 'registro') {
      this.form.controls.nombre.addValidators([Validators.required, Validators.minLength(2)]);
      this.form.controls.password.addValidators(Validators.minLength(8));
      this.form.controls.nombre.updateValueAndValidity();
      this.form.controls.password.updateValueAndValidity();
    }
  }

  protected invalido(campo: 'nombre' | 'email' | 'password'): boolean {
    const control = this.form.controls[campo];
    return control.invalid && (control.touched || this.enviado());
  }

  protected enviar(): void {
    this.enviado.set(true);
    this.error.set(null);
    if (this.form.invalid) return;

    const { nombre, email, password } = this.form.getRawValue();
    const peticion = this.modo() === 'registro'
      ? this.auth.registrar(nombre.trim(), email.trim(), password)
      : this.auth.login(email.trim(), password);

    this.enviando.set(true);
    peticion.subscribe({
      next: () => {
        const destino = this.volver()?.startsWith('/') ? this.volver()! : '/resumen';
        void this.router.navigateByUrl(this.modo() === 'registro' ? '/importar' : destino);
      },
      error: (e) => {
        this.enviando.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }
}
