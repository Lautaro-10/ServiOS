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

| Requerimiento mínimo | Estado | Enlace al código / controladores | 
|---|---|---|---|
| Modelo relacional: seis tablas principales relacionadas, relaciones 1:N y órdenes vinculadas a equipo y tipo de servicio |  Implementado | [script.sql:5](./script.sql#L5) · [claves foráneas](./script.sql#L24) | DER documentado abajo.  |
| Login, autorización por roles y avatar de usuario | Implementado | [configuración de cookies y política Admin](./Program.cs#L9) · [login, perfil y actualización del avatar](./Controllers/UsuariosController.cs#L25) · [protección de Clientes](./Controllers/ClientesController.cs#L6) |  |
| Manejo de archivos adicional al avatar |  Implementado | [subida y consulta de adjuntos de órdenes](./Controllers/OrdenesReparacionController.cs#L467) · [validación y almacenamiento de imágenes](./Helpers/ImageUploadHelper.cs#L3) | Captura o GIF mostrando la carga y visualización de una imagen pendiente. |
| ABM/CRUD con Vue.js y AJAX/JSON sin recarga |  Implementado en Clientes | [vista Vue](./Views/Clientes/Index.cshtml#L1) · [lógica Vue y peticiones AJAX](./wwwroot/js/clientes.js#L1) · [API JSON de Clientes](./Controllers/Api/ClientesApiController.cs#L9) | GIF o video mostrando listado, alta, edición y baja sin recargar la página pendiente. |
| Paginación por servidor en listados principales |  Implementado | [Clientes API](./Controllers/Api/ClientesApiController.cs#L23) · [Equipos](./Controllers/EquiposController.cs#L44) · [Órdenes](./Controllers/OrdenesReparacionController.cs#L91) · [Pagos](./Controllers/PagosController.cs#L21) · [componente de paginación](./Models/PaginacionRespuesta.cs) | |

Las evidencias marcadas como pendientes deben capturarse durante la prueba final y guardarse en el repositorio (por ejemplo, dentro de `docs/evidencias/`) antes de la presentación. No se incluyen capturas simuladas.

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
