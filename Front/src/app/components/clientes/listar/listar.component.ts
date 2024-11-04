import { Component, OnInit } from '@angular/core';
import { ClienteService } from '../../../Services/cliente.service';

@Component({
  selector: 'app-listar-clientes',
  templateUrl: './listar.component.html',
  styleUrls: ['./listar.component.css']
})
export class ListarClientesComponent implements OnInit {
  clientes: any[] = [];
  filtros = {
    nombre: '',
    correo: '',
    telefono: '',
    identificacion: ''
  };

  constructor(private clienteService: ClienteService) {}

  ngOnInit(): void {
    this.cargarClientes();
  }

  cargarClientes(): void {
    this.clienteService.obtenerClientes(this.filtros).subscribe(
      (data) => {
        this.clientes = data; // Carga los clientes desde el backend
      },
      (error) => {
        console.error('Error al cargar clientes:', error);
      }
    );
  }

  buscarClientes(): void {
    // Recargar los clientes aplicando filtros
    this.cargarClientes();
  }

  limpiarFiltros(): void {
    // Limpiar filtros y recargar la lista completa
    this.filtros = {
      nombre: '',
      correo: '',
      telefono: '',
      identificacion: ''
    };
    this.cargarClientes();
  }
}
