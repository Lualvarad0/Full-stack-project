import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-listar-facturas',
  templateUrl: './listar.component.html',
  styleUrls: ['./listar.component.css']
})
export class ListarFacturasComponent implements OnInit {
  facturas: any[] = []; // Suponiendo que esto contiene tus facturas
  filteredFacturas: any[] = [];
  filters: { criterion: string, value: string }[] = [];

  // Propiedades para manejar los filtros
  filterValue: string = ''; // Valor del filtro ingresado
  selectedFilter: string = ''; // Filtro seleccionado del dropdown

  // Tipos de filtros disponibles
  filterOptions = [
    { label: 'ID', value: 'id' },
    { label: 'Nombre Cliente', value: 'nombreCliente' },
    { label: 'Monto', value: 'monto' },
    { label: 'Fecha', value: 'fecha' }
  ];

  constructor() {}

  ngOnInit() {
    this.loadFacturas(); // Carga inicial de facturas
  }

  loadFacturas() {
    // Aquí deberías cargar tus facturas desde un servicio
    this.facturas = [
      { id: 1, nombreCliente: 'Cliente A', monto: 100, fecha: '2023-01-01' },
      { id: 2, nombreCliente: 'Cliente B', monto: 200, fecha: '2023-01-02' },
      // Agrega más facturas según sea necesario
    ];
    this.filteredFacturas = this.facturas; // Inicialmente, todas las facturas están filtradas
  }

  filterFacturas() {
    this.filteredFacturas = this.facturas.filter(factura => {
      return this.filters.every(filter => {
        const facturaValue = factura[filter.criterion];
        return facturaValue.toString().toLowerCase().includes(filter.value.toLowerCase());
      });
    });
  }

  updateFilterValue() {
    // Reinicia filterValue al valor correspondiente del filtro seleccionado
    const existingFilter = this.filters.find(filter => filter.criterion === this.selectedFilter);
    this.filterValue = existingFilter ? existingFilter.value : '';
  }


  addFilter(criterion: string, value: string) {
    if (value) {
      this.filters.push({ criterion, value });
      this.filterFacturas(); // Actualiza la lista filtrada
      this.filterValue = ''; // Limpia el valor del filtro después de agregarlo
    }
  }

  removeFilter(filterToRemove: { criterion: string, value: string }) {
    this.filters = this.filters.filter(filter => filter !== filterToRemove);
    this.filterFacturas(); // Actualiza la lista filtrada
  }

  clearFilters() {
    this.filters = [];
    this.filteredFacturas = this.facturas; // Reinicia los filtros
    this.filterValue = ''; // Limpia el valor del filtro
  }
}

