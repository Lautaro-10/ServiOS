# ServiOS
# ServiOS - Sistema de Gestión de Órdenes de Servicio Técnico

## Integrante
* **Alumno:** Cadelago Pereira, Lautaro Santino
* **Email:** laucadelago123@gmail.com

---

## Descripción del Proyecto
ServiOS es una aplicación web desarrollada con ASP.NET Core MVC y MySQL para laboratorios y talleres de reparación de dispositivos móviles y microelectrónica. Permite registrar clientes, rastrear equipos por número de IMEI, gestionar el ciclo de vida de las órdenes de servicio técnico, registrar presupuestos y controlar señas y cobros asociados.

## Puesta en marcha local

1. Instalar el SDK 10 de .NET y MySQL/MariaDB.
2. Ejecutar `script.sql` en una instalación nueva. Este script elimina y vuelve a crear `ServiOSDB`.
3. Configurar `ConnectionStrings:DefaultConnection` en `appsettings.Development.json` o mediante la variable de entorno `ConnectionStrings__DefaultConnection`.
4. Ejecutar `dotnet restore` y `dotnet run`.

El proyecto apunta a `net10.0`. Vue 3 se sirve desde `wwwroot/lib/vue` para que la pantalla AJAX no dependa de una CDN. Para regenerar la dependencia local, ejecutar `npm install` y copiar `node_modules/vue/dist/vue.global.prod.js` a `wwwroot/lib/vue/vue.global.prod.js`.

La sección de Clientes demuestra un CRUD SPA: la vista usa Vue y todas las operaciones de listado, alta, edición y baja se realizan contra `/api/clientes` como JSON, incluyendo paginación del lado del servidor. Los demás módulos conservan sus vistas MVC.

El inicio de sesión usa las cuentas activas de la tabla `Usuarios`. El rol `Admin` administra clientes, equipos, servicios y pagos; `Admin` y `Tecnico` pueden operar órdenes. El perfil permite cargar un avatar, y las imágenes de las órdenes se guardan en `App_Data/uploads/ordenes` y se sirven mediante una acción autenticada.

## Evidencias de requisitos

Las capturas y el video de evidencia se encuentran en [`docs/evidencias/`](./docs/evidencias/). La tabla enlaza cada archivo disponible;

| Requerimiento mínimo | Estado | Enlace al código / controladores | Evidencia (captura / GIF / video) |
|---|---|---|---|
| Modelo relacional: seis tablas principales relacionadas, relaciones 1:N y órdenes vinculadas a equipo y tipo de servicio |  Implementado | [Definición de tablas](./script.sql#L5) · [Claves foráneas](./script.sql#L15) | DER documentado en Mermaid debajo de la tabla.  `docs/evidencias/`. |
| Login, autorización por roles y avatar de usuario |  Implementado | [Cookies y política Admin](./Program.cs#L9) · [Login y perfil](./Controllers/UsuariosController.cs#L25) · [Restricción de Clientes](./Controllers/ClientesController.cs#L6) | [Captura del login](<./docs/evidencias/Captura-login.jpg>) · [Captura del perfil](<./docs/evidencias/Captura%20de%20pantalla%20Perfil%20App.jpg>) |
| Manejo de archivos adicional al avatar |  Implementado | [Carga y consulta de adjuntos de órdenes](./Controllers/OrdenesReparacionController.cs#L467) · [Validación y almacenamiento](./Helpers/ImageUploadHelper.cs#L3) |  |
| ABM/CRUD con Vue.js y AJAX/JSON sin recarga |  Implementado en Clientes | [Vista Vue](./Views/Clientes/Index.cshtml#L1) · [Peticiones AJAX](./wwwroot/js/clientes.js#L1) · [API JSON](./Controllers/Api/ClientesApiController.cs#L9) | [Video del CRUD de Clientes](<./docs/evidencias/Clientes%20ABM.mp4>) |
| Paginación por servidor en listados principales |  Implementado | [Clientes API](./Controllers/Api/ClientesApiController.cs#L23) · [Equipos](./Controllers/EquiposController.cs#L44) · [Órdenes](./Controllers/OrdenesReparacionController.cs#L91) · [Pagos](./Controllers/PagosController.cs#L21) · [Modelo de respuesta paginada](./Models/PaginacionRespuesta.cs) | [Captura de la petición de paginación](<./docs/evidencias/Captura-red-paginador.jpg>). Tipos de Servicio se considera catálogo maestro pequeño. |


### Diagrama entidad-relación

```mermaid
---
config:
  layout: elk
---
erDiagram
    CLIENTES {
        int Id PK
        string Dni UK
        string Nombre
        string Apellido
        string Telefono
        string Email
        datetime FechaAlta
    }
    EQUIPOS {
        int Id PK
        int ClienteId FK
        string Marca
        string Modelo
        string Imei UK
        string ClaveDesbloqueo
        string ObservacionesEsteticas
        boolean Activo
    }
    TIPOS_SERVICIO {
        int Id PK
        string Descripcion
        decimal CostoBaseEstimado
    }
    USUARIOS {
        int Id PK
        string Nombre
        string Apellido
        string Email UK
        string PasswordHash
        string Rol
        string Avatar
        boolean Activo
    }
    ORDENES_REPARACION {
        int Id PK
        int EquipoId FK
        int TipoServicioId FK
        int UsuarioTecnicoId FK
        date FechaIngreso
        date FechaPrometida
        date FechaEntregaReal
        string FallaDeclarada
        string DiagnosticoTecnico
        decimal CostoRepuestos
        decimal CostoManoObra
        string Estado
    }
    PAGOS {
        int Id PK
        int OrdenId FK
        int UsuarioCobroId FK
        datetime FechaPago
        decimal Importe
        string Concepto
        boolean Anulado
    }
    ORDEN_ADJUNTOS {
        int Id PK
        int OrdenId FK
        string RutaArchivo
        datetime FechaSubida
    }
    CLIENTES ||--o{ EQUIPOS : posee
    EQUIPOS ||--o{ ORDENES_REPARACION : recibe
    TIPOS_SERVICIO ||--o{ ORDENES_REPARACION : clasifica
    USUARIOS o|--o{ ORDENES_REPARACION : atiende
    ORDENES_REPARACION ||--o{ PAGOS : registra
    USUARIOS ||--o{ PAGOS : cobra
    ORDENES_REPARACION ||--o{ ORDEN_ADJUNTOS : adjunta
