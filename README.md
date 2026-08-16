# AgroDirecto

Plataforma de venta directa del agricultor al consumidor — aplicación web de comercio electrónico.

AgroDirecto conecta a pequeños agricultores locales directamente con los consumidores finales, eliminando intermediarios de la cadena de comercialización agrícola. Los agricultores publican y gestionan su catálogo de productos; los consumidores navegan el catálogo, arman un carrito de compras y realizan pedidos en línea.

- **Curso:** Desarrollo de Servicios Web I
- **Stack:** ASP.NET Core MVC (.NET 8) · ADO.NET · SQL Server · SignalR · Bootstrap 5
- **Metodología:** RUP (Inicio, Elaboración, Construcción, Transición)

El acceso a datos se hace con **ADO.NET** (`SqlConnection` / `SqlCommand` / `SqlDataReader`) y **procedimientos almacenados**, siguiendo el estilo del curso. No se usa Entity Framework.

---

# Cómo ponerlo en marcha

> ### ⚠️ Léelo antes de empezar
>
> **Si ya habías creado la base `AgroDirectoDB_V2` en una versión anterior del proyecto, bórrala primero.**
> El proyecto cambió: se agregó la tabla `Compra` y dos columnas nuevas en `Pedido`. Los scripts
> crean las tablas desde cero, no las modifican, así que sobre una base vieja te van a salir
> errores de *"Ya hay un objeto con el nombre..."* y la aplicación fallará al abrir los pedidos.
>
> En SSMS, antes de todo:
>
> ```sql
> USE master;
> GO
> ALTER DATABASE AgroDirectoDB_V2 SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
> DROP DATABASE AgroDirectoDB_V2;
> GO
> ```
>
> No pierdes nada importante: todo lo que hay son datos de prueba y los scripts los vuelven a crear.
> Si es la primera vez que clonas, sáltate esto.

## 1. Lo que necesitas instalado

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) o superior
- SQL Server Express, con la instancia llamada `SQLEXPRESS`
- SQL Server Management Studio (SSMS)

## 2. Clonar el repositorio

```bash
git clone https://github.com/alexanderchumbes07/AgroDirecto.git
```

## 3. Crear la base de datos — los 9 scripts, en orden

El repositorio **no incluye la base de datos**, solo los scripts que la crean. Están en la carpeta
`database/` y **hay que ejecutarlos en orden**, del 01 al 09: cada uno da por hecho que el anterior
ya corrió.

Ábrelos en SSMS y ejecuta cada uno con **F5**:

| Orden | Archivo | Qué hace |
|---|---|---|
| 1 | `01_CrearBaseDatos.sql` | Crea la base, las 14 tablas, los índices y los datos maestros (roles, categorías, distritos, unidades) |
| 2 | `02_ProcedimientosAlmacenados.sql` | Categorías, unidades de medida y distritos |
| 3 | `03_ProcedimientosSeguridad.sql` | Registro y login |
| 4 | `04_DatosIniciales.sql` | Crea el usuario administrador |
| 5 | `05_ProcedimientosProducto.sql` | Productos del agricultor y catálogo |
| 6 | `06_DatosPrueba.sql` | 3 agricultores, 1 cliente y 17 productos con fotos |
| 7 | `07_ProcedimientosUsuario.sql` | Gestión de usuarios y aprobación de agricultores |
| 8 | `08_ProcedimientosCarrito.sql` | Carrito, compra y estados del pedido |
| 9 | `09_ProcedimientosReporte.sql` | Reportes de ventas |

**Cómo saber si salió bien:** al terminar deberías tener **14 tablas y 48 procedimientos**.
Para comprobarlo, ejecuta esto en SSMS:

```sql
USE AgroDirectoDB_V2;
SELECT COUNT(*) AS Tablas FROM sys.tables;          -- debe decir 14
SELECT COUNT(*) AS Procedimientos FROM sys.procedures;  -- debe decir 48
```

Si te sale otro número, algún script falló. Vuelve a ejecutarlos desde el 01 (borrando antes la
base, como se explica arriba) y revisa la pestaña de mensajes de SSMS buscando el error en rojo.

## 4. Ejecutar el proyecto

```bash
cd AgroDirecto/src/AgroDirecto.Web
dotnet run
```

Abre **http://localhost:5090**

> `dotnet run` desde la raíz del repositorio **no funciona**: el `.csproj` está en `src/AgroDirecto.Web`.
> En Visual Studio, abre esa carpeta (o el `.csproj`) y pulsa F5.

**No hace falta cambiar la cadena de conexión.** En `appsettings.json` está configurada como
`Server=.\SQLEXPRESS`, donde el punto significa "la instancia local de esta PC". Solo edítala si tu
instancia de SQL Server tiene otro nombre.

## 5. Entrar con las cuentas de prueba

| Perfil | Correo | Contraseña |
|---|---|---|
| Administrador | `admin@agrodirecto.com` | `Admin123` |
| Agricultor | `agricultor1@agrodirecto.com` | `Agro123` |
| Agricultor | `agricultor2@agrodirecto.com` | `Agro123` |
| Agricultor | `agricultor3@agrodirecto.com` | `Agro123` |
| Cliente | `cliente1@agrodirecto.com` | `Agro123` |

También puedes crear tu propia cuenta desde **Registrarse**, eligiendo si eres cliente o agricultor.

> Cada perfil ve una aplicación distinta. Si entras como agricultor **no verás el catálogo**: es a
> propósito, ahí están los precios de los demás agricultores.

---

# Si algo falla

| Lo que ves | Qué pasó | Cómo se arregla |
|---|---|---|
| `Cannot open database "AgroDirectoDB_V2"` | No ejecutaste los scripts, o fallaron | Ejecuta los 9 en orden (paso 3) |
| `Could not find stored procedure 'usp_...'` | Te faltó algún script | Ejecuta los 9 en orden, sin saltarte ninguno |
| `Ya hay un objeto con el nombre 'Rol'` | Ya tenías la base creada | Bórrala y vuelve a empezar (aviso del principio) |
| `Invalid column name 'CompraId'` | Tu base es de una versión anterior | Bórrala y vuelve a empezar |
| La app arranca pero no carga nada | SQL Server apagado o instancia con otro nombre | Revisa el servicio y el `Server=` de `appsettings.json` |
| `MSB3027: no se puede copiar ... .exe` | Ya tienes el proyecto corriendo | Cierra la ventana donde corre y vuelve a compilar |

Para comprobar rápido si la aplicación llega a la base, entra a **`/Conexion`**. Es una pantalla de
diagnóstico que no está en el menú y solo funciona en modo desarrollo.

---

# Estructura del repositorio

```
AgroDirecto/
├── docs/                          Documentación del proyecto
│   ├── Alcance_AgroDirecto.docx   Definición de alcance y guía de fases
│   ├── Avance_DSW1 (1).docx       Informe en curso: resumen, SEPTE y objetivos
│   ├── Informe_AgroDirecto.docx   Versión anterior del informe
│   └── er_diagram.png             Diagrama entidad-relación
├── database/                      Los 9 scripts, en orden de ejecución
│   ├── 01_CrearBaseDatos.sql  ...  09_ProcedimientosReporte.sql
│   └── AgroDirecto_BD.sql         Propuesta inicial del esquema (ver nota abajo)
└── src/
    └── AgroDirecto.Web/           Proyecto ASP.NET Core MVC
        ├── Controllers/           Un controlador por módulo
        ├── Data/                  ConexionBD y repositorios (interfaz + implementación)
        ├── Hubs/                  PedidosHub: avisos en tiempo real con SignalR
        ├── Models/                ViewModels con validaciones
        ├── Seguridad/             Cifrado de contraseñas (PBKDF2)
        ├── Views/                 Vistas Razor
        ├── wwwroot/               CSS, JavaScript e imágenes
        └── NOTAS.md               Decisiones técnicas explicadas
```

---

# Qué está construido

Las siete fases del alcance están implementadas y probadas.

| Fase | Módulo | Estado |
|---|---|---|
| 0 | Preparación del entorno | ✅ |
| 1 | Base de datos | ✅ 14 tablas + datos maestros |
| 2 | Seguridad y autenticación | ✅ Cookie + roles + PBKDF2 |
| 3 | Mantenimientos (Admin) | ✅ Categorías, unidades, distritos, usuarios |
| 4 | Gestión de productos (Agricultor) | ✅ |
| 5 | Catálogo y búsqueda con AJAX | ✅ Web API + `fetch` |
| 6 | Carrito de compras y checkout | ✅ Transaccional |
| 7 | Reportes | ✅ Con filtros y paginación |
| — | Avisos en tiempo real (SignalR) | ✅ |

## Cómo funciona una compra

El carrito puede llevar productos de **varios agricultores**, y cada uno entrega lo suyo por su
cuenta. Por eso la compra se reparte:

```
Compra  1 ── N  Pedido  1 ── N  DetallePedido
(lo que ve            (lo que ve
 el cliente)           cada agricultor)
```

El cliente ve **una sola compra**, con el detalle separado por proveedor y el total. Cada agricultor
ve **solo su pedido** y lo mueve por su cuenta:

```
Pendiente  →  Confirmado  →  Entregado
     ↘  Cancelado (devuelve el stock)
```

Con SignalR, el cliente ve cambiar el estado sin recargar, y al agricultor le aparece el pedido
nuevo apenas alguien le compra.

## Perfiles de usuario

- **Cliente:** busca en el catálogo, arma el carrito, compra y sigue sus pedidos.
- **Agricultor:** gestiona su catálogo (precio, stock) y atiende los pedidos que recibe. No ve el catálogo general.
- **Administrador:** aprueba agricultores, gestiona los datos maestros y ve los reportes.

---

# Notas para el equipo

> **Sobran dos esquemas.** `database/AgroDirecto_BD.sql` fue la propuesta inicial y plantea el mismo
> modelo pero sobre una base llamada `AgroDirectoDB`. El que usa el proyecto hoy es la serie `01_` a
> `09_`. Conviene decidir con cuál se queda el equipo y borrar el otro, para que nadie ejecute el que
> no corresponde.

> **Las contraseñas de este README son de prueba y a propósito están a la vista.** Es un proyecto
> académico que corre solo en `localhost`, sin servidor publicado ni datos reales. Si algún día se
> desplegara de verdad, habría que cambiarlas antes.
>
> Los pendientes técnicos están en [`src/AgroDirecto.Web/NOTAS.md`](src/AgroDirecto.Web/NOTAS.md).

## Fuera de alcance

- Pasarela de pago real (el pago se maneja de forma simulada, contra entrega)
- Módulo de logística/delivery — la entrega se coordina entre agricultor y cliente
- Aplicación móvil nativa
- Facturación electrónica
