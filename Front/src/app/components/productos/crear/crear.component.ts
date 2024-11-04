import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ProductoService } from '../../../Services/producto.service';
import { Producto } from '../../../models/producto.models';

@Component({
  selector: 'app-crear-producto',
  templateUrl: './crear.component.html',
  styleUrls:['./crear.component.css']
})
export class CrearProductoComponent {
  productoForm: FormGroup;

  constructor(private fb: FormBuilder, private productoService: ProductoService) {
    this.productoForm = this.fb.group({
      nombre: ['', Validators.required],
      descripcion: ['', Validators.required],
      precio: [0, [Validators.required, Validators.min(0)]],
      stock: [0, [Validators.required, Validators.min(0)]],
    });
  }

  agregarProducto() {
    if (this.productoForm.valid) {
      const nuevoProducto: Producto = this.productoForm.value;
      this.productoService.crearProducto(nuevoProducto).subscribe(response => {
        console.log('Producto agregado:', response);
        this.productoForm.reset(); // Limpiar el formulario
      });
    } else {
      // Manejar el caso de formulario inválido
      console.log('Formulario inválido');
    }
  }
}
