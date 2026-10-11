import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideArrowRight,
  LucideCalculator,
  LucideCalendarCheck,
  LucideChartPie,
  LucideCheck,
  LucideHandCoins,
  LucideLock,
  LucideMail,
  LucidePlus,
  LucideScale,
  LucideShieldCheck,
  LucideSlidersHorizontal,
  LucideSparkles,
} from '@lucide/angular';
import { CANASTA_ENERO, calcularTope } from '../../core/gastos-personales';
import { DineroPipe } from '../../shared/formato';
import { AUTORIA, Credito, Marca, NOMBRE_APP } from '../../shared/marca';
import { DemoPanel } from './demo-panel';
import { FlujoFacturas } from './flujo-facturas';

type IconoFuncion = 'ingresos' | 'categorias' | 'tope' | 'iva' | 'conciliacion' | 'filtros';

/** Landing pública: hero con vista previa del panel + secciones de producto (todo en código, sin imágenes). */
@Component({
  selector: 'app-landing',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink, Marca, Credito, DemoPanel, FlujoFacturas, DineroPipe, LucideSparkles, LucideArrowRight,
    LucideCheck, LucidePlus, LucideMail,
    LucideHandCoins, LucideChartPie, LucideScale, LucideCalculator, LucideCalendarCheck, LucideSlidersHorizontal,
    LucideShieldCheck, LucideLock,
  ],
  templateUrl: './landing.html',
})
export class Landing {
  protected readonly nombre = NOMBRE_APP;
  protected readonly anio = new Date().getFullYear();

  /** Tope real del año en curso (canasta de enero del INEC), si ya está registrado. */
  protected readonly tope = calcularTope(Math.max(...Object.keys(CANASTA_ENERO).map(Number)), 0, false);

  protected readonly pasos = [
    { titulo: 'Activa tu buzón', texto: 'Te damos una dirección propia. Reenvía ahí tus facturas con un filtro de Gmail o dala al comprar. Sin contraseñas.' },
    { titulo: 'Verificamos con el SRI', texto: 'Cada XML que llega se valida en el servicio oficial del SRI. Solo entran comprobantes autorizados.' },
    { titulo: 'Tu mes, en orden', texto: 'Ingresos, gastos, clientes, IVA y deducibles listos. Al cierre, concilia con el reporte del SRI.' },
  ];

  protected readonly funciones: { icono: IconoFuncion; titulo: string; texto: string }[] = [
    { icono: 'ingresos', titulo: 'Ingresos y clientes', texto: 'Lo que facturas con tu RUC se separa solo, con tus clientes y lo que te compró cada uno.' },
    { icono: 'categorias', titulo: 'Gastos por categoría', texto: 'Vivienda, salud, educación, alimentación, vestimenta y turismo. Categoriza un proveedor una vez y listo.' },
    { icono: 'tope', titulo: 'Alerta de tope deducible', texto: 'Ve cuánto te falta para la rebaja máxima del impuesto a la renta según tus cargas familiares.' },
    { icono: 'iva', titulo: 'IVA del mes', texto: 'IVA cobrado en tus facturas frente al IVA pagado en tus compras, mes a mes.' },
    { icono: 'conciliacion', titulo: 'Conciliación con el SRI', texto: 'Sube el reporte mensual del SRI y detecta las facturas que nunca llegaron a tu correo.' },
    { icono: 'filtros', titulo: 'Filtros por fecha y monto', texto: 'Encuentra cualquier comprobante por rango de fechas, montos, proveedor o cliente.' },
  ];

  protected readonly seguridad = [
    { titulo: 'Sin tu clave del SRI', texto: 'Nunca te pedimos usuario ni contraseña de SRI en Línea.' },
    { titulo: 'Sin acceso a tu correo', texto: 'Tú decides qué reenviar al buzón; no entramos a tu bandeja ni guardamos su contraseña.' },
    { titulo: 'Avisos de cada ingreso', texto: 'Te avisamos por correo cada vez que alguien entra a tu cuenta o cambia su configuración.' },
  ];

  protected readonly planes = [
    {
      nombre: 'Personal',
      precio: 'Gratis',
      periodo: '',
      descripcion: 'Para ordenar tus gastos y tu deducible.',
      destacado: false,
      cta: 'Empezar gratis',
      incluye: ['Buzón de facturas propio', 'Verificación con el SRI', 'Gastos por categoría y proveedores', 'Tope deducible y rebaja estimada', 'Conciliación con el reporte del SRI', 'Avisos de seguridad'],
      pronto: [] as string[],
    },
    {
      nombre: 'Profesional',
      precio: '$9',
      periodo: '/mes + IVA',
      descripcion: 'Para quien factura con su RUC.',
      destacado: true,
      cta: 'Probar Profesional',
      incluye: ['Todo lo de Personal', 'Ingresos y clientes', 'IVA del mes: cobrado vs pagado', 'Filtros por fecha y monto'],
      pronto: ['Emitir facturas electrónicas', 'Declaración de IVA (formulario 104)'],
    },
    {
      nombre: 'Contadores',
      precio: 'A medida',
      periodo: '',
      descripcion: 'Para estudios que llevan varios RUC.',
      destacado: false,
      cta: 'Conversemos',
      incluye: ['Varios RUC en una cuenta', 'Panel por cliente', 'Acompañamiento en la puesta en marcha'],
      pronto: [] as string[],
    },
  ];
  protected readonly autoria = AUTORIA;

  protected readonly preguntas = [
    {
      pregunta: '¿Necesito darles mi clave del SRI o de mi correo?',
      respuesta: 'No. Reenvías tus facturas a tu buzón de Nudo y cada una se verifica con el servicio público de autorización del SRI, que no requiere tu clave.',
    },
    {
      pregunta: '¿Por qué no se descargan directamente del SRI con mi RUC?',
      respuesta: 'El SRI no ofrece a otras aplicaciones una forma de leer la lista de tus comprobantes; solo permite verificarlos uno por uno. Por eso usamos el reenvío de tu correo, donde todo emisor está obligado a enviarte la factura, y el reporte mensual del SRI para conciliar.',
    },
    {
      pregunta: '¿Cuánto cuesta?',
      respuesta: 'Empezar es gratis. Durante el lanzamiento todas las funciones disponibles son gratis; el plan Profesional se cobrará cuando lleguen la facturación electrónica y la declaración de IVA.',
    },
    {
      pregunta: '¿Qué pasa con las facturas que no llegan a mi correo?',
      respuesta: 'Al cierre del mes descargas el reporte de comprobantes recibidos de SRI en Línea y lo subes: el sistema agrega las que faltan y no duplica las que ya tienes.',
    },
    {
      pregunta: '¿Sirve si estoy en el RIMPE?',
      respuesta: 'Sí. Registras tu régimen en el perfil y el resumen de ingresos, gastos y deducibles funciona igual.',
    },
    {
      pregunta: '¿Puedo emitir facturas desde la plataforma?',
      respuesta: 'Está en desarrollo, junto con la preparación de la declaración de IVA. Por ahora registramos las facturas que emites desde tu sistema actual.',
    },
  ];
}
