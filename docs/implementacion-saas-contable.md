# Trabajo autorizado: SaaS contable

El usuario autorizó todos los módulos de la evaluación anterior, además de recuperación de contraseña. Este archivo registra el alcance de implementación y permite revisar las entregas sin confundir módulos preparados con trámites presentados al SRI.

- [x] Acceso seguro: recuperar/cambiar contraseña, verificar correo, gestionar sesiones y avisos (75 pruebas servidor, 34 frontend en esta etapa; SMTP y migración pendientes de despliegue).
- [x] Cartera de contribuyentes con solicitud, autorización, revocación y permisos por contribuyente; conserva los propietarios originales y limita acceso delegado.
- [x] Finalidad personal/negocio y clasificación por partidas, incluyendo facturas mixtas y base personal sin IVA/ICE.
- [x] Retenciones recibidas/emitidas, XML/PDF y notas de crédito propias con controles de sustento y saldos; SRI simulado en pruebas.
- [x] Revisión, cierre y reapertura justificada de períodos, con auditoría y bloqueo de cambios/importaciones.
- [x] Preparación de IVA, renta de persona natural 2026, retenciones y bases ATS, con revisión e historial; presentación registrada manualmente.
- [x] Cobros/pagos parciales, movimientos bancarios y conciliación.
- [x] Exportaciones CSV/XLSX/ZIP, XML originales y validador XSD de un ATS completo del propio RUC.
- [x] Cargas familiares identificadas, proyección y preparación de gastos personales 2026.
- [x] Perfil de obligaciones y calendario por régimen; feriados nacionales/locales y condiciones especiales requieren confirmación del usuario.
- [x] Entrega al contador mediante paquete privado y cartera autorizada, informes y estado de pendientes.
- [x] Plan de cuentas individual, asientos revisables/contabilizados, mayor, balance de comprobación y situación/resultados.

Las migraciones se prueban en `facturas_test`. No se ejecutan automáticamente sobre la base operacional durante el desarrollo. No se presentan declaraciones ni se envían documentos al SRI en nombre de usuarios reales como parte de las pruebas.

Alcances y límites: `tesoreria-y-libros.md`, `preparacion-tributaria.md` y `recuperacion-y-seguridad-cuenta.md`. No se genera/presenta automáticamente un ATS oficial ni se liquida automáticamente renta de sociedades. SMTP, URL pública, claves persistentes de cifrado, RUC del proveedor y migraciones necesitan configuración de despliegue.

Verificación de esta entrega: suite completa del servidor con 89 pruebas aprobadas; frontend con 40 pruebas aprobadas y compilación de producción correcta. Tras los últimos ajustes, seis pruebas específicas de libros, períodos, tributación y documentos volvieron a pasar. Modelo EF sin cambios pendientes respecto de sus migraciones; diferencias sin errores de espacios. PDF de nota de crédito y retención renderizados e inspeccionados visualmente. No se realizó commit, push ni despliegue.
