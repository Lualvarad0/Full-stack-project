import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../auth.service';
import { Router } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  loginForm: FormGroup;
  loading: boolean = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
    });
  }

  onLogin() {
    if (this.loginForm.valid) {
      const { email, password } = this.loginForm.value;

      // Simulación de autenticación
      if (email === 'admin@gmail.com' && password === 'admin') {
        // Redirigir al dashboard si las credenciales son correctas
        this.router.navigate(['/dashboard']);
      } else {
        // Manejo de error si las credenciales son incorrectas
        console.error('Credenciales incorrectas');
        alert('Usuario o contraseña incorrectos');
      }
    } else {
      console.error('Formulario no válido');
      alert('Por favor complete el formulario correctamente');
    }

    this.loading = true;
    setTimeout(() => {
      this.loading = false;
    }, 2000);
  }

}
