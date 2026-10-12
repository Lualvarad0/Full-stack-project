import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideCircleCheck,
  LucideCircleDashed,
  LucideCircleMinus,
  LucideCircleX,
} from '@lucide/angular';
import { ESTADOS_IMPORTACION, ImportacionResponse } from '@core/comprobantes';
@Component({
  selector: 'app-resultado-importacion',
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideCircleCheck, LucideCircleDashed, LucideCircleMinus, LucideCircleX],
  templateUrl: './resultado-importacion.html',
})
export class ResultadoImportacion {
  readonly resultado = input<ImportacionResponse | null>(null);
  protected readonly estados = ESTADOS_IMPORTACION;
  protected readonly problemas = computed(() =>
    (this.resultado()?.claves ?? []).filter(
      (c) => c.resultado !== 'Importado' && c.resultado !== 'YaExistia',
    ),
  );
}
