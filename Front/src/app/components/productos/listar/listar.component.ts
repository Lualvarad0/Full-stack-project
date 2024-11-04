import { Component, Input } from '@angular/core';

interface Producto {
  id: number;
  nombre: string;
  descripcion: string;
  precio: number;
}

@Component({
  selector: 'app-listar-productos',
  templateUrl: './listar.component.html',
  styleUrls: ['./listar.component.css']
})
export class ListarProductosComponent {
  @Input() productos: Producto[] = [];
  filtro: string = '';

  get productosFiltrados() {
    return this.productos.filter(producto =>
      producto.nombre.toLowerCase().includes(this.filtro.toLowerCase()) ||
      producto.descripcion.toLowerCase().includes(this.filtro.toLowerCase())
    );
  }
}
