import { PlanesLanding } from './planes';
import { PreguntasLanding } from './preguntas';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideArrowRight,
  LucideCalculator,
  LucideCalendarCheck,
  LucideChartPie,
  LucideHandCoins,
  LucideLock,
  LucideMail,
  LucideMenu,
  LucideScale,
  LucideShieldCheck,
  LucideLandmark,
  LucideFileText,
  LucideUsers,
  LucideWallet,
  LucideX,
} from '@lucide/angular';
import { CANASTA_ENERO, calcularTope } from '@core/perfil';
import { NOMBRE_APP } from '@core/ui';
import { FuncionLanding } from '@features/landing/models/landing.model';
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
    LucideArrowRight,
    LucideMail,
    LucideHandCoins,
    LucideChartPie,
    LucideScale,
    LucideCalculator,
    LucideCalendarCheck,
    LucideWallet,
    LucideFileText,
    LucideLandmark,
    LucideUsers,
    LucideShieldCheck,
    LucideLock,
    LucideMenu,
    LucideX,
  ],
  templateUrl: './landing.html',
})
export class Landing {
  protected readonly nombre = NOMBRE_APP;
  protected readonly anio = new Date().getFullYear();
  /** Menú a pantalla completa en móvil. */
  protected readonly menuAbierto = signal(false);

  /** Tope real del año en curso (canasta de enero del INEC), si ya está registrado. */
  protected readonly tope = calcularTope(
    Math.max(...Object.keys(CANASTA_ENERO).map(Number)),
    0,
    false,
  );

  protected readonly pasos = [
    {
      titulo: 'Empieza gratis',
      texto:
        'Dinos cuánto ganas y anota tus cuentas del mes. Marca el día en que pagas cada una y mira cuánto te queda.',
    },
    {
      titulo: 'Trae tus facturas',
      texto:
        'Con Profesional reenvías tus facturas a tu buzón propio o subes el reporte del SRI. Cada XML se verifica con el SRI.',
    },
    {
      titulo: 'Tu mes, en orden',
      texto:
        'Ingresos, gastos, clientes, IVA y deducibles listos. Al cierre, concilia y prepara tu declaración.',
    },
  ];

  /** Cada función con el plan desde el que se incluye (los planes superiores incluyen lo de los inferiores). */
  protected readonly funciones: FuncionLanding[] = [
    {
      icono: 'cuentas',
      plan: 'Personal',
      titulo: 'Mis cuentas del mes',
      texto:
        'Anota arriendo, servicios y deudas; marca el día en que pagas cada una y ve cuánto te queda de lo que ganas.',
    },
    {
      icono: 'factura',
      plan: 'Profesional',
      titulo: 'Facturación electrónica',
      texto:
        'Emite facturas, notas de crédito y retenciones firmadas con tu firma electrónica y autorizadas por el SRI.',
    },
    {
      icono: 'ingresos',
      plan: 'Profesional',
      titulo: 'Ingresos y clientes',
      texto:
        'Lo que facturas con tu RUC se separa solo, con tus clientes y lo que te compró cada uno.',
    },
    {
      icono: 'categorias',
      plan: 'Profesional',
      titulo: 'Gastos por categoría',
      texto:
        'Sube tus facturas o tráelas por correo: vivienda, salud, educación, alimentación, vestimenta y turismo.',
    },
    {
      icono: 'tope',
      plan: 'Profesional',
      titulo: 'Alerta de tope deducible',
      texto:
        'Ve cuánto te falta para la rebaja máxima del impuesto a la renta según tus cargas familiares.',
    },
    {
      icono: 'iva',
      plan: 'Profesional',
      titulo: 'IVA y declaraciones',
      texto:
        'IVA cobrado frente a pagado, vencimientos según tu RUC y la preparación de tus declaraciones.',
    },
    {
      icono: 'conciliacion',
      plan: 'Profesional',
      titulo: 'Conciliación con el SRI',
      texto:
        'Sube el reporte mensual del SRI y detecta las facturas que nunca llegaron a tu correo.',
    },
    {
      icono: 'bancos',
      plan: 'Profesional',
      titulo: 'Cobros, pagos y bancos',
      texto:
        'Registra cobros y pagos, importa tu estado de cuenta y concilia cada movimiento con su factura.',
    },
    {
      icono: 'cartera',
      plan: 'Contadores',
      titulo: 'Cartera de contribuyentes',
      texto:
        'Atiende varios RUC desde una cuenta: cada cliente te autoriza con permiso de consulta o edición.',
    },
  ];

  protected readonly etiquetaPlan: Record<FuncionLanding['plan'], string> = {
    Personal: 'Gratis',
    Profesional: 'Profesional',
    Contadores: 'Contadores',
  };

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
