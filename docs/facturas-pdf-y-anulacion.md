# PDF y anulación de facturas emitidas

Profesional y Contadores pueden ver y descargar el PDF desde el detalle de factura. El administrador conserva el acceso previsto por el catálogo de planes. El endpoint `GET /api/facturacion/facturas/{id}/pdf` exige sesión, propietario y plan; aplica los límites de lectura y no permite caché. El PDF se genera desde el XML original guardado, con fuente Ubuntu embebida, código de barras Code 128, autorización, comprador, detalles, impuestos, pagos y paginación. Los documentos de pruebas o no autorizados muestran una advertencia de validez. El PDF no se firma ni reemplaza el XML firmado.

El visor recibe un PDF autenticado como Blob y revoca su URL al cerrar, cambiar de factura, cambiar de sesión o destruir la vista. No recibe URLs externas del usuario.

## Anulación ante el SRI

Revisión de documentación oficial: 11 de octubre de 2026. No se encontró una API pública documentada para anular facturas autorizadas. La ficha 2.34 publica recepción, autorización y consultas. El servicio de anulación de descuento de devolución de IVA corresponde a otro proceso y no anula una factura.

El botón Gestionar anulación ofrece la clave y acceso al portal oficial. `POST /api/facturacion/facturas/{id}/anular` valida propietario, plan, estado autorizado y exclusión de consumidor final; **no envía una solicitud al SRI**, no cambia el estado ni elimina ingresos. La finalización del trámite y su confirmación tributaria se realizan en el portal. No se implementó sincronización automática de anulaciones ni emisión de notas de crédito.

Las resoluciones 14 y 17 de 2025 establecen el día 7 del mes siguiente para anulación en línea, con ajuste por días de descanso/feriados. Fuera del plazo procede una nota de crédito dentro de las condiciones normativas. Las facturas a consumidor final no admiten anulación ni nota de crédito. La aceptación del receptor exigida para retenciones, notas de crédito y débito no debe generalizarse a facturas. El portal determina las restricciones adicionales y calendario aplicable.

**Registros históricos:** la implementación anterior marcaba `Anulada` y eliminaba ingresos sin confirmación del SRI. Esos registros requieren conciliación individual con el SRI; esta actualización no cambia su estado ni reconstruye ingresos automáticamente.

Fuentes oficiales:
- [Facturación electrónica, ficha técnica 2.34 y guías](https://www.sri.gob.ec/facturacion-electronica).
- [Resolución NAC-DGERCGC25-00000014](https://www.sri.gob.ec/o/sri-portlet-biblioteca-alfresco-internet/descargar?id=137046a6-787c-47fb-a2d7-176595d292dc&nombre=NAC-DGERCGC25-00000014.pdf).
- [Reforma NAC-DGERCGC25-00000017](https://www.sri.gob.ec/o/sri-portlet-biblioteca-alfresco-internet/descargar?id=e98fc8a6-299e-4ea9-8de7-2f6c70dbb4f5&nombre=NAC-DGERCGC25-00000017.pdf).

Dependencias: PDFsharp 6.2.4 (MIT), ZXing.Net 0.16.11 (Apache 2.0), fuentes Ubuntu originales (Ubuntu Font Licence 1.0, incluida en `Back/Facturacion/Fonts/UFL.txt`). Las fuentes se empaquetan como recursos para Windows/Linux sin depender de fuentes del sistema.
