# Clasificación asistida de gastos e ingresos

## Comportamiento

Al abrir un comprobante, el servidor analiza sus descripciones y devuelve una propuesta, explicación y versión de reglas. Funciona con documentos ya importados y nuevos. El motor es local y determinista: no envía datos a servicios de IA, no ejecuta instrucciones contenidas en las descripciones y no consulta reglas privadas de otras cuentas.

La propuesta no cambia la categoría persistida ni los totales por rubro hasta que el usuario la confirma o elige otra. Las categorías existentes y reglas de proveedor del usuario tienen prioridad. Recordar la elección para todos los documentos del proveedor se ofrece desmarcado; al activarlo se mantiene el comportamiento existente sobre documentos actuales y futuros de esa cuenta.

## Gastos personales

Reglas `GP-2026.1`, revisadas el 11 de octubre de 2026, para comprobantes emitidos en 2026. Sugieren vivienda, educación/arte/cultura, salud, alimentación, vestimenta o turismo. Los nombres del proveedor, RUC o porcentaje de IVA no se usan para inferir un rubro. Cada concepto con importe debe coincidir con un único rubro y todos deben coincidir en el mismo para sugerir la factura completa.

Facturas mixtas, descripciones genéricas, conceptos ambiguos, notas de crédito/débito y años sin reglas quedan para revisión manual. La clasificación por partidas y distribución fiscal de facturas mixtas no está implementada: el usuario debe revisar la factura completa. La categoría comercial de un ingreso no puede convertirlo en gasto ni modificar el XML.

El reconocimiento por términos es una heurística del sistema, no un algoritmo oficial del SRI. Debe confirmarse la finalidad personal, territorio ecuatoriano, titular/cargas y condiciones particulares. Turismo requiere verificar registro del establecimiento y finalidad nacional. Alimentación/salud de mascotas, intereses quirografarios y conceptos cuya elegibilidad exige más contexto no se asignan automáticamente. `NoDeducible` sigue siendo una opción manual; no se declara que un gasto de negocio sea no deducible por no pertenecer a rubros personales.

El sistema no certifica la rebaja, no presenta el Anexo de Gastos Personales y no calcula nuevas condiciones fiscales en esta funcionalidad. Los totales existentes conservan su definición.

## Ingresos

Categorias comerciales: Ventas, Servicios y Otros ingresos. Se sugieren ventas/servicios cuando todos los conceptos son compatibles; en casos mixtos o insuficientes el usuario elige. No representan una clasificación oficial de ingresos gravados/exentos del SRI. La modificación exige plan Profesional/Contadores (o administrador con acceso del plan), propietario de la sesión y un valor del catálogo. El endpoint usa validación y límites de escritura existentes.

Migración `CategoriaIngresos`: añade `comprobantes.categoria_ingreso` nullable, sin sobrescribir datos. Debe aplicarse en el despliegue antes de iniciar la versión nueva del servidor; únicamente la base de pruebas se migra automáticamente al validar.

## Fuentes

- [SRI, gastos personales 2026](https://www.sri.gob.ec/detalle-noticias?idnoticia=1301&marquesina=1): rubros actuales, turismo nacional registrado y conceptos sujetos a revisión.
- [SRI, conceptos y ejemplos de gastos personales](https://www.sri.gob.ec/o/sri-portlet-biblioteca-alfresco-internet/descargar/1094dc69-9983-4bbd-b68b-26e72cf63bd3/Gastos%20personales%20conceptos%20y%20ejemplos.pdf): referencias de los conceptos; no se usan límites ni cálculos históricos de rebaja.

Arquitectura: `Back/Clasificacion` mantiene estrategias de reglas y motor; los DTO exponen la propuesta. `ClasificacionComprobante` presenta revisión/corrección separado de la página de detalle. Tailwind y los tokens compartidos controlan estilos.
