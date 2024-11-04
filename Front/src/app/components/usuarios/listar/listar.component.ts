import { Component } from '@angular/core';

interface Usuario {
  nombre: string;
  email: string;
  contrasena: string;
  telefono: string;
  direccion: string;
  fechaNacimiento: Date;
  genero: string;
  edad: number;
  tipo: string;
  area: string;
  salario: number;
  activo: boolean;
}

@Component({
  selector: 'app-listar',
  templateUrl: './listar.component.html',
  styleUrls: ['./listar.component.css'],
})
export class ListarUsuariosComponent {
  usuarios: Usuario[] = []; // Aquí almacenarás los usuarios
  usuariosFiltrados: Usuario[] = []; // Usuarios que se mostrarán en la lista
  filtro: string = ''; // Filtro de búsqueda

  constructor() {
    // Inicializa con algunos usuarios de ejemplo
    this.usuarios = [
      {
        nombre: 'Juan Pérez',
        email: 'juan@example.com',
        contrasena: 'password',
        telefono: '123456789',
        direccion: 'Calle 123',
        fechaNacimiento: new Date('1990-01-01'),
        genero: 'masculino',
        edad: 34,
        tipo: 'usuario',
        area: 'Contabilidad',
        salario: 2000,
        activo: true,
      },
      {
        nombre: 'María Gómez',
        email: 'maria@example.com',
        contrasena: 'password',
        telefono: '987654321',
        direccion: 'Calle 456',
        fechaNacimiento: new Date('1985-05-15'),
        genero: 'femenino',
        edad: 39,
        tipo: 'administrador',
        area: 'Finanzas',
        salario: 3000,
        activo: false,
      },
      // Agrega más usuarios según sea necesario
    ];
    this.usuariosFiltrados = this.usuarios; // Inicializa la lista filtrada
  }

  buscarUsuarios() {
    this.usuariosFiltrados = this.usuarios.filter((usuario) => {
      return (
        usuario.nombre.toLowerCase().includes(this.filtro.toLowerCase()) ||
        usuario.email.toLowerCase().includes(this.filtro.toLowerCase()) ||
        usuario.telefono.includes(this.filtro) ||
        usuario.direccion.toLowerCase().includes(this.filtro.toLowerCase()) ||
        usuario.area.toLowerCase().includes(this.filtro.toLowerCase())
      );
    });
  }
}
