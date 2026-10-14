# Seguridad y aislamiento de cuentas

## Regla de acceso

Una sesión normal solo puede leer y modificar registros cuyo propietario coincide con el usuario del JWT validado. Los IDs de una ruta, los filtros y un `usuarioId` enviado por el navegador no conceden permisos. Los registros ajenos responden con el mismo 404 que un ID inexistente.

Las tablas físicas siguen siendo comunes; sus filas privadas quedan separadas por `UsuarioId`. El servidor controla esa separación, también si alguien llama directamente a la API. La reutilización de componentes Angular no comparte datos entre cuentas.

| Área                                      | Protección revisada                                                                                                                                                                                                     |
| ----------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Clientes y productos del catálogo antiguo | Listados, búsquedas, detalles, edición, eliminación y duplicados filtrados por propietario. Cada cuenta conserva su descripción y su stock.                                                                             |
| Facturas del catálogo antiguo             | Listados y detalles propios. Clientes y productos usados para emitir deben pertenecer a la misma cuenta. La eliminación solo devuelve stock propio.                                                                     |
| Comprobantes, XML y proveedores           | Consultas y cambios por propietario; categorías de proveedor e importaciones separadas por cuenta.                                                                                                                      |
| Perfil, firma y datos de emisión          | El propietario proviene del JWT; configuración y secuencias independientes.                                                                                                                                             |
| Facturas electrónicas y servicios         | Detalles, XML, modificación, eliminación, reintentos y anulación por propietario.                                                                                                                                       |
| Buzón, avisos y suscripción               | Datos propios; webhooks requieren su secreto o firma, además del límite de solicitudes.                                                                                                                                 |
| Administración                            | Acceso explícito por rol administrativo. El administrador conserva los permisos de gestión de usuarios y métricas del negocio; una cuenta normal no puede acceder a esas rutas. Un JWT con rol revocado deja de servir. |

El registro público crea únicamente cuentas `user`, incluso si la base está vacía. El primer administrador debe asignarse mediante una operación privada del responsable de la base de datos; no se habilita desde un formulario público. Los administradores existentes conservan su rol.

## Sesiones del front

El token se guarda en `sessionStorage`, separado del almacenamiento compartido del navegador. Se elimina la copia antigua en `localStorage`, por lo que la actualización puede exigir volver a iniciar sesión. Las pestañas abiertas desde otra pueden recibir una copia inicial de `sessionStorage` del navegador; no debe confundirse eso con un navegador o perfil independiente.

Al cambiar de cuenta, las cargas ocultan inmediatamente los datos anteriores; las solicitudes pendientes se cancelan y sus respuestas no actualizan la siguiente sesión. El perfil se comprueba contra el usuario actual, las notificaciones se limpian y el período se reinicia. El tema se guarda con una clave por cuenta. El Bearer solo se adjunta al origen y la ruta de la API, no a una URL que simplemente comparte un prefijo.

Las respuestas de la API incluyen `Cache-Control: no-store` y `X-Content-Type-Options: nosniff`.

## Límites de solicitudes y entrada

El límite corresponde a las solicitudes que envía un formulario, una búsqueda o una acción, no a cada tecla presionada. Se ejecuta en el servidor y cubre todos los controladores, incluidas las rutas antiguas. Cambiar de endpoint o ID no reinicia el presupuesto de la misma categoría.

| Presupuesto                                                           | Valor por minuto | Clave de configuración    |
| --------------------------------------------------------------------- | ---------------: | ------------------------- |
| Global por IP                                                         |              300 | `RateLimiting:PorIp`      |
| Lecturas por usuario; sin sesión, por IP                              |              180 | `RateLimiting:Lecturas`   |
| Escrituras por usuario                                                |               30 | `RateLimiting:Escrituras` |
| Registro e inicio de sesión, compartidos por IP                       |                8 | `RateLimiting:Acceso`     |
| Importar, emitir, reintentar, anular, firma y suscripción por usuario |                6 | `RateLimiting:Costosas`   |
| Webhooks por IP                                                       |              120 | `RateLimiting:Webhooks`   |

Se usa una ventana fija de un minuto, sin cola. Al excederla se responde 429 con `Retry-After`. Son límites por instancia de la API: un despliegue con varias réplicas necesita un límite compartido en el gateway o un almacén distribuido. No se confía en `X-Forwarded-For` arbitrarios; si se despliega detrás de un proxy, debe configurarse el transporte de la IP real únicamente desde proxies de confianza y revisar el presupuesto global para ese despliegue.

Los DTO y filtros verifican tipos, enumeraciones, formatos, rangos y longitudes. Los campos comerciales admiten texto plano, sin etiquetas ni controles ocultos; las contraseñas conservan sus caracteres especiales. Las líneas de una factura se limitan a 100, una importación a 500 claves y la subida manual a 20 archivos. El cuerpo normal se limita a 1 MiB en Kestrel; las cargas específicas mantienen límites de 10 MiB para importaciones, 25 MiB para correo y 100 KiB para firmas.

## Inyecciones

Las consultas se mantienen en EF con parámetros, incluido el SQL interpolado que bloquea la secuencia de emisión. Una cadena como `' OR 1=1` es un dato, no SQL ejecutable. No se bloquean palabras SQL como sustituto de la parametrización. El front muestra los textos con interpolación de Angular; no se encontraron usos de `innerHTML` o bypass de sanitización en el código activo.

El lector XML rechaza DTD y entidades, no resuelve recursos externos y limita el tamaño del documento. La clave de acceso se valida antes de construir el mensaje SOAP. Los errores de lectura XML devuelven un mensaje genérico; el detalle técnico queda en el registro del servidor.

Estas medidas cubren los caminos revisados y no constituyen una garantía absoluta de ausencia de vulnerabilidades. Deben mantenerse al añadir endpoints y revisarse junto con TLS, permisos de la base, secretos y la configuración real del alojamiento.

Referencias: [ASP.NET Core Rate Limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-8.0), [OWASP: consultas parametrizadas](https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet).

## Migración de catálogos existentes

`AislamientoCatalogos` añade propietarios a clientes y productos e identifica clientes de forma única por usuario e identificación. No borra ni trunca descripciones antiguas y no adivina a qué cuenta pertenece un registro antiguo.

Los catálogos antiguos quedan inicialmente con propietario nulo, fuera de la API. Las facturas antiguas que los referencian también quedan fuera de las vistas hasta que un responsable verifique y asigne correctamente todos sus propietarios. Si un registro fue usado por varias cuentas, debe separarse y ajustar sus referencias antes de asignarlo; no se debe entregarlo completo a una sola cuenta.

Antes del despliegue, revisar el inventario de esos registros y preparar su asignación si existen. Aplicar la migración con `dotnet ef database update --project Back`, usando la conexión del despliegue; no se ha aplicado automáticamente sobre la base operativa durante esta revisión. Volver al esquema anterior puede fallar si ya existen identificaciones repetidas entre cuentas y elimina la información de propiedad, por lo que no debe usarse como mecanismo rutinario de recuperación.

## Verificación

Las pruebas de seguridad comprueban el acceso cruzado a catálogos, facturas, XML, servicios, perfil y buzón; IDs falsificados, límites compartidos entre rutas y separación entre usuarios, acceso público limitado, roles revocados, rechazo de HTML y XML con entidades, límites de longitud y SQL tratado como texto. Las pruebas del front cubren cambio de cuenta con respuestas tardías, alcance del token y preferencias separadas.

Las pruebas de integración usan exclusivamente `facturas_test` y un SRI/Kushki simulado. Sus presupuestos generales son altos porque comparten una IP; los casos de rate limit levantan una API con límites bajos y verifican el 429 real.
