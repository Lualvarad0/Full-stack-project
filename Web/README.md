# Frontend de Nudo

El frontend vigente es `Web/` (Angular standalone). El repositorio conserva únicamente esta aplicación; el frontend antiguo está excluido del control de versiones.

## Ejecutar

Desde `Web/`:

```bash
npm install
npm run setup:env
npm start
```

La aplicación abre en http://localhost:4200. Para compilar: `npm run build`. Para verificar la configuración local: `npm run build -- --configuration development`.

## Organización

```text
src/app/
  app.routes.ts       Rutas, carga diferida y permisos de entrada
  app.config.ts       Proveedores globales de Angular
  core/
    auth/             Sesión, modelos, guards e interceptor de autenticación
    admin/            Servicio, modelos y guard de administración
    comprobantes/     Comprobantes, categorías, importación, filtros y período
    facturacion/      Emisión, firma, servicios, modelos y tarifas
    perfil/           Perfil tributario, deducibles y validators compartidos
    suscripcion/      Planes, pagos, modelos y guard del plan
    contabilidad/     Servicios de cartera, períodos, tesorería, libros y tributación
    correo/           Buzón, actividad y sus contratos
    ui/               Preferencias, avisos, marca y tipos visuales
    http/             Carga reactiva y tratamiento de errores HTTP
    storage/          Persistencia local
    utils/            Texto, fechas, formato, archivos y búsqueda generales
  features/
    auth/             Inicio de sesión y registro
    landing/          Presentación pública y demostración
    resumen/          Resumen de la cuenta
    gastos/           Gastos recibidos y deducibles
    ingresos/         Ingresos del RUC
    comprobante/      Detalle de un comprobante
    importar/         Importación de comprobantes
    proveedores/      Proveedores de los gastos
    clientes/         Clientes de los ingresos
    facturacion/      Emisión, detalle y catálogo de servicios
    suscripcion/      Planes, prueba y pagos
    configuracion/    Perfil y configuración de la cuenta
    cartera/          Autorizaciones y selección de contribuyente
    contabilidad/     Revisión, cierres, retenciones y libros
    tesoreria/        Cobros, pagos, bancos y conciliación
    tributacion/      Preparaciones, cargas, obligaciones y exportaciones
    admin/            Administración por rol
    proximamente/     Pantalla común de funciones futuras
  layout/             Estructura de navegación de la sesión
  shared/
    components/       Componentes reutilizados entre funcionalidades
    pipes/            Formato para las plantillas
src/environments/     Configuración local ignorada por Git y plantillas públicas
```

Cada funcionalidad mantiene juntos sus componentes y sus plantillas. Un componente usado solamente por una funcionalidad pertenece a esa carpeta; pasa a `shared/` cuando se reutiliza. `core/` contiene infraestructura y contratos compartidos. Los alias existentes son `@core/*`, `@features/*`, `@shared/*` y `@env/*`.

## Core por área y validadores

Cada área de `core/` reúne sus archivos en subcarpetas `services/`, `models/`, `constants/`, `guards/`, `interceptors/`, `utils/` o `validators/` según corresponda. Solo se crean las subcarpetas que tienen contenido. Los imports identifican el área, por ejemplo `@core/auth` o `@core/facturacion`; dentro del área se usan imports directos para evitar dependencias sobre su propio índice.

Los validadores exclusivos van en `features/<área>/validators/`. Los compartidos entre áreas, como cédula y RUC, van en `core/perfil/validators/identificacion.validator.ts`. Los modelos privados de la landing están en `features/landing/models/`. Las utilidades generales permanecen en `core/utils/`; no se mezclan allí reglas del perfil o de facturación.

## Responsabilidades, herencia y reutilización

Las páginas coordinan componentes y acciones. Las plantillas extensas viven en archivos HTML, y los bloques con estado o responsabilidad propia son componentes independientes.

| Carpeta | Responsabilidades |
| --- | --- |
| `layout/` | `Shell` controla el menú móvil y la navegación; `Sidebar` presenta los enlaces y la cuenta; `Header` maneja el período y el tema. |
| `gastos/`, `ingresos/`, `comprobante/` | `Gastos` e `Ingresos` heredan de `ListadoComprobantes`. La base implementa carga, búsqueda, filtros y sumas netas; los métodos `filtrosAdicionales`, `filtrosActivos` y `seleccionar` permiten especializar el comportamiento. Ambos componen `TablaComprobantes`, que solo presenta los datos y emite cambios de categoría. El detalle conserva sus acciones específicas. |
| `admin/` | La página selecciona una pestaña. `AdminResumen`, `AdminUsuarios`, `AdminPagos` y `AdminSistema` tienen sus propios datos y vistas; solo se carga la pestaña activa. |
| `facturacion/` | `NuevaFactura` coordina emisión y validación final; `ClienteFacturaFormulario` y `LineasFacturaEditor` manejan sus campos. `calculos-factura.ts` centraliza subtotales y totales; `validators/cliente.validator.ts` comparte la validación progresiva y `validators/factura.validator.ts` valida la emisión. Listado, servicios y detalle mantienen sus acciones específicas. |
| `configuracion/` | La página compone tarjetas. `PerfilFormulario` concentra el formulario tributario y sus validaciones. |
| `importar/` | `Importar` maneja archivos y peticiones; `ResultadoImportacion` presenta los resultados y observaciones. |
| `resumen/` | `ResumenEstado` carga datos y calcula indicadores; es un proveedor local de la página, sin estado global entre sesiones. `Resumen` compone la vista y los gráficos existentes. |
| `landing/` | Planes y preguntas tienen componentes propios; el contador animado y los datos ilustrativos están separados de la demo y su recorrido. |
| `auth/` | La pantalla compone `FormularioAcceso`; los validadores de edad se encuentran en `validators/registro.validator.ts`. |
| `clientes/`, `proveedores/` | Listados independientes con filtros y acciones propios. Reutilizan el esqueleto de carga; clientes e ingresos comparten el aviso para completar el perfil. |
| `suscripcion/` | La página coordina selección del plan, alta y cancelación. `TarjetaPago` encapsula el SDK y los campos seguros. |
| `proximamente/` | Componente pequeño parametrizado por la ruta. |

La herencia se usa para algoritmos comunes con variaciones del dominio. La composición se usa para bloques visuales, formularios y tarjetas. No se crean jerarquías de componentes por plan ni se duplican páginas. `crearBusqueda` centraliza la espera de búsqueda y cancela los temporizadores al limpiar o destruir la vista.

## Tailwind y colores

`src/styles.css` importa Tailwind y los archivos del sistema visual:

- `styles/tokens.css`: única definición de colores del producto, marca y temas claro/oscuro.
- `styles/theme.css`: tokens expuestos como utilidades de Tailwind y animaciones del producto.
- `styles/components.css`: estilos reutilizables con `@apply` (`card`, `btn`, `input`, `estado-ok`, `estado-error`, `estado-atencion`, etc.).
- `styles/base.css`: reglas base y utilidades generales.
- `styles/motion.css`: transiciones y preferencias de movimiento reducido.

Usar clases semánticas como `text-success`, `bg-card` o `btn-primary`, sin repetir códigos hexadecimales en los componentes. Kushki acepta nombres de clases: `TarjetaPago` pasa `campo-tarjeta`, `campo-tarjeta-focus` y `campo-tarjeta-invalid`, definidos con `@utility` y `@apply` en `styles/components.css`. El SDK toma los estilos calculados de esas clases al montar los campos seguros; así no se duplican medidas, tipografía o colores en TypeScript. Los estados visuales repetidos se definen una sola vez con Tailwind y se llaman por clase.

## Verificación de la estructura

Ejecutar `npm run setup:env` y `npm run build` desde esta carpeta. Las pruebas se mantienen como archivos locales excluidos de esta entrega de producción.

## Planes y permisos

Se organiza por funcionalidades porque los planes comparten pantallas. No crear copias de gastos, configuración o resumen por cada plan.

| Plan de la API | Acceso actual |
| --- | --- |
| Personal (gratuito) | Resumen, gastos, importación, proveedores, comprobantes y cuenta, con límites aplicados por la API |
| Profesional (de pago) | Funciones comunes, facturación, tesorería, libros y preparación tributaria |
| Contadores (de pago) | Funciones profesionales y cartera de contribuyentes con autorización y permisos individuales |

`core/suscripcion/constants/planes.ts` contiene los textos del catálogo; `core/suscripcion/models/suscripcion.model.ts` define los identificadores. Los precios efectivos y límites vienen del backend. `core/suscripcion/guards/plan-profesional.guard.ts` y `core/admin/guards/admin.guard.ts` controlan sus respectivas rutas. Los controles de plan dentro de las pantallas se mantienen para las funciones compartidas. El rol administrador tiene sus reglas independientes del plan.

Al añadir una función exclusiva, usar la misma funcionalidad y el guard correspondiente en `app.routes.ts`. Los permisos efectivos también deben validarse en la API.

## Entornos fuera de Git

`environment.ts` y `environment.development.ts` son locales y no se versionan. Las plantillas `.ts.example` son públicas y permiten reconstruir la configuración de un clon nuevo con `npm run setup:env`. El comando conserva archivos existentes.

- Desarrollo: editar `src/environments/environment.development.ts` (API local por defecto).
- Producción: crear o proporcionar `src/environments/environment.ts` antes de compilar (ruta `/api` por defecto).
- CI/despliegue: ejecutar `npm run setup:env` antes del build o generar los archivos con la configuración de ese entorno.

Angular sustituye el archivo de producción por el de desarrollo al usar la configuración `development`. Todo valor incluido en el frontend puede verse en el navegador: los secretos pertenecen al backend.

Excluir los archivos evita futuras versiones; no borra las versiones de commits anteriores.
