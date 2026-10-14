import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideCheck, LucideSparkles } from '@lucide/angular';
import { AVISO_LANZAMIENTO, Periodicidad, Plan, PLANES } from '@core/suscripcion';
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
  protected readonly opciones: readonly Periodicidad[] = ['Mensual', 'Anual'];
  protected readonly periodicidad = signal<Periodicidad>('Mensual');

  protected precio(p: Plan): string {
    return this.periodicidad() === 'Anual' ? p.precioAnual : p.precio;
  }

  protected periodo(p: Plan): string {
    if (!p.periodo) return '';
    return this.periodicidad() === 'Anual' ? '/año + IVA' : p.periodo;
  }

  /** El registro y la página de pago recuerdan el plan y la periodicidad elegidos aquí. */
  protected parametros(p: Plan): Record<string, string> {
    return p.periodo ? { plan: p.id, periodicidad: this.periodicidad() } : { plan: p.id };
  }
}
