import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { BarrasComparadas, BarrasHorizontales, BarrasMes, Segmentado } from '@shared/components';
import { ResumenEstado } from './resumen-estado';

@Component({
  selector: 'app-grafico-resumen',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BarrasComparadas, BarrasHorizontales, BarrasMes, Segmentado],
  templateUrl: './grafico-resumen.html',
})
export class GraficoResumen {
  protected readonly estado = inject(ResumenEstado);
  protected readonly vista = signal<'evolucion' | 'categorias'>('evolucion');
  protected readonly opciones = [
    { valor: 'evolucion', etiqueta: 'Por mes' },
    { valor: 'categorias', etiqueta: 'Por categoría' },
  ] as const;
}
