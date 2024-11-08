# Ingreso y Gestión de Facturas


[![Angular|Project](https://i.ytimg.com/vi/9ZD7cKIaxdM/hq720.jpg?sqp=-oaymwEhCK4FEIIDSFryq4qpAxMIARUAAAAAGAElAADIQj0AgKJD&rs=AOn4CLAQb1MLfIrKeAzWKkyZXJnganG8Ew)](https://blog.angular.dev/angular-v16-is-here-4d7a28ec680d)

Este es un sistema de **Ingreso y Gestión de Facturas** desarrollado con **Angular 16** para el frontend y **.NET 6** para el backend. La aplicación permite a los usuarios realizar el registro, consulta y gestión de facturas de manera intuitiva y eficiente.

## Características

- **Crear y Listar Facturas**: Permite registrar nuevas facturas con información detallada y consultar la lista de facturas existentes.
- **Consulta y Búsqueda**: Opción para buscar facturas específicas mediante filtros.
- **Gestión de Facturas**: Edición y eliminación de facturas existentes.
- **Autenticación de Usuarios**: Seguridad integrada para controlar el acceso de usuarios.
- **Dashboard Interactivo**: Visualización general de facturas e informes.

## Tecnologías Utilizadas

- **Frontend**: Angular 16
  - Componentes reutilizables
  - Formularios reactivos
  - Servicios de conexión HTTP
  - Interfaz de usuario moderna con Angular Material (opcional)
  
- **Backend**: .NET 6
  - API RESTful para CRUD de facturas
  - Control de acceso basado en roles
  - Integración con base de datos (SQL Server, MySQL, etc.)

## Requisitos Previos

Asegúrate de tener instalados los siguientes programas:

- [Node.js y npm](https://nodejs.org/) (para Angular)
- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Git](https://git-scm.com/) (opcional para clonación del proyecto)

## Instalación

2. **Configurar el backend**:

   - Navega a la carpeta de tu proyecto backend.
   - Configura la conexión a la base de datos en el archivo `appsettings.json`.
   - Ejecuta las migraciones (si es necesario) y prepara la base de datos.

## Ejecución

1. **Ejecución del frontend**:

   Desde la carpeta `Front`, ejecuta el siguiente comando:

   ```bash
   ng serve

2. **Ejecución del backend**:

   Desde la carpeta del proyecto backend, ejecuta:

   ```bash
   dotnet run


## Contribución

Si deseas contribuir, realiza un fork del repositorio, crea una rama para tus cambios y envía un pull request para revisión.

1. Haz un fork del proyecto.
2. Crea una nueva rama (`git checkout -b feature/nueva-funcionalidad`).
3. Realiza tus cambios y haz un commit (`git commit -m 'Agregar nueva funcionalidad'`).
4. Envía los cambios (`git push origin feature/nueva-funcionalidad`).
5. Abre un pull request.

## Licencia

Este proyecto está bajo la licencia MIT. Consulta el archivo `LICENSE` para más información.
