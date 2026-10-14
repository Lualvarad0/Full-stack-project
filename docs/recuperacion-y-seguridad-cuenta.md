# Acceso seguro

- Recuperación por correo: respuesta genérica para cuentas activas, inexistentes o suspendidas, con tiempo mínimo similar; límite de acceso existente por IP y adicional de tres enlaces por cuenta/hora, separados al menos un minuto.
- Token aleatorio de 256 bits, hash SHA-256 almacenado en `tokens_cuenta`. Vence en 30 minutos, tiene propósito separado de verificación y es de un solo uso incluso bajo solicitudes simultáneas.
- Enlaces construidos desde `Frontend:Url` confiable, HTTPS en producción. El token va en el fragmento, no se envía al servidor web al abrir la página y se retira del historial visible. El restablecimiento solo ocurre tras enviar y confirmar una contraseña.
- Contraseñas BCrypt: al menos ocho caracteres y máximo 72 bytes UTF-8; no se truncan silenciosamente. No se filtran símbolos de contraseñas como si fueran HTML.
- Cambio desde Configuración exige contraseña actual. Recuperar o cambiar invalida todas las sesiones y enlaces de recuperación anteriores, registra actividad y notifica sin incluir la contraseña. Después se inicia sesión normalmente.
- Verificación de correo de un solo uso, 24 horas, sin bloqueo retroactivo de cuentas existentes.
- Sesiones registradas por JWT `jti`, versión de seguridad y dispositivo. Configuración permite consultar propias, cerrar una o cerrar las otras. Salir por la interfaz revoca la sesión en el servidor antes de limpiar la pestaña.

## Correo y despliegue

Configurar `Smtp:Host`, `Puerto`, `Usuario`, `Clave`, `Remitente` y `Frontend:Url`. Recuperación necesita SMTP; sin configurarlo mantiene respuesta genérica y no crea enlaces. Verificación informa que el correo no está disponible.

Los correos de acceso se guardan cifrados con Data Protection en una cola persistente independiente de avisos. El worker reintenta fallos hasta cinco veces, exige TLS (STARTTLS, o SSL en puerto 465), elimina el mensaje tras el envío y no registra enlaces ni contraseñas en logs. Persistir y proteger `DataProtection:RutaClaves` para sobrevivir reinicios y compartir claves entre instancias.

La migración `AccesoSeguroCuenta` añade tokens, cola y sesiones. Debe aplicarse antes de iniciar el servidor actualizado. Los JWT anteriores al despliegue no contienen sesión registrada ni versión y requieren ingresar de nuevo. Las pruebas usan transporte de correo simulado, sin enviar mensajes reales.

Referencia: [OWASP Forgot Password](https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html).
