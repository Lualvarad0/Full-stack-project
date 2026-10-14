# Nudo — facturas, gastos y SRI

SaaS para personas con RUC y contadores en Ecuador: verifica comprobantes con el SRI, propone categorías para revisión, emite documentos electrónicos y permite controlar gastos, cobros, pagos, bancos y libros contables. Incluye preparación tributaria 2026 con revisión explícita del usuario.

## Tecnologías

- **Frontend** (`Web/`): Angular 22 (standalone, signals, zoneless) + Tailwind CSS 4 + Lucide
  - Modo claro/oscuro, diseño responsive

- **Backend**: .NET 8 + PostgreSQL
  - API RESTful (clientes, productos, facturas, usuarios)
  - Autenticación JWT con roles (`admin`, `user`); el registro público siempre crea cuentas `user`
  - Entity Framework Core con migraciones (Npgsql)

## Requisitos Previos

- [Node.js y npm](https://nodejs.org/) (para Angular)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) y la herramienta `dotnet-ef` (`dotnet tool install -g dotnet-ef`)
- [Docker](https://www.docker.com/) (para PostgreSQL local)

## Instalación y ejecución

1. **Levantar PostgreSQL** (desde la raíz del repo):

   ```bash
   docker compose up -d
   ```

2. **Configurar secretos del backend** (desde `Back/`; quedan fuera del repo):

   ```bash
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=facturas;Username=facturas;Password=facturas_dev"
   dotnet user-secrets set "Jwt:Key" "<una clave aleatoria de al menos 32 caracteres>"
   ```

3. **Crear las tablas y ejecutar el backend** (desde `Back/`):

   ```bash
   dotnet ef database update
   dotnet run
   ```

   La API queda en `http://localhost:5000` y Swagger en `http://localhost:5000/swagger`.

4. **Ejecutar el frontend** (desde `Web/`):

   ```bash
   npm install
   npm run setup:env
   npm start
   ```

   La app queda en `http://localhost:4200` y usa la API de `http://localhost:5000/api` (`src/environments/environment.development.ts`). En producción usa `/api` del mismo dominio (`src/environments/environment.ts`).

## Estructura del repositorio

| Ruta | Contenido |
| --- | --- |
| `Back/` | API .NET, modelos, servicios, migraciones y recursos necesarios |
| `Web/` | Frontend Angular vigente, componentes, Tailwind y plantillas públicas de entorno |
| `infra/` | Worker de recepción de correo para Cloudflare; los secretos se configuran externamente |
| `docker-compose.yml` | PostgreSQL para desarrollo local; no es una configuración de producción |
| `.gitignore` | Excluye dependencias, compilaciones, secretos, entornos y copias locales |

El frontend antiguo y las pruebas se conservan únicamente como copias locales y no se incluyen en esta entrega de producción. Los archivos de entorno reales tampoco se versionan.

Para verificar una instalación nueva:

```bash
dotnet build Back/Back.csproj --configuration Release
```

Desde `Web/`, ejecutar `npm run setup:env` y `npm run build`. Consulta [la organización del frontend](Web/README.md).

## Perfil tributario, ingresos y gastos

`GET/PUT /api/perfil` guarda cédula, RUC, razón social y régimen. Con el RUC registrado, los comprobantes cuyo emisor es tu RUC se clasifican como ingresos; el resto, como gastos. Cambiar RUC invalida sus revisiones fiscales y requiere períodos abiertos para reclasificar. Las cargas familiares identificadas se registran por ejercicio en el apartado de gastos personales.

## Gastos personales desde el SRI

El módulo `api/comprobantes` importa tus facturas de consumo verificándolas contra el servicio web público de autorización del SRI (no requiere tu clave de SRI en Línea).

1. En **SRI en Línea → Facturación electrónica → Comprobantes electrónicos recibidos**, consulta el mes y descarga el reporte `.txt`.
2. Súbelo en `POST /api/comprobantes/importar` (también acepta los `.xml` que llegan al correo, varios a la vez), o pega claves de acceso en `POST /api/comprobantes/importar/claves`.
3. La API extrae las claves de acceso (49 dígitos, validadas con módulo 11), consulta cada una en el SRI y guarda las facturas, notas de crédito y notas de débito autorizadas con su detalle e IVA. Las que ya existen no se vuelven a consultar.
4. Revisa la sugerencia y confirma o cambia la categoría. La finalidad personal/negocio y el tratamiento de IVA se revisan por documento o por partida; recordar una categoría del proveedor es una opción explícita.
5. `GET /api/comprobantes/resumen?anio=2026&mes=10` devuelve el gasto neto (las notas de crédito restan), IVA, total deducible, totales por categoría y principales proveedores; sin `mes`, el resumen anual con desglose mensual.

## Buzón de reenvío (facturas automáticas desde el correo)

Cada usuario tiene una dirección propia `<código>@<Correo:DominioEntrante>` (`GET /api/correo`). Reenvía ahí sus facturas con un filtro de Gmail (`has:attachment filename:xml`); Nudo lee los XML (y ZIP), los verifica con el SRI y los registra como gasto o ingreso. Cuando Gmail pide confirmar la dirección de reenvío, Nudo detecta el código y lo muestra en pantalla. No se guardan contraseñas de correo.

Para activarlo en un dominio (ejemplo con Cloudflare, gratis):

1. Agrega un subdominio de facturas (p. ej. `facturas.tudominio.com`) a **Cloudflare Email Routing**.
2. Crea un **Email Worker** con [infra/cloudflare-email-worker.js](infra/cloudflare-email-worker.js) y sus variables `NUDO_WEBHOOK_URL` (`https://<api>/api/correo/entrante`) y `NUDO_WEBHOOK_SECRET`.
3. En Email Routing, regla **catch-all** → enviar al Worker.
4. En la API: `Correo:DominioEntrante=facturas.tudominio.com` y `Correo:SecretoWebhook=<el mismo secreto>`.

El webhook también acepta formularios multipart de Mailgun o SendGrid Inbound Parse (campo `recipient`/`to` y adjuntos).

## Aislamiento de cuentas

Cada sesión accede únicamente a los registros de su titular. El acceso de un contador requiere autorización activa de ese titular, con permiso de consulta o edición. El servidor valida propietarios, sesiones y límites de solicitudes incluso si se llama directamente a la API.

## Acceso seguro y contabilidad

Recuperación de contraseña por correo, verificación de correo y sesiones revocables. Configurar `Smtp:Host`, `Smtp:Puerto`, `Smtp:Usuario`, `Smtp:Clave`, `Smtp:Remitente` y `Frontend:Url` para habilitar el envío. Los enlaces de recuperación vencen, son de un solo uso y no contienen la contraseña.

Los planes de pago incluyen cartera autorizada, revisión de partidas, retenciones, notas de crédito, documentos PDF, cierres y auditoría, tesorería, conciliación bancaria, plan de cuentas, asientos, mayor y balances. Las exportaciones entregan bases CSV/XLSX, XML originales y un paquete ZIP para el contador.

La preparación de renta automática corresponde a personas naturales y al ejercicio 2026. Las sociedades utilizan los libros y bases con conciliación fiscal específica. Revisar una declaración o validar la estructura de un XML ATS no lo presenta al SRI: la presentación se registra manualmente con su referencia. El calendario requiere confirmar obligaciones y feriados. La anulación de documentos autorizados se completa en SRI en Línea.

## Avisos de seguridad

Cada ingreso al sistema y cada cambio de configuración (perfil, RUC, dirección del buzón) queda en **Configuración → Actividad de tu cuenta** (`GET /api/avisos`) y se envía por correo al usuario si hay SMTP configurado (`Smtp:Host`, `Smtp:Puerto`, `Smtp:Usuario`, `Smtp:Clave`, `Smtp:Remitente`). Sin SMTP, el envío solo se registra en el log.

## Planes y pagos (Kushki)

Personal es gratis. Profesional ($9,99/mes o $99/año + IVA) y Contadores ($49/mes o $490/año + IVA, hasta 15 RUC) se cobran con tarjeta mediante suscripciones de [Kushki](https://docs.kushki.com/ec/recurring-payments/overview), con 14 días gratis. Emitir facturas electrónicas requiere un plan de pago (el administrador está exento). Los precios están en `Back/Pagos/CatalogoPlanes.cs` y los textos en `Web/src/app/core/suscripcion/constants/planes.ts`.

1. En la consola de Kushki (UAT para pruebas), copia la clave pública, la privada y la firma de webhooks (Desarrolladores → Webhooks).
2. Configura `Kushki:ClavePublica`, `Kushki:ClavePrivada`, `Kushki:FirmaWebhook` y `Kushki:Pruebas` (`true` en UAT).
3. Registra el webhook `https://<api>/api/pagos/kushki/webhook` para los eventos de suscripciones con tarjeta.

### Producción

Configura por variables de entorno: `ConnectionStrings__Default`, `Jwt__Key`, `Cors__Origins__0` (URL del frontend), `Correo__DominioEntrante`, `Correo__SecretoWebhook`, `Smtp__*`, `Frontend__Url` HTTPS, `Sri__RucProveedor` y `DataProtection__RutaClaves` (carpeta privada y persistente).

**Base de datos en [Neon](https://neon.tech)** (PostgreSQL administrado):

- **Proyecto:** crea un proyecto en la región más cercana a tus usuarios (para Ecuador, AWS São Paulo `sa-east-1`) con PostgreSQL 16.
- **Cadena de la app:** usa la cadena *pooled* (host con `-pooler`) en formato .NET: `Host=ep-xxx-pooler.sa-east-1.aws.neon.tech;Database=neondb;Username=...;Password=...;SSL Mode=Require;Timeout=30`. `Timeout=30` da margen a que Neon despierte el cómputo si estaba en reposo.
- **Cadena de migraciones:** aplica las migraciones con la cadena *directa* (sin `-pooler`) y `dotnet ef database update --project Back --connection "<cadena directa>"`.
- **Reintentos:** no actives `EnableRetryOnFailure`. La API abre transacciones propias (`FOR UPDATE` al emitir y en la cartera), y la estrategia de reintentos de EF no las admite.

Aplica las migraciones antes de iniciar la versión actualizada. `Database__MigrateOnStartup=true` permite aplicarlas al arrancar si se habilita explícitamente. Los JWT anteriores a la incorporación de sesiones revocables requieren iniciar sesión de nuevo. Respalda tanto la base de datos como las claves de cifrado. Las credenciales de PostgreSQL en `docker-compose.yml` son solo un ejemplo de desarrollo y no deben usarse en producción.

---

Diseño y desarrollo: [Cornerstone Develop](https://www.alvarado-dev.com/)


## Contribución

Si deseas contribuir, realiza un fork del repositorio, crea una rama para tus cambios y envía un pull request para revisión.

1. Haz un fork del proyecto.
2. Crea una nueva rama (`git checkout -b feature/nueva-funcionalidad`).
3. Realiza tus cambios y haz un commit (`git commit -m 'Agregar nueva funcionalidad'`).
4. Envía los cambios (`git push origin feature/nueva-funcionalidad`).
5. Abre un pull request.
