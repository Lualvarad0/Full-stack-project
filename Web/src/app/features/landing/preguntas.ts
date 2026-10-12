import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LucidePlus } from '@lucide/angular';
@Component({
  selector: 'app-preguntas-landing',
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucidePlus],
  templateUrl: './preguntas.html',
})
export class PreguntasLanding {
  protected readonly preguntas = [
    {
      pregunta: '¿Necesito darles mi clave del SRI o de mi correo?',
      respuesta:
        'No. Reenvías tus facturas a tu buzón de Nudo y cada una se verifica con el servicio público de autorización del SRI, que no requiere tu clave.',
    },
    {
      pregunta: '¿Por qué no se descargan directamente del SRI con mi RUC?',
      respuesta:
        'El SRI no ofrece a otras aplicaciones una forma de leer la lista de tus comprobantes; solo permite verificarlos uno por uno. Por eso usamos el reenvío de tu correo, donde todo emisor está obligado a enviarte la factura, y el reporte mensual del SRI para conciliar.',
    },
    {
      pregunta: '¿Cuánto cuesta?',
      respuesta:
        'El plan Personal es gratis. Profesional cuesta $14,99 al mes + IVA (o $149 al año) e incluye la facturación electrónica; Contadores, $49 al mes + IVA para hasta 10 RUC. Los planes de pago tienen 14 días gratis y se pagan con tarjeta.',
    },
    {
      pregunta: '¿Qué pasa con las facturas que no llegan a mi correo?',
      respuesta:
        'Al cierre del mes descargas el reporte de comprobantes recibidos de SRI en Línea y lo subes: el sistema agrega las que faltan y no duplica las que ya tienes.',
    },
    {
      pregunta: '¿Sirve si estoy en el RIMPE?',
      respuesta:
        'Sí. Registras tu régimen en el perfil y el resumen de ingresos, gastos y deducibles funciona igual.',
    },
    {
      pregunta: '¿Puedo emitir facturas desde la plataforma?',
      respuesta:
        'Sí, con el plan Profesional: subes tu firma electrónica (.p12), creas la factura y Nudo la firma, la envía al SRI y te muestra su autorización. La declaración de IVA llega pronto.',
    },
  ];
}
