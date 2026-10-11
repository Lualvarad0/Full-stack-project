# Nudo — facturas, gastos y SRI

SaaS para personas con RUC en Ecuador: importa y verifica con el SRI los comprobantes que recibes, categoriza tus gastos personales, controla el tope deducible y (próximamente) emite facturas electrónicas y prepara la declaración mensual de IVA.

## Tecnologías

- **Frontend** (`Web/`): Angular 22 (standalone, signals, zoneless) + Tailwind CSS 4 + Lucide
  - Modo claro/oscuro, diseño responsive
  - Carpeta `Front/`: proyecto Angular 16 original, ya no se usa

- **Backend**: .NET 8 + PostgreSQL
  - API RESTful (clientes, productos, facturas, usuarios)
  - Autenticación JWT con roles (`admin`, `user`); el primer usuario registrado es `admin`
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
   npm start
   ```

   La app queda en `http://localhost:4200` y usa la API de `http://localhost:5000/api` (`src/environments/environment.development.ts`). En producción usa `/api` del mismo dominio (`src/environments/environment.ts`).

### Pruebas

Con PostgreSQL levantado (`docker compose up -d`), desde la raíz:

```bash
dotnet test Back.Tests
```

Las pruebas de integración crean y borran su propia base `facturas_test` y usan un SRI simulado.

## Perfil tributario, ingresos y gastos

`GET/PUT /api/perfil` guarda cédula, RUC (se validan con el algoritmo del Registro Civil/SRI), razón social, régimen, cargas familiares y enfermedad catastrófica. Con el RUC registrado, cada comprobante importado cuyo emisor es tu RUC se guarda como **ingreso** (`direccion=Emitido`) y el resto como **gasto** (`Recibido`); al cambiar el RUC se reclasifica todo. Los gastos que no están a tu cédula/RUC se marcan (`aNombreDelUsuario=false`). Los clientes salen de las facturas emitidas: `GET /api/comprobantes/clientes`.

## Gastos personales desde el SRI

El módulo `api/comprobantes` importa tus facturas de consumo verificándolas contra el servicio web público de autorización del SRI (no requiere tu clave de SRI en Línea).

1. En **SRI en Línea → Facturación electrónica → Comprobantes electrónicos recibidos**, consulta el mes y descarga el reporte `.txt`.
2. Súbelo en `POST /api/comprobantes/importar` (también acepta los `.xml` que llegan al correo, varios a la vez), o pega claves de acceso en `POST /api/comprobantes/importar/claves`.
3. La API extrae las claves de acceso (49 dígitos, validadas con módulo 11), consulta cada una en el SRI y guarda las facturas, notas de crédito y notas de débito autorizadas con su detalle e IVA. Las que ya existen no se vuelven a consultar.
4. Asigna una categoría (Vivienda, Educacion, Salud, Alimentacion, Vestimenta, Turismo, NoDeducible) con `PUT /api/comprobantes/{id}/categoria`: se recuerda para el proveedor y se aplica a sus comprobantes actuales y futuros.
5. `GET /api/comprobantes/resumen?anio=2026&mes=10` devuelve el gasto neto (las notas de crédito restan), IVA, total deducible, totales por categoría y principales proveedores; sin `mes`, el resumen anual con desglose mensual.

## Buzón de reenvío (facturas automáticas desde el correo)

Cada usuario tiene una dirección propia `<código>@<Correo:DominioEntrante>` (`GET /api/correo`). Reenvía ahí sus facturas con un filtro de Gmail (`has:attachment filename:xml`); Nudo lee los XML (y ZIP), los verifica con el SRI y los registra como gasto o ingreso. Cuando Gmail pide confirmar la dirección de reenvío, Nudo detecta el código y lo muestra en pantalla. No se guardan contraseñas de correo.

Para activarlo en un dominio (ejemplo con Cloudflare, gratis):

1. Agrega un subdominio de facturas (p. ej. `facturas.tudominio.com`) a **Cloudflare Email Routing**.
2. Crea un **Email Worker** con [infra/cloudflare-email-worker.js](infra/cloudflare-email-worker.js) y sus variables `NUDO_WEBHOOK_URL` (`https://<api>/api/correo/entrante`) y `NUDO_WEBHOOK_SECRET`.
3. En Email Routing, regla **catch-all** → enviar al Worker.
4. En la API: `Correo:DominioEntrante=facturas.tudominio.com` y `Correo:SecretoWebhook=<el mismo secreto>`.

El webhook también acepta formularios multipart de Mailgun o SendGrid Inbound Parse (campo `recipient`/`to` y adjuntos).

## Avisos de seguridad

Cada ingreso al sistema y cada cambio de configuración (perfil, RUC, dirección del buzón) queda en **Configuración → Actividad de tu cuenta** (`GET /api/avisos`) y se envía por correo al usuario si hay SMTP configurado (`Smtp:Host`, `Smtp:Puerto`, `Smtp:Usuario`, `Smtp:Clave`, `Smtp:Remitente`). Sin SMTP, el envío solo se registra en el log.

### Producción

Configura por variables de entorno: `ConnectionStrings__Default`, `Jwt__Key`, `Cors__Origins__0` (URL del frontend), `Correo__DominioEntrante`, `Correo__SecretoWebhook`, `Smtp__*`, `Frontend__Url` y, si quieres que la API aplique las migraciones al arrancar, `Database__MigrateOnStartup=true`.

---

Diseño y desarrollo: [Cornerstone Develop](https://portafolio-hwaq.vercel.app/)


## Contribución

Si deseas contribuir, realiza un fork del repositorio, crea una rama para tus cambios y envía un pull request para revisión.

1. Haz un fork del proyecto.
2. Crea una nueva rama (`git checkout -b feature/nueva-funcionalidad`).
3. Realiza tus cambios y haz un commit (`git commit -m 'Agregar nueva funcionalidad'`).
4. Envía los cambios (`git push origin feature/nueva-funcionalidad`).
5. Abre un pull request.

## Licencia

Este proyecto está bajo la licencia MIT. Consulta el archivo `LICENSE` para más información.
