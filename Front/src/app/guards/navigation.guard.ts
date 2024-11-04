import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class NavigationGuard implements CanActivate {

  // Define las rutas que se permiten
  private allowedRoutes: string[] = ['/dashboard', '/facturas/crear', '/facturas/listar',
                                     '/clientes/crear', '/clientes/listar',
                                     '/productos/crear', '/productos/listar',
                                     '/usuarios/crear', '/usuarios/listar'];

  constructor(private router: Router) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): boolean {
    const currentRoute = state.url; // Obtiene la ruta actual

    // Verifica si la ruta actual es una de las permitidas
    if (this.allowedRoutes.includes(currentRoute)) {
      return true; // Permite la navegación a rutas permitidas
    }

    // Si la ruta no está permitida, redirige a la página 404
    this.router.navigate(['/404']); // Redirige a la página 404
    return false;
  }
}
