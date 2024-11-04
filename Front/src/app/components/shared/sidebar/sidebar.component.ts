import { Component } from '@angular/core';
import { Router } from '@angular/router'; // Importa Router para la navegación

type Section = 'clientes' | 'productos' | 'facturas' | 'usuarios';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css']
})
export class SidebarComponent {
  sections: Record<Section, boolean> = {
    clientes: false,
    productos: false,
    facturas: false,
    usuarios: false // Solo para administradores
  };

  // Cambia esto según el rol del usuario
  userRole: 'admin' | 'user' = 'admin'; // Cambiado a 'admin'

  constructor(private router: Router) {} // Inyección del Router

  toggleSection(section: Section) {
    // Verifica si la sección es 'usuarios' y el rol es 'user', en cuyo caso no permite abrirla
    if (section === 'usuarios' && this.userRole !== 'admin') {
      console.warn('Acceso denegado: solo los administradores pueden acceder a esta sección.');
      return;
    }

    // Alterna el estado de la sección específica
    this.sections[section] = !this.sections[section];

    // Asegúrate de cerrar otras secciones al abrir una nueva, si lo deseas
    Object.keys(this.sections).forEach(key => {
      if (key !== section) {
        this.sections[key as Section] = false; // Asegúrate de hacer un casting aquí
      }
    });
  }

  logout() {
    // Implementa la lógica de cierre de sesión aquí
    console.log('Cerrando sesión...');

    // Elimina el token de autenticación y cualquier otro dato del usuario
    localStorage.removeItem('authToken'); // Si usas localStorage para almacenar el token
    sessionStorage.removeItem('authToken'); // Si usas sessionStorage

    // Redirige al usuario a la página de inicio o inicio de sesión
    this.router.navigate(['/login']); // Cambia '/login' por la ruta de tu página de inicio o login
  }
}
