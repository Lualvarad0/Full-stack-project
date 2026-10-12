import { PlanesLanding } from './planes';
import { PreguntasLanding } from './preguntas';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideArrowRight,
  LucideCalculator,
  LucideCalendarCheck,
  LucideChartPie,
  LucideHandCoins,
  LucideLock,
  LucideMail,
  LucideScale,
  LucideShieldCheck,
  LucideSlidersHorizontal,
  LucideSparkles,
} from '@lucide/angular';
import { CANASTA_ENERO, calcularTope } from '@core/perfil';
import { NOMBRE_APP } from '@core/ui';
import { IconoFuncion } from '@features/landing/models/landing.model';
import { Credito, Marca } from '@shared/components';
import { DineroPipe } from '@shared/pipes';
import { DemoPanel } from './demo-panel';
import { FlujoFacturas } from './flujo-facturas';
/** Landing pública: hero con vista previa del panel + secciones de producto (todo en código, sin imágenes). */
@Component({
  selector: 'app-landing',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PlanesLanding,
    PreguntasLanding,
    RouterLink,
    Marca,
    Credito,
    DemoPanel,
    FlujoFacturas,
    DineroPipe,
    LucideSparkles,
    LucideArrowRight,
    LucideMail,
    LucideHandCoins,
    LucideChartPie,
    LucideScale,
    LucideCalculator,
    LucideCalendarCheck,
    LucideSlidersHorizontal,
    LucideShieldCheck,
    LucideLock,
  ],
  templateUrl: './landing.html',
})
export class Landing {
  protected readonly nombre = NOMBRE_APP;
  protected readonly anio = new Date().getFullYear();

  /** Tope real del año en curso (canasta de enero del INEC), si ya está registrado. */
  protected readonly tope = calcularTope(
    Math.max(...Object.keys(CANASTA_ENERO).map(Number)),
    0,
    false,
  );

  protected readonly pasos = [
    {
      titulo: 'Activa tu buzón',
      texto:
        'Te damos una dirección propia. Reenvía ahí tus facturas con un filtro de Gmail o dala al comprar. Sin contraseñas.',
    },
    {
      titulo: 'Verificamos con el SRI',
      texto:
        'Cada XML que llega se valida en el servicio oficial del SRI. Solo entran comprobantes autorizados.',
    },
    {
      titulo: 'Tu mes, en orden',
      texto:
        'Ingresos, gastos, clientes, IVA y deducibles listos. Al cierre, concilia con el reporte del SRI.',
    },
  ];

  protected readonly funciones: { icono: IconoFuncion; titulo: string; texto: string }[] = [
    {
      icono: 'ingresos',
      titulo: 'Ingresos y clientes',
      texto:
        'Lo que facturas con tu RUC se separa solo, con tus clientes y lo que te compró cada uno.',
    },
    {
      icono: 'categorias',
      titulo: 'Gastos por categoría',
      texto:
        'Vivienda, salud, educación, alimentación, vestimenta y turismo. Categoriza un proveedor una vez y listo.',
    },
    {
      icono: 'tope',
      titulo: 'Alerta de tope deducible',
      texto:
        'Ve cuánto te falta para la rebaja máxima del impuesto a la renta según tus cargas familiares.',
    },
    {
      icono: 'iva',
      titulo: 'IVA del mes',
      texto: 'IVA cobrado en tus facturas frente al IVA pagado en tus compras, mes a mes.',
    },
    {
      icono: 'conciliacion',
      titulo: 'Conciliación con el SRI',
      texto:
        'Sube el reporte mensual del SRI y detecta las facturas que nunca llegaron a tu correo.',
    },
    {
      icono: 'filtros',
      titulo: 'Filtros por fecha y monto',
      texto: 'Encuentra cualquier comprobante por rango de fechas, montos, proveedor o cliente.',
    },
  ];

  protected readonly seguridad = [
    {
      titulo: 'Sin tu clave del SRI',
      texto: 'Nunca te pedimos usuario ni contraseña de SRI en Línea.',
    },
    {
      titulo: 'Sin acceso a tu correo',
      texto:
        'Tú decides qué reenviar al buzón; no entramos a tu bandeja ni guardamos su contraseña.',
    },
    {
      titulo: 'Avisos de cada ingreso',
      texto:
        'Te avisamos por correo cada vez que alguien entra a tu cuenta o cambia su configuración.',
    },
  ];
}
