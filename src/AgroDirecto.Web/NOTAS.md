# Estado del proyecto

Basado en la guía de fases de `docs/Alcance_AgroDirecto.docx`, sección 8.

## Cómo levantarlo

1. Ejecutar en SSMS, **en orden**, los nueve scripts de `database/`:
   `01_CrearBaseDatos` → `02_ProcedimientosAlmacenados` → `03_ProcedimientosSeguridad` →
   `04_DatosIniciales` → `05_ProcedimientosProducto` → `06_DatosPrueba` →
   `07_ProcedimientosUsuario` → `08_ProcedimientosCarrito` → `09_ProcedimientosReporte`
2. Ajustar el `Server=` de `appsettings.json` si tu instancia no se llama `SQLEXPRESS`.
3. Desde esta carpeta: `dotnet run` → http://localhost:5090

Cuenta de administrador: **admin@agrodirecto.com / Admin123** (cambiarla antes de la entrega).
Los datos de prueba (`06`) traen 3 agricultores y 1 cliente, todos con contraseña **Agro123**.
Cliente y Agricultor también se pueden crear desde el formulario de registro.

## Avance por fases

| Fase | Estado |
|---|---|
| 0 — Preparación del entorno | ✅ Hecho |
| 1 — Base de datos | ✅ Hecho (13 tablas + datos maestros) |
| 2 — Seguridad y autenticación | ✅ Hecho |
| 3 — Mantenimientos (Admin) | ✅ Hecho (Categorías, UnidadMedida, Distrito y Usuarios) |
| 4 — Productos (Agricultor) | ✅ Hecho |
| 5 — Catálogo con AJAX | ✅ Hecho |
| 6 — Carrito y checkout | ✅ Hecho |
| 7 — Reportes | ✅ Hecho |

Las siete fases del alcance están implementadas y probadas de extremo a extremo.

## Decisiones tomadas

**Framework: .NET 8 (ASP.NET Core MVC), no .NET Framework.** El documento de alcance decía
.NET Framework, pero el curso se dicta sobre .NET 8 y el ejemplo del profesor (TiendaWeb)
usa esa versión. El plan es una guía, se ajusta a lo que pide el curso.

**Acceso a datos: ADO.NET, no Entity Framework.** Igual que TiendaWeb: `ConexionBD` +
interfaz `IXxxRepositorio` + implementación con `SqlConnection` / `SqlCommand` /
`SqlDataReader`, todo registrado por inyección de dependencias en `Program.cs`.

**Los mantenimientos van por procedimientos almacenados**, como exige la sección 7 del
alcance. Es la única diferencia con TiendaWeb, que usa SQL escrito dentro del repositorio.

**Autenticación por cookie** (`AddAuthentication().AddCookie()`), con `[Authorize(Roles = "...")]`
en los controladores. El profesor no ha dictado autenticación, así que aquí se sale de lo
visto en clase; es la forma estándar en ASP.NET Core y no requiere paquetes extra.

**Contraseñas con PBKDF2** (`Seguridad/Password.cs`): 100000 iteraciones, sal aleatoria por
usuario, comparación en tiempo constante. Viene incluido en .NET, así que el proyecto sigue
con **un solo paquete NuGet** (`Microsoft.Data.SqlClient`).

**El registro crea usuario y perfil en una transacción**, dentro de `usp_Usuario_Registrar`.
Si falla la creación del perfil no queda un usuario huérfano.

**Nombre de la base: `AgroDirectoDB_V2`.** El nombre `AgroDirectoDB` está ocupado por una
versión anterior del proyecto. Si el equipo se queda solo con esta, se renombra en
`01_CrearBaseDatos.sql` y en `appsettings.json`.

**Prefijo `usp_` en los procedimientos**, no `sp_`. SQL Server resuelve los nombres que
empiezan en `sp_` contra la base `master` primero, lo que provoca errores confusos si un
script se ejecuta sin haber hecho `USE` de la base correcta.

**El `AgricultorId` nunca viene del formulario.** Se saca de la cookie de sesión
(`ProductoController.AgricultorActual()`). Si viniera del formulario, cualquiera podría
publicar o editar productos a nombre de otro agricultor cambiando un campo oculto.

**El checkout es una transacción** (`usp_Pedido_Registrar`): valida el stock de todo el
carrito *antes* de tocar nada, y recién después crea la cabecera, el detalle con el precio
congelado, descuenta el stock y cierra el carrito. Con `SET XACT_ABORT ON` y
`XACT_STATE()` en el `CATCH`, para que un fallo a medias no deje un pedido incompleto.

**Una compra se reparte en un pedido por agricultor.**

```
Compra  1 ── N  Pedido  1 ── N  DetallePedido
(lo que ve            (lo que ve
 el cliente)           cada agricultor)
```

El carrito puede llevar productos de varios agricultores, y cada uno prepara y entrega lo
suyo por su cuenta. Con un solo estado por compra, el primero en marcar "Entregado" decidía
por los demás. Ahora cada agricultor maneja el suyo, y el cliente sigue viendo **una compra**
con el detalle separado por proveedor y su total.

El estado general de la compra es el del pedido **menos avanzado**, sin contar los
cancelados: si uno ya entregó y el otro sigue pendiente, la compra está pendiente.

**El agricultor mueve su pedido: Pendiente → Confirmado → Entregado.** Puede cancelar
mientras no lo haya entregado. Las transiciones se validan en `usp_Pedido_CambiarEstado`,
no en la vista: esconder un botón no es una regla, solo es no mostrarla.

**Cancelar devuelve el stock.** El checkout lo descontó al comprar; si la venta no se
concreta, ese stock tiene que volver a estar disponible. Por eso ese procedimiento también
es transaccional.

Los reportes ya excluían los pedidos cancelados (`WHERE e.Nombre <> 'Cancelado'`), así que
siguen cuadrando. Ojo con una cosa: ahora "Pedidos" cuenta pedidos por agricultor, no
compras. Una compra a dos agricultores cuenta como dos pedidos.

**El agricultor necesita aprobación del administrador para publicar.** Se comprueba en
`usp_Producto_Insertar`, y el catálogo filtra por `Aprobado = 1`: si le retiran la
aprobación, sus productos desaparecen de la vista pública sin tener que borrarlos.

**El catálogo NO es público: solo entran Cliente y Administrador.** Es una decisión de
negocio: el catálogo es la lista de precios de todos los agricultores, y si un agricultor
la ve puede bajar los suyos para quitarle la venta al vecino. El agricultor solo ve y
gestiona sus propios productos.

El `[Authorize]` va en **dos** sitios y ninguno sobra: `CatalogoController` (la página) y
`ProductosApiController` (el JSON). Proteger solo la página no serviría de nada, porque
cualquiera entraría directo a `/api/productosapi` y leería los precios ahí.

Consecuencia asumida: **un visitante sin cuenta ya no puede mirar la tienda**, tiene que
registrarse primero. Y hay un límite que el software no puede cubrir: un agricultor puede
crearse una cuenta de Cliente con otro correo y ver todo igual.

**Avisos en tiempo real con SignalR** (`Hubs/PedidosHub.cs`). Dos casos: al cliente le
cambia el estado de su pedido sin recargar, y al agricultor le aparece el pedido nuevo.

Lo importante son los **grupos**. Aquí viajan pedidos y precios, así que nadie recibe lo de
otro: al conectarse, cada usuario entra a `cliente-{id}` o `agricultor-{id}`. El grupo lo
calcula el Hub leyendo la cookie de sesión, **nunca lo manda el navegador**: si el navegador
pudiera pedir "méteme en agricultor-6", cualquiera leería los pedidos de la competencia.

El aviso se envía **después** de guardar en la base, y no es la fuente de la verdad: si el
usuario tiene la página cerrada o se le cae la conexión, al recargar ve el estado correcto
porque sale de SQL. Si SignalR falla, la aplicación funciona igual que antes.

SignalR viene incluido en ASP.NET Core, así que el proyecto **sigue con un solo paquete
NuGet**. La librería de JavaScript está descargada en `wwwroot/js/signalr.min.js` en vez de
traerse de un CDN, para que funcione sin internet.

**Las fotos de producto son por URL**, no subida de archivos: el campo `ImagenUrl` guarda una
dirección, y sirve tanto una externa (`https://...`) como una del propio proyecto
(`/img/productos/...`). Si la dirección no carga, el catálogo muestra el marcador en lugar
del ícono roto. Subir archivos al servidor no lo pide el alcance.

**Las fotos de los datos de prueba están dentro del proyecto**, en `wwwroot/img/productos/`,
no enlazadas a webs ajenas. Así el catálogo se ve igual en cualquier máquina, funciona sin
internet durante la sustentación y no se rompe si el sitio de origen borra el archivo. Son
de Wikimedia Commons; el origen de cada una está en `CREDITOS.md` de esa carpeta.

## Cómo agregar un mantenimiento nuevo

Categorías es el molde. Para cualquier otro se repite la misma estructura:

1. Los 5 procedimientos en `database/02_ProcedimientosAlmacenados.sql`
2. `Models/XxxViewModel.cs`
3. `Data/IXxxRepositorio.cs` y `Data/XxxRepositorio.cs`
4. Registrar el repositorio en `Program.cs`
5. `Controllers/XxxController.cs` con `[Authorize(Roles = "Administrador")]`
6. `Views/Xxx/` con Index, Registrar, Editar y Detalle
7. Agregar el enlace en `Views/Shared/_Layout.cshtml`

## Pendiente antes de la entrega

**Lo más importante: el informe no describe lo que se construyó.**
`docs/Informe_AgroDirecto.docx` no menciona ADO.NET, .NET 8, los procedimientos almacenados,
la autenticación por cookie ni el modelo Compra/Pedido. Describe el plan, no el producto.

**Limpieza técnica**

- El checkout no vuelve a comprobar que el agricultor siga aprobado: si a alguien le retiran
  la aprobación mientras un cliente ya tenía su producto en el carrito, la compra pasa igual.
- El cliente no puede cancelar su propia compra; solo el agricultor cancela. Falta decidir
  si eso es lo que se quiere.
- El logo y los iconos de `wwwroot/img/` pesan ~12 MB: son de 1024–1536 px y se muestran a
  36–72 px. Reducirlos a 256 px dejaría el total en ~400 KB. (Las fotos de producto ya están
  a 400 px y pesan 1,1 MB entre las 17.)

**Decisiones que le tocan al equipo, no al código**

- Hay dos proyectos AgroDirecto distintos (este y el avance del compañero, con EF Core).
  Falta definir cuál es el oficial.
- En `database/` conviven dos esquemas: `AgroDirecto_BD.sql` (el original del repositorio)
  y `01_CrearBaseDatos.sql` (el que usa esta aplicación). Sobra uno.
- La carátula del informe tiene el nombre del curso equivocado.
