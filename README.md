# ServiOS
# ServiOS - Sistema de Gestión de Órdenes de Servicio Técnico

## Integrante
* **Alumno:** Cadelago Pereira, Lautaro Santino
* **Email:** laucadelago123@gmail.com

---

## Descripción del Proyecto
ServiOS es una aplicación web desarrollada con ASP.NET Core MVC y MySQL para laboratorios y talleres de reparación de dispositivos móviles y microelectrónica. Permite registrar clientes, rastrear equipos por número de IMEI, gestionar el ciclo de vida de las órdenes de servicio técnico, registrar presupuestos y controlar señas y cobros asociados.

## Puesta en marcha local

1. Instalar el SDK de .NET requerido por `ServiOS.csproj` y MySQL/MariaDB.
2. Ejecutar `script.sql` en una instalación nueva. Este script elimina y vuelve a crear `ServiOSDB`.
3. Para una base existente, ejecutar `database/migrations/20261105_add_orden_adjuntos.sql` sin recrear la base.
4. Configurar `ConnectionStrings:DefaultConnection` en `appsettings.Development.json` o mediante la variable de entorno `ConnectionStrings__DefaultConnection`.
5. Ejecutar `dotnet restore` y `dotnet run`.

El inicio de sesión usa las cuentas activas de la tabla `Usuarios`. El rol `Admin` administra clientes, equipos, servicios y pagos; `Admin` y `Tecnico` pueden operar órdenes. El perfil permite cargar un avatar, y las imágenes de las órdenes se guardan en `App_Data/uploads/ordenes` y se sirven mediante una acción autenticada.
```mermaid
---
config:
  layout: elk
---
erDiagram
	direction TB
	CLIENTE {
		int id PK ""  
		string dni UK ""  
		string nombre  ""  
		string apellido  ""  
		string telefono  ""  
		string email  ""  
		datetime fecha_alta  ""  
	}

	EQUIPO {
		int id PK ""  
		int cliente_id FK ""  
		string marca  ""  
		string modelo  ""  
		string imei UK ""  
		string clave_desbloqueo  ""  
		string observaciones_esteticas  ""  
		boolean activo  ""  
	}

	TIPO_SERVICIO {
		int id PK ""  
		string descripcion  ""  
		decimal costo_base_estimado  ""  
	}

	ORDEN_REPARACION {
		int id PK ""  
		int equipo_id FK ""  
		int tipo_servicio_id FK ""  
		int usuario_tecnico_id FK ""  
		date fecha_ingreso  ""  
		date fecha_prometida  ""  
		date fecha_entrega_real  ""  
		string falla_declarada  ""  
		string diagnostico_tecnico  ""  
		decimal costo_repuestos  ""  
		decimal costo_mano_obra  ""  
		string estado  ""  
	}

	USUARIO {
		int id PK ""  
		string nombre  ""  
		string apellido  ""  
		string email UK ""  
		string password_hash  ""  
		string rol  ""  
		string avatar  ""  
		boolean activo  ""  
	}

	PAGO {
		int id PK ""  
		int orden_id FK ""  
		int usuario_cobro_id FK ""  
		datetime fecha_pago  ""  
		decimal importe  ""  
		string concepto  ""  
		boolean anulado  ""  
	}

	Untitled-Entity {

	}

	CLIENTE||--o{EQUIPO:"posee"
	TIPO_SERVICIO||--o{ORDEN_REPARACION:"clasifica"
	EQUIPO||--o{ORDEN_REPARACION:"ingresa_en"
	USUARIO||--o{ORDEN_REPARACION:"atiende_crea"
	ORDEN_REPARACION||--o{PAGO:"registra"
	USUARIO}|--|{Untitled-Entity:"  "
