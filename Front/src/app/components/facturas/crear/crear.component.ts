import { Component } from '@angular/core';

@Component({
  selector: 'app-crear-factura',
  templateUrl: './crear.component.html',
  styleUrls: ['./crear.component.css']
})
export class CrearFacturaComponent {
  // Lista de clientes simulada
  clientes = [
    { id: '1', nombre: 'Cliente A', telefono: '123456789', correo: 'clientea@example.com', cedula: '001' },
    { id: '2', nombre: 'Cliente B', telefono: '987654321', correo: 'clienteb@example.com', cedula: '002' },
    // Agrega más clientes según sea necesario
  ];

  // Variables para el formulario
  clienteId: string = '';
  nombreCliente: string = '';
  telefono: string = '';
  correo: string = '';
  vendedor: string = '';
  fecha: string = '';
  formaPago: string = '';
  detalles: Array<{ producto: string; cantidad: number; precioUnitario: number; precioTotal: number }> = [{
    producto: '',
    cantidad: 0,
    precioUnitario: 0,
    precioTotal: 0
  }];

  // Variables para la búsqueda
  busqueda: string = '';
  clientesFiltrados: Array<any> = [];

  // Actualizar la lista de clientes filtrados basado en la búsqueda
  actualizarBusqueda() {
    if (this.busqueda) {
      const busquedaLower = this.busqueda.toLowerCase();
      this.clientesFiltrados = this.clientes.filter(cliente =>
        cliente.nombre.toLowerCase().includes(busquedaLower) ||
        cliente.cedula.includes(busquedaLower)
      );
    } else {
      this.clientesFiltrados = [];
    }
  }

  // Seleccionar cliente
  seleccionarCliente(cliente: any) {
    this.clienteId = cliente.cedula; // o cliente.id según prefieras
    this.nombreCliente = cliente.nombre;
    this.telefono = cliente.telefono;
    this.correo = cliente.correo;
    this.busqueda = ''; // Limpiar la búsqueda
    this.clientesFiltrados = []; // Limpiar resultados
  }

  // Función para agregar un nuevo detalle de producto a la factura
  agregarDetalle() {
    this.detalles.push({ producto: '', cantidad: 0, precioUnitario: 0, precioTotal: 0 });
  }

  // Función para crear la factura
  crearFactura() {
    // Aquí implementa la lógica para crear la factura (guardar o enviar los datos a un backend, etc.)
    console.log({
      clienteId: this.clienteId,
      nombreCliente: this.nombreCliente,
      telefono: this.telefono,
      correo: this.correo,
      vendedor: this.vendedor,
      fecha: this.fecha,
      formaPago: this.formaPago,
      detalles: this.detalles
    });
  }

  // Calcula el precio total de un detalle (producto)
  calcularPrecioTotal(detalle: { cantidad: number; precioUnitario: number }): number {
    return detalle.cantidad * detalle.precioUnitario;
  }
}
