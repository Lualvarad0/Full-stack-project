import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

// Componentes de cada sección
import { CrearFacturaComponent } from './facturas/crear/crear.component';
import { ListarFacturasComponent } from './facturas/listar/listar.component';
import { CrearClienteComponent } from './clientes/crear/crear.component';
import { ListarClientesComponent } from './clientes/listar/listar.component';
import { CrearProductoComponent } from './productos/crear/crear.component';
import { ListarProductosComponent } from './productos/listar/listar.component';
import { CrearUsuarioComponent } from './usuarios/crear/crear.component';
import { ListarUsuariosComponent } from './usuarios/listar/listar.component';

// Componentes compartidos
import { SidebarComponent } from './shared/sidebar/sidebar.component';
import { FooterComponent } from './shared/footer/footer.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { LayoutComponent } from './layout/layout.component';
import { NotFoundComponent } from './not-found/not-found.component';

@NgModule({
  declarations: [
    // Facturas
    CrearFacturaComponent,
    ListarFacturasComponent,
    // Clientes
    CrearClienteComponent,
    ListarClientesComponent,
    // Productos
    CrearProductoComponent,
    ListarProductosComponent,
    // Usuarios
    CrearUsuarioComponent,
    ListarUsuariosComponent,
    // Componentes compartidos
    SidebarComponent,
    FooterComponent,
    DashboardComponent,
    LayoutComponent,
    NotFoundComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule
  ],
  exports: [
    // Exporta los componentes que se necesitan en otros módulos
    CrearFacturaComponent,
    ListarFacturasComponent,
    CrearClienteComponent,
    ListarClientesComponent,
    CrearProductoComponent,
    ListarProductosComponent,
    CrearUsuarioComponent,
    ListarUsuariosComponent,
    SidebarComponent,
    FooterComponent,
    LayoutComponent
  ]
})
export class ComponentsModule { }
