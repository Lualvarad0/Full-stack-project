import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideSparkles } from '@lucide/angular';
/** Módulos del roadmap: explica qué hará cada uno. Título y puntos vienen de la data de la ruta. */
@Component({
  selector: 'app-proximamente',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideSparkles],
  templateUrl: './proximamente.html',
})
export class Proximamente {
  readonly titulo = input('');
  readonly descripcion = input('');
  readonly puntos = input<string[]>([]);
}
