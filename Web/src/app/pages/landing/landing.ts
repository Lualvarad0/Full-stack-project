import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideArrowRight,
  LucideBell,
  LucideChevronDown,
  LucideChevronRight,
  LucideCircleCheck,
  LucideEllipsisVertical,
  LucidePlus,
  LucideSearch,
  LucideSparkles,
} from '@lucide/angular';
import { Marca } from '../../shared/marca';

/** Landing pública: navbar + hero con vista previa del panel (todo en código, sin imágenes). */
@Component({
  selector: 'app-landing',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink, Marca, LucideSparkles, LucideArrowRight, LucideSearch, LucideBell, LucideChevronDown,
    LucideChevronRight, LucideCircleCheck, LucidePlus, LucideEllipsisVertical,
  ],
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing {
  // Datos ilustrativos de la vista previa.
  protected readonly menu = [
    { nombre: 'Resumen', activo: true },
    { nombre: 'Gastos', insignia: '3' },
    { nombre: 'Ingresos' },
    { nombre: 'Clientes', flecha: true },
    { nombre: 'Proveedores' },
    { nombre: 'Importar del SRI' },
    { nombre: 'Configuración', flecha: true },
  ];
  protected readonly impuestos = ['Declarar IVA', 'Tope deducible', 'Retenciones', 'Recordatorios'];
  protected readonly acciones = ['Nueva factura', 'Declarar IVA', 'Clientes', 'Proveedores', 'Reportes'];
  protected readonly categorias = [
    { nombre: 'Vivienda', monto: '$2.700,00' },
    { nombre: 'Salud', monto: '$1.350,72' },
    { nombre: 'Alimentación', monto: '$980,40' },
  ];
  protected readonly movimientos = [
    { fecha: '09 oct', descripcion: 'Constructora del Pacífico', monto: '+$977,50', estado: 'Verificado', ok: true, ingreso: true },
    { fecha: '08 oct', descripcion: 'De Prati', monto: '−$91,89', estado: 'Por categorizar', ok: false },
    { fecha: '05 oct', descripcion: 'Inmobiliaria Los Ceibos', monto: '−$450,00', estado: 'Verificado', ok: true },
    { fecha: '03 oct', descripcion: 'Fybeca', monto: '−$43,92', estado: 'Verificado', ok: true },
  ];
}
