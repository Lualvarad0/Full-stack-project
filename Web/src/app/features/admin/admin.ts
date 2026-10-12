import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { Segmentado, Titulo } from '@shared/components';
import { AdminResumen } from './resumen';
import { AdminUsuarios } from './usuarios';
import { AdminPagos } from './pagos';
import { AdminSistema } from './sistema';
@Component({
  selector: 'app-admin',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Titulo, Segmentado, AdminResumen, AdminUsuarios, AdminPagos, AdminSistema],
  templateUrl: './admin.html',
})
export class Admin {
  protected readonly pestana = signal('resumen');
  protected readonly pestanas = [
    { valor: 'resumen', etiqueta: 'Resumen' },
    { valor: 'usuarios', etiqueta: 'Usuarios' },
    { valor: 'pagos', etiqueta: 'Pagos' },
    { valor: 'sistema', etiqueta: 'Sistema' },
  ];
}
