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
import { AVISO_LANZAMIENTO, CANASTA_ENERO, NOMBRE_APP, PLANES } from '@core/constants';
import { IconoFuncion } from '@core/models';
import { calcularTope } from '@core/utils';
import { Credito, Marca } from '@shared/components';
import { DineroPipe } from '@shared/pipes';
import { DemoPanel } from './demo-panel';
import { FlujoFacturas } from './flujo-facturas';

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

  protected readonly planes = PLANES;
  protected readonly avisoLanzamiento = AVISO_LANZAMIENTO;

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
      respuesta: 'El plan Personal es gratis. Profesional cuesta $14,99 al mes + IVA (o $149 al año) e incluye la facturación electrónica; Contadores, $49 al mes + IVA para hasta 10 RUC. Los planes de pago tienen 14 días gratis y se pagan con tarjeta.',
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
      respuesta: 'Sí, con el plan Profesional: subes tu firma electrónica (.p12), creas la factura y Nudo la firma, la envía al SRI y te muestra su autorización. La declaración de IVA llega pronto.',
    },
  ];
}
