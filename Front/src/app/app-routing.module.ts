import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

// Importación de los componentes
import { LoginComponent } from './auth/login/login.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { CrearFacturaComponent } from './components/facturas/crear/crear.component';
import { ListarFacturasComponent } from './components/facturas/listar/listar.component';
import { CrearClienteComponent } from './components/clientes/crear/crear.component';
import { ListarClientesComponent } from './components/clientes/listar/listar.component';
import { CrearProductoComponent } from './components/productos/crear/crear.component';
import { ListarProductosComponent } from './components/productos/listar/listar.component';
import { CrearUsuarioComponent } from './components/usuarios/crear/crear.component';
import { ListarUsuariosComponent } from './components/usuarios/listar/listar.component';
import { NotFoundComponent } from './components/not-found/not-found.component'; // Asegúrate de que la ruta sea correcta

// Guardia de autenticación
import { authGuard } from './guards/auth.guard';
import { NavigationGuard } from './guards/navigation.guard'; // Importar el nuevo guardia
import { LayoutComponent } from './components/layout/layout.component';

const routes: Routes = [
  { path: 'login', component: LoginComponent },

  // Rutas protegidas que usarán el LayoutComponent
  {
    path: '',
    component: LayoutComponent,
    canActivate: [NavigationGuard], // Proteger todas las rutas bajo el layout
    children: [
      { path: 'dashboard', component: DashboardComponent },
      { path: 'facturas/crear', component: CrearFacturaComponent },
      { path: 'facturas/listar', component: ListarFacturasComponent },
      { path: 'clientes/crear', component: CrearClienteComponent },
      { path: 'clientes/listar', component: ListarClientesComponent },
      { path: 'productos/crear', component: CrearProductoComponent },
      { path: 'productos/listar', component: ListarProductosComponent },
      { path: 'usuarios/crear', component: CrearUsuarioComponent },
      { path: 'usuarios/listar', component: ListarUsuariosComponent },
      { path: '', redirectTo: '/404', pathMatch: 'full' }, // Redirige a 404 si la ruta está vacía
      ],
  },

  // Página 404
  { path: '404', component: NotFoundComponent },

  // Redirección a 404 para rutas desconocidas
  { path: '**', redirectTo: '/404' } // Redirigir cualquier ruta desconocida a 404
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
