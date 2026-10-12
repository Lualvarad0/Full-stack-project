import { ResumenEstado } from './resumen-estado';
import { GraficoResumen } from './grafico-resumen';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DecimalPipe, SlicePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  LucideArrowRight,
  LucideCircleCheck,
  LucideFileUp,
  LucideIdCard,
  LucideInfo,
  LucideMail,
  LucideReceipt,
  LucideScale,
  LucideTrendingDown,
  LucideTrendingUp,
  LucideTriangleAlert,
} from '@lucide/angular';
import {
  BienvenidaResumen,
  BarrasHorizontales,
  CategoriaBadge,
  ErrorCarga,
  Monto,
  Titulo,
} from '@shared/components';
import { DineroPipe, FechaCortaPipe } from '@shared/pipes';
@Component({
  providers: [ResumenEstado],
  selector: 'app-resumen',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    GraficoResumen,
    ErrorCarga,
    Titulo,
    Monto,
    CategoriaBadge,
    RouterLink,
    DecimalPipe,
    SlicePipe,
    DineroPipe,
    FechaCortaPipe,
    BarrasHorizontales,

    BienvenidaResumen,
    LucideFileUp,
    LucideTriangleAlert,
    LucideCircleCheck,
    LucideInfo,
    LucideIdCard,
    LucideTrendingUp,
    LucideTrendingDown,
    LucideScale,
    LucideReceipt,
    LucideMail,
    LucideArrowRight,
  ],
  templateUrl: './resumen.html',
})
export class Resumen {
  protected readonly estado = inject(ResumenEstado);
}
