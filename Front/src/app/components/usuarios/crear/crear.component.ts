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
  numeroCuenta: string;
  tipoCuenta: string;
  salario: number;
  activo: boolean; // Cambiado a booleano
}

@Component({
  selector: 'app-crear',
  templateUrl: './crear.component.html',
  styleUrls: ['./crear.component.css'],
})
export class CrearUsuarioComponent {
  nuevoUsuario: Usuario = {
    nombre: '',
    email: '',
    contrasena: '',
    telefono: '',
    direccion: '',
    fechaNacimiento: new Date(),
    genero: '',
    edad: 0,
    tipo: 'usuario',
    area: '',
    numeroCuenta: '',
    tipoCuenta: 'ahorros',
    salario: 0,
    activo: true, // Inicializado como verdadero
  };

  agregarUsuario() {
    // Lógica para agregar usuario
    console.log('Usuario agregado:', this.nuevoUsuario);
    this.resetFormulario();
  }

  resetFormulario() {
    this.nuevoUsuario = {
      nombre: '',
      email: '',
      contrasena: '',
      telefono: '',
      direccion: '',
      fechaNacimiento: new Date(),
      genero: '',
      edad: 0,
      tipo: 'usuario',
      area: '',
      numeroCuenta: '',
      tipoCuenta: 'ahorros',
      salario: 0,
      activo: true,
    };
  }
}
