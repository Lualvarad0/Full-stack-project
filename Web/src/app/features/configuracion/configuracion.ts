import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucidePalette, LucideSettings, LucideUserRound } from '@lucide/angular';
import { AuthService } from '@core/auth';
import { CarteraService } from '@core/contabilidad/services/cartera.service';
import {
  ActividadCuenta,
  BuzonTarjeta,
  DatosEmisorTarjeta,
  FirmaTarjeta,
  PlanTarjeta,
  Titulo,
} from '@shared/components';
import { ConfiguracionSistemaTarjeta } from './configuracion-sistema';
import { DatosPersonalesTarjeta } from './datos-personales';
import { PerfilFormulario } from './perfil-formulario';
import { PreferenciasDispositivo } from './preferencias-dispositivo';
import { SeguridadCuentaTarjeta } from './seguridad-cuenta';

type Seccion = 'perfil' | 'sistema' | 'preferencias';

const SECCIONES: readonly { id: Seccion; nombre: string; descripcion: string }[] = [
  {
    id: 'perfil',
    nombre: 'Mi perfil',
    descripcion: 'Tus datos personales y los de tu RUC (cédula, régimen y cargas familiares).',
  },
  {
    id: 'sistema',
    nombre: 'Sistema',
    descripcion: 'Cómo funciona tu cuenta: inicio, avisos, plan, seguridad, facturación electrónica y actividad.',
  },
  {
    id: 'preferencias',
    nombre: 'Preferencias',
    descripcion: 'Cómo se ve Nudo en este dispositivo.',
  },
];

/** Configuración en tres secciones (?seccion=perfil|sistema|preferencias). */
@Component({
  selector: 'app-configuracion',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    Titulo,
    DatosPersonalesTarjeta,
    PerfilFormulario,
    ConfiguracionSistemaTarjeta,
    SeguridadCuentaTarjeta,
    PlanTarjeta,
    FirmaTarjeta,
    DatosEmisorTarjeta,
    BuzonTarjeta,
    ActividadCuenta,
    PreferenciasDispositivo,
    LucideUserRound,
    LucideSettings,
    LucidePalette,
  ],
  templateUrl: './configuracion.html',
})
export class Configuracion {
  protected readonly cartera = inject(CarteraService);
  protected readonly auth = inject(AuthService);

  /** Query param ?seccion= (enlaces de otras páginas, p. ej. "Ir a Configuración" de facturación). */
  readonly seccion = input<string | undefined>();
  protected readonly secciones = SECCIONES;
  protected readonly actual = computed<Seccion>(
    () => SECCIONES.find((s) => s.id === this.seccion())?.id ?? 'perfil',
  );
  protected readonly descripcion = computed(
    () => SECCIONES.find((s) => s.id === this.actual())!.descripcion,
  );
}
