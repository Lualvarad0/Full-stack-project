import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideCheck, LucideSparkles } from '@lucide/angular';
import { AVISO_LANZAMIENTO, PLANES } from '@core/suscripcion';
@Component({
  selector: 'app-planes-landing',
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideCheck, LucideSparkles],
  templateUrl: './planes.html',
})
export class PlanesLanding {
  protected readonly planes = PLANES;
  protected readonly avisoLanzamiento = AVISO_LANZAMIENTO;
}
