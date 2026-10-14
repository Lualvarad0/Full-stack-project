# Clasificación por partidas y cierre

La clasificación por conceptos distingue Personal, Negocio y No aplicable. Cada partida personal confirma su rubro; las partidas de ingreso confirman su categoría comercial. Las descripciones y montos del XML no se modifican. La página muestra hasta 100 conceptos por vez y guarda esa página; el comprobante queda pendiente hasta revisar todas las partidas.

IVA por partida se conserva del desglose XML; si no existe, se indica desconocido y no se inventa una distribución proporcional. Crédito tributario requiere finalidad de negocio, importe de IVA conocido y selección explícita. La elección del usuario sigue sujeta a los requisitos tributarios, no se certifica automáticamente.

La base personal del resumen usa el subtotal sin IVA/ICE, solo de conceptos personales confirmados o de la clasificación global personal confirmada. Los totales generales de factura y las notas de crédito mantienen sus importes y signos originales. Los gráficos generales por categoría todavía expresan el importe total del documento; la preparación tributaria debe utilizar la base personal confirmada.

Referencia oficial: [guía de renta de personas naturales, exclusión del IVA e ICE](https://www.sri.gob.ec/o/sri-portlet-biblioteca-alfresco-internet/descargar/55428073-d439-4d4b-9d91-388b3306c424/Gu%C3%ADa%20para%20el%20llenado%20del%20Formulario%20Impuesto%20a%20la%20Renta%20personas%20naturales.pdf).

El cierre comprueba clasificación, revisión del IVA de negocio y estados pendientes del SRI. Los registros cerrados no pueden clasificarse, eliminarse ni recibir nuevas importaciones. Las reglas globales de proveedor tampoco pueden modificar meses cerrados. Un cambio de RUC que reclasifique registros cerrados se rechaza. Emisión y cierre se serializan por titular; una factura pendiente de autorización impide cerrar. El período puede reabrirse con motivo de al menos diez caracteres.

El historial registra titular, actor real (incluido contador autorizado), fecha, operación y cambios de partidas. No se elimina al cambiar categorías. El administrador debe suspender cuentas con historial contable; no puede borrarlas desde el panel. Los métodos nuevos conservan permisos de consulta/edición y límites de escritura.

La migración `ClasificacionPartidasYCierre` añade campos, períodos y auditoría. Preserva categorías manuales anteriores y no confirma propuestas automáticas. Las partidas anteriores sin IVA desglosado mantienen ese importe como desconocido.
