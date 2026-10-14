# Cartera de contribuyentes

El contador necesita plan Contadores (o rol administrador con acceso explícito) y correo verificado. Solicita autorización mediante el correo de una cuenta activa del titular. La respuesta es genérica; no descubre datos de cuentas desconocidas ni permite acceso mientras la solicitud está pendiente. Las solicitudes vencen en siete días. No se envía más de una solicitud al mismo titular por día y se limita el volumen por contador.

El titular revisa solicitudes en Cartera y accesos. Debe verificar su correo para aceptar. Consulta permite leer y exportar; Edición permite trabajar con sus comprobantes, incluyendo emitir con la firma ya configurada. El consentimiento muestra esa capacidad. El contador no puede modificar firma, datos del emisor ni perfil del titular. Cuenta, contraseña, sesiones, pagos y buzón no se delegan.

El selector envía `X-Contribuyente` solo a las áreas contables. El servidor valida vínculo activo, permiso, cuenta y plan en cada petición. El encabezado nunca sustituye esa autorización. Los registros conservan `UsuarioId` del titular. El plan del contador habilita temporalmente las funciones profesionales en la petición, sin cambiar la suscripción del titular.

La revocación bloquea las siguientes peticiones. El frontend cancela cargas y oculta inmediatamente los datos al cambiar de contribuyente. No persiste la selección entre sesiones. El titular sigue trabajando con su propia cuenta y puede retirar permisos.

Migración `CarteraContadores` añade vínculos; no cambia propietarios ni registros existentes. La prueba funcional cubre consentimiento, consulta, edición, firma privada, terceros, revocación y pérdida de plan.
