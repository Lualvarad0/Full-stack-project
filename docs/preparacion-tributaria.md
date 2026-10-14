# Preparación tributaria y entrega al contador

Implementación del ejercicio **2026**, reglas `EC-2026.1`. No reutiliza sus tablas para otros años. La preparación utiliza comprobantes del ambiente de producción; la importación ya no incorpora comprobantes autorizados de pruebas a los registros tributarios.

## Obligaciones y calendario

Mi RUC permite registrar actividades, establecimientos, periodicidad de IVA/retenciones IR/ATS, renta, gastos personales, sociedad, contribuyente especial y domicilio Galápagos. Sugiere periodicidad desde el régimen, pero exige contrastarla con el certificado del RUC. No consulta ni actualiza automáticamente el catastro del SRI.

Los vencimientos se calculan por noveno dígito, periodicidad y condición del titular. Renta general de persona natural vence en marzo siguiente; renta RIMPE de persona natural, en mayo; sociedades, en abril. IVA y retenciones vencen el mes siguiente; ATS, el subsiguiente; AGP, en febrero siguiente. Ajusta fines de semana y **feriados observados que el usuario debe completar**, incluidos decretos y traslados excepcionales. Especiales anticipan el vencimiento no hábil; para los demás se desplaza al siguiente hábil sin abandonar el mes. La configuración no confirmada aparece expresamente como pendiente.

## Revisión fiscal

Separada de las categorías comerciales/personales: ingreso gravado general/RIMPE/exento; gasto deducible general/costo RIMPE/no deducible. El usuario confirma código de sustento, forma de pago, relación y observación. El sistema valida formato y dirección, no decide elegibilidad de exenciones o catálogos únicamente por palabras de una descripción.

Cambiar RUC invalida las revisiones fiscales y obliga a revisar finalidad/IVA de los documentos reclasificados. Cambiar RUC o régimen invalida la confirmación de obligaciones.

## IVA y retenciones

Bases por código de tarifa del XML original, notas con signo, crédito de compras del negocio expresamente confirmado, partidas mixtas, factor de proporcionalidad, crédito anterior y ajustes con sustento. Muestra débito, crédito, retenciones recibidas, valor estimado a pagar y saldo a favor. Retenciones emitidas y recibidas se preparan por período fiscal, separando renta e IVA.

No incorpora operaciones que no estén registradas: importaciones, documentos físicos, devoluciones y otros casos deben completarse en los ajustes y en la preparación oficial correspondiente. El impuesto estimado no incluye multas/intereses ni certifica aceptación del formulario 104/103.

## Renta y gastos personales

Tabla progresiva general 2026 y tabla RIMPE. RIMPE usa cobros y retenciones vinculados, netos de impuestos, más ajustes de ingresos percibidos externos. Separa esos ingresos de los del régimen general y no deduce costos RIMPE ni aplica rebaja personal a su impuesto. Para personas naturales, se añaden sueldos gravados, IESS personal, retenciones del formulario 107, ingresos/costos externos, exoneraciones y anticipos con sustento. Las sociedades disponen de libros y bases exportables; esta liquidación de renta de persona natural **bloquea su revisión como declaración de sociedad** y requiere conciliación fiscal específica.

Cargas identificadas por año, parentesco, nacimiento, ingresos y condición acreditada. Valida hijos hasta el año de cumplir 21 (sin límite de edad si hay discapacidad), dependencia confirmada e ingresos anuales hasta SBU 2026 de USD 482. El usuario declara uso exclusivo; no revela ni consulta cargas de otras cuentas y no sustituye su registro en SRI. El número orientativo del perfil se sincroniza al añadir/quitar cargas del ejercicio.

Gastos personales netos sin IVA/ICE, únicamente a nombre del titular o cargas registradas, con partidas personales y rubros confirmados. Permite proyección por rubro. Canasta enero 2026 USD 821,80; 7/9/11/14/17/20 canastas según cargas; 100 en el supuesto de discapacidad o enfermedad catastrófica/rara/huérfana; ajuste Galápagos 1,803. Rebaja potencial 18 % del menor entre base personal y límite; rebaja efectiva limitada al impuesto causado general. Los conceptos requieren sus condiciones legales y respaldo.

## Revisión y presentación

Guarda una preparación revisada con huella de cálculo, documentos, revisiones fiscales, cargas, obligaciones y retenciones. Detecta si las bases cambiaron; no permite registrar presentación sobre una revisión desactualizada. La presentación se registra **manualmente**, con fecha y referencia del comprobante del SRI, conservando revisiones anteriores. Revisar o marcar presentado no envía una declaración al SRI.

## Exportación

Excel XLSX real con identificaciones y claves como texto; CSV UTF-8 con protección contra fórmulas en textos. Balance, diario, tesorería, documentos y paquete ZIP con XML originales y preparaciones JSON. El paquete incluye renta anual y revisiones de todo el ejercicio, aunque el intervalo de comprobantes sea mensual. Limita comprobantes a 5000 y XML a 50 MB por entrega; intervalos menores permiten descargar conjuntos grandes.

ATS: base de trabajo con sustento y pagos, y validación de un XML ATS completo mediante el esquema oficial embebido `ats.xsd`. Comprueba estructura y RUC del informante, prohíbe DTD/entidades externas y limita tamaño. **No genera ni presenta automáticamente un ATS oficial** a partir de un conjunto potencialmente incompleto; los catálogos y reglas de negocio se completan/validan con el programa ATS vigente. La validación XSD no implica aceptación del SRI. Los JSON/CSV/XLSX del paquete no se etiquetan como formularios oficiales.

## Fuentes primarias consultadas el 11 de octubre de 2026

- [Tablas IR del SRI](https://www.sri.gob.ec/o/sri-portlet-biblioteca-alfresco-internet/descargar/fa75d2ba-c784-4b33-af3a-8390f2f7af13/Tablas%20c%C3%A1lculo%20IR.pdf), resolución NAC-DGERCGC25-00000043.
- [Proyección de gastos personales 2026](https://www.sri.gob.ec/detalle-noticias?idnoticia=1261&marquesina=1), límites, condición especial e índice Galápagos.
- [RIMPE del SRI](https://www.sri.gob.ec/rimpe), tablas y plazos. Se conserva el valor de fracción básica 2.797,52 publicado por el SRI para el tramo de 200.000 a 300.000; no se sustituye por cifras de fuentes secundarias.
- [Impuesto a la Renta del SRI](https://www.sri.gob.ec/impuesto-renta), base, IESS y ajuste de vencimientos.
- [Ministerio del Trabajo: SBU 2026](https://www.trabajo.gob.ec/despues-de-casi-una-decada-hay-consenso-gobierno-empleadores-y-trabajadores-acuerdan-fijar-el-salario-basico-unificado-de-2026-en-usd-482-no-hay-imposicion-hay-union/).
- [Anexos y guías oficiales](https://www.sri.gob.ec/web/intersri/formularios-e-instructivos1), ATS 1.19.0 para septiembre 2026 en adelante y enlace al [esquema descargado](https://descargas.sri.gob.ec/download/anexos/ats/ats.xsd).

Migración `PreparacionTributaria`. Pruebas exclusivamente en `facturas_test`, SRI simulado y sin presentación real.
