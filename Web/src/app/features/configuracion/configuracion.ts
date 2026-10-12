import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { PreferenciasService } from '@core/ui';
import {
  ActividadCuenta,
  BuzonTarjeta,
  DatosEmisorTarjeta,
  FirmaTarjeta,
  PlanTarjeta,
  Segmentado,
  Titulo,
} from '@shared/components';
import { PerfilFormulario } from './perfil-formulario';
import { SeguridadCuentaTarjeta } from './seguridad-cuenta';
import { CarteraService } from '@core/contabilidad/services/cartera.service';
@Component({
  selector: 'app-configuracion',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Titulo,
    PerfilFormulario,
    SeguridadCuentaTarjeta,
    PlanTarjeta,
    FirmaTarjeta,
    DatosEmisorTarjeta,
    BuzonTarjeta,
    ActividadCuenta,
    Segmentado,
  ],
  templateUrl: './configuracion.html',
})
export class Configuracion {
  protected readonly cartera = inject(CarteraService);
  protected readonly preferencias = inject(PreferenciasService);
  protected readonly temas = [
    { valor: 'light', etiqueta: 'Claro' },
    { valor: 'dark', etiqueta: 'Oscuro' },
  ] as const;
}
