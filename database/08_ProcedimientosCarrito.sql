/* ================================================================
   AGRODIRECTO - 08. Carrito y checkout (Fase 6)
   Ejecutar después de 07_ProcedimientosUsuario.sql.

   Es el núcleo del sistema. El checkout (usp_Pedido_Registrar) hace
   dentro de UNA transacción: registrar el pedido, copiar el detalle,
   descontar el stock y vaciar el carrito. Si algo falla, no queda
   nada a medias.
   ================================================================ */

USE AgroDirectoDB_V2;
GO

IF DB_NAME() <> N'AgroDirectoDB_V2'
BEGIN
    RAISERROR(N'ERROR: no se pudo cambiar a AgroDirectoDB_V2. Ejecuta primero 01_CrearBaseDatos.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO


/* ---------------- Perfil del cliente ---------------- */

DROP PROCEDURE IF EXISTS usp_Cliente_ObtenerPorUsuario;
GO

CREATE PROCEDURE usp_Cliente_ObtenerPorUsuario
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ClienteId, UsuarioId, DistritoId, ISNULL(Direccion, '') AS Direccion
    FROM Cliente
    WHERE UsuarioId = @UsuarioId;
END
GO


/* ---------------- Carrito activo ----------------
   Cada cliente tiene como mucho un carrito ACTIVO. Si no existe, se
   crea al vuelo, así el resto del código nunca tiene que comprobarlo. */

DROP PROCEDURE IF EXISTS usp_Carrito_ObtenerOCrear;
GO

CREATE PROCEDURE usp_Carrito_ObtenerOCrear
    @ClienteId INT,
    @CarritoId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 @CarritoId = CarritoId
    FROM Carrito
    WHERE ClienteId = @ClienteId AND Estado = 'ACTIVO'
    ORDER BY CarritoId DESC;

    IF @CarritoId IS NULL
    BEGIN
        INSERT INTO Carrito (ClienteId) VALUES (@ClienteId);
        SET @CarritoId = SCOPE_IDENTITY();
    END
END
GO


DROP PROCEDURE IF EXISTS usp_Carrito_Listar;
GO

CREATE PROCEDURE usp_Carrito_Listar
    @CarritoId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT dc.DetalleCarritoId, dc.ProductoId, dc.Cantidad, dc.PrecioUnitario,
           (dc.Cantidad * dc.PrecioUnitario) AS Subtotal,
           p.Nombre, p.Stock, p.ImagenUrl,
           u.Abreviatura AS Unidad,
           ISNULL(a.NombreComercial, us.Nombres + ' ' + us.Apellidos) AS Agricultor
    FROM DetalleCarrito dc
    INNER JOIN Producto     p  ON p.ProductoId = dc.ProductoId
    INNER JOIN UnidadMedida u  ON u.UnidadMedidaId = p.UnidadMedidaId
    INNER JOIN Agricultor   a  ON a.AgricultorId = p.AgricultorId
    INNER JOIN Usuario      us ON us.UsuarioId = a.UsuarioId
    WHERE dc.CarritoId = @CarritoId
    ORDER BY dc.DetalleCarritoId;
END
GO


/* ---------------- Agregar al carrito ----------------
   Si el producto ya está, suma la cantidad en lugar de duplicar la línea. */

DROP PROCEDURE IF EXISTS usp_Carrito_Agregar;
GO

CREATE PROCEDURE usp_Carrito_Agregar
    @CarritoId  INT,
    @ProductoId INT,
    @Cantidad   DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Cantidad <= 0
    BEGIN
        RAISERROR(N'La cantidad debe ser mayor que cero.', 16, 1);
        RETURN;
    END

    DECLARE @Stock  DECIMAL(10,2), @Precio DECIMAL(10,2), @Activo BIT, @Nombre VARCHAR(120);

    SELECT @Stock = Stock, @Precio = Precio, @Activo = Estado, @Nombre = Nombre
    FROM Producto WHERE ProductoId = @ProductoId;

    IF @Nombre IS NULL OR @Activo = 0
    BEGIN
        RAISERROR(N'El producto ya no está disponible.', 16, 1);
        RETURN;
    END

    DECLARE @YaTiene DECIMAL(10,2) =
        ISNULL((SELECT Cantidad FROM DetalleCarrito
                WHERE CarritoId = @CarritoId AND ProductoId = @ProductoId), 0);

    IF (@YaTiene + @Cantidad) > @Stock
    BEGIN
        DECLARE @m1 NVARCHAR(300) =
            N'Solo quedan ' + CAST(@Stock AS NVARCHAR(20)) + N' de "' + @Nombre + N'".';
        RAISERROR(@m1, 16, 1);
        RETURN;
    END

    IF @YaTiene > 0
        UPDATE DetalleCarrito
        SET Cantidad = @YaTiene + @Cantidad
        WHERE CarritoId = @CarritoId AND ProductoId = @ProductoId;
    ELSE
        INSERT INTO DetalleCarrito (CarritoId, ProductoId, Cantidad, PrecioUnitario)
        VALUES (@CarritoId, @ProductoId, @Cantidad, @Precio);
END
GO


DROP PROCEDURE IF EXISTS usp_Carrito_ActualizarCantidad;
GO

CREATE PROCEDURE usp_Carrito_ActualizarCantidad
    @DetalleCarritoId INT,
    @CarritoId        INT,
    @Cantidad         DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProductoId INT, @Stock DECIMAL(10,2), @Nombre VARCHAR(120), @Precio DECIMAL(10,2), @MontoMinimo DECIMAL(10,2);

    SELECT @ProductoId = dc.ProductoId, @Stock = p.Stock, @Nombre = p.Nombre, @Precio = p.Precio, @MontoMinimo = p.MontoMinimo
    FROM DetalleCarrito dc
    INNER JOIN Producto p ON p.ProductoId = dc.ProductoId
    WHERE dc.DetalleCarritoId = @DetalleCarritoId AND dc.CarritoId = @CarritoId;

    IF @ProductoId IS NULL
    BEGIN
        RAISERROR(N'Ese producto no está en tu carrito.', 16, 1);
        RETURN;
    END

    IF @Cantidad <= 0
    BEGIN
        DELETE FROM DetalleCarrito WHERE DetalleCarritoId = @DetalleCarritoId;
        RETURN;
    END

    IF @Cantidad > @Stock
    BEGIN
        DECLARE @m2 NVARCHAR(300) =
            N'Solo quedan ' + CAST(@Stock AS NVARCHAR(20)) + N' de "' + @Nombre + N'".';
        RAISERROR(@m2, 16, 1);
        RETURN;
    END

    IF (@Cantidad * @Precio) < @MontoMinimo
    BEGIN
        DECLARE @m3 NVARCHAR(300) =
            N'El subtotal para "' + @Nombre + N'" debe ser de al menos S/ ' + CAST(@MontoMinimo AS NVARCHAR(20)) + N'.';
        RAISERROR(@m3, 16, 1);
        RETURN;
    END

    UPDATE DetalleCarrito SET Cantidad = @Cantidad WHERE DetalleCarritoId = @DetalleCarritoId;
END
GO

DROP PROCEDURE IF EXISTS usp_Carrito_Quitar;
GO

CREATE PROCEDURE usp_Carrito_Quitar
    @DetalleCarritoId INT,
    @CarritoId        INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM DetalleCarrito
    WHERE DetalleCarritoId = @DetalleCarritoId AND CarritoId = @CarritoId;
END
GO


/* ================================================================
   CHECKOUT — el procedimiento más importante del sistema.
   Todo ocurre dentro de una transacción: o se completa entero, o no
   se hace nada. Sin esto podría quedar un pedido registrado con el
   stock sin descontar, o al revés.
   ================================================================ */

DROP PROCEDURE IF EXISTS usp_Pedido_Registrar;
GO

/* Registra la compra completa. El carrito puede traer productos de varios
   agricultores, así que se crea UNA Compra y dentro un Pedido por cada
   agricultor: cada uno prepara y entrega lo suyo, y maneja su propio estado.
   Todo ocurre dentro de una sola transacción: o se registra la compra
   entera o no se registra nada. */
CREATE PROCEDURE usp_Pedido_Registrar
    @ClienteId        INT,
    @DireccionEntrega VARCHAR(200),
    @CompraId         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CarritoId INT =
        (SELECT TOP 1 CarritoId FROM Carrito
         WHERE ClienteId = @ClienteId AND Estado = 'ACTIVO' ORDER BY CarritoId DESC);

    IF @CarritoId IS NULL OR NOT EXISTS (SELECT 1 FROM DetalleCarrito WHERE CarritoId = @CarritoId)
    BEGIN
        RAISERROR(N'Tu carrito está vacío.', 16, 1);
        RETURN;
    END

    -- 1) Comprobar el stock de TODO antes de tocar nada. Alguien pudo
    --    haber comprado mientras el carrito estaba abierto.
    DECLARE @Falta VARCHAR(120) =
        (SELECT TOP 1 p.Nombre
         FROM DetalleCarrito dc
         INNER JOIN Producto p ON p.ProductoId = dc.ProductoId
         WHERE dc.CarritoId = @CarritoId
           AND (p.Stock < dc.Cantidad OR p.Estado = 0));

    IF @Falta IS NOT NULL
    BEGIN
        DECLARE @m NVARCHAR(300) =
            N'Ya no hay stock suficiente de "' + @Falta + N'". Ajusta tu carrito.';
        RAISERROR(@m, 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Pendiente INT = (SELECT EstadoPedidoId FROM EstadoPedido WHERE Nombre = 'Pendiente');
        DECLARE @Total DECIMAL(12,2) =
            (SELECT SUM(Cantidad * PrecioUnitario) FROM DetalleCarrito WHERE CarritoId = @CarritoId);

        -- 2) Cabecera de la compra: lo que el cliente entiende por "mi pedido"
        INSERT INTO Compra (ClienteId, Total, DireccionEntrega)
        VALUES (@ClienteId, @Total, @DireccionEntrega);

        SET @CompraId = SCOPE_IDENTITY();

        -- 3) Un pedido por agricultor, con el subtotal que le corresponde.
        --    OUTPUT guarda qué PedidoId le tocó a cada agricultor, para poder
        --    repartir el detalle en el paso siguiente.
        DECLARE @Reparto TABLE (PedidoId INT, AgricultorId INT);

        INSERT INTO Pedido (CompraId, ClienteId, AgricultorId, EstadoPedidoId, Total, DireccionEntrega)
        OUTPUT INSERTED.PedidoId, INSERTED.AgricultorId INTO @Reparto (PedidoId, AgricultorId)
        SELECT @CompraId, @ClienteId, pr.AgricultorId, @Pendiente,
               SUM(dc.Cantidad * dc.PrecioUnitario), @DireccionEntrega
        FROM DetalleCarrito dc
        INNER JOIN Producto pr ON pr.ProductoId = dc.ProductoId
        WHERE dc.CarritoId = @CarritoId
        GROUP BY pr.AgricultorId;

        -- 4) Detalle, congelando el precio del momento de la compra. Cada
        --    línea cae en el pedido del agricultor dueño del producto.
        INSERT INTO DetallePedido (PedidoId, ProductoId, Cantidad, PrecioUnitario, Subtotal)
        SELECT r.PedidoId, dc.ProductoId, dc.Cantidad, dc.PrecioUnitario,
               dc.Cantidad * dc.PrecioUnitario
        FROM DetalleCarrito dc
        INNER JOIN Producto pr ON pr.ProductoId = dc.ProductoId
        INNER JOIN @Reparto r  ON r.AgricultorId = pr.AgricultorId
        WHERE dc.CarritoId = @CarritoId;

        -- 5) Descontar el stock
        UPDATE p
        SET p.Stock = p.Stock - dc.Cantidad
        FROM Producto p
        INNER JOIN DetalleCarrito dc ON dc.ProductoId = p.ProductoId
        WHERE dc.CarritoId = @CarritoId;

        -- 6) Cerrar el carrito
        DELETE FROM DetalleCarrito WHERE CarritoId = @CarritoId;
        UPDATE Carrito SET Estado = 'PROCESADO' WHERE CarritoId = @CarritoId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO


/* ---------------- Compras (lo que ve el cliente) ---------------- */

/* Quedó de cuando un pedido era una compra entera. Lo reemplaza
   usp_Compra_ListarPorCliente. Se borra por si alguna base lo conserva. */
DROP PROCEDURE IF EXISTS usp_Pedido_ListarPorCliente;
GO

DROP PROCEDURE IF EXISTS usp_Compra_ListarPorCliente;
GO

CREATE PROCEDURE usp_Compra_ListarPorCliente
    @ClienteId INT
AS
BEGIN
    SET NOCOUNT ON;

    /* El "estado general" de una compra es el del pedido menos avanzado,
       sin contar los cancelados: si un agricultor ya entregó pero el otro
       sigue pendiente, la compra está pendiente. Si TODOS los pedidos
       están cancelados, la compra está cancelada. */
    SELECT c.CompraId, c.FechaCompra, c.Total, c.DireccionEntrega,
           (SELECT COUNT(*) FROM Pedido p WHERE p.CompraId = c.CompraId) AS Proveedores,
           (SELECT COUNT(*) FROM DetallePedido d
            INNER JOIN Pedido p2 ON p2.PedidoId = d.PedidoId
            WHERE p2.CompraId = c.CompraId) AS Items,
           ISNULL((SELECT TOP 1 e.Nombre
                   FROM Pedido p3
                   INNER JOIN EstadoPedido e ON e.EstadoPedidoId = p3.EstadoPedidoId
                   WHERE p3.CompraId = c.CompraId AND e.Nombre <> 'Cancelado'
                   ORDER BY p3.EstadoPedidoId), 'Cancelado') AS Estado
    FROM Compra c
    WHERE c.ClienteId = @ClienteId
    ORDER BY c.CompraId DESC;
END
GO

DROP PROCEDURE IF EXISTS usp_Compra_ObtenerDetalle;
GO

/* Devuelve una fila por producto, arrastrando a qué pedido y a qué
   agricultor pertenece. La aplicación las agrupa por proveedor. */
CREATE PROCEDURE usp_Compra_ObtenerDetalle
    @CompraId  INT,
    @ClienteId INT = NULL      -- si viene, comprueba que la compra es suya
AS
BEGIN
    SET NOCOUNT ON;

    /* Si la compra no es de este cliente no se lanza error: se devuelven
       cero filas y para él simplemente no existe. Así el controlador
       responde 404 sin tener que atrapar una excepción. */

    SELECT c.CompraId, c.FechaCompra, c.Total AS TotalCompra, c.DireccionEntrega,
           ped.PedidoId, ped.AgricultorId, ped.Total AS TotalPedido,
           e.Nombre AS Estado,
           ISNULL(a.NombreComercial, ua.Nombres + ' ' + ua.Apellidos) AS Agricultor,
           ISNULL(d.Nombre, '') AS DistritoAgricultor,
           ua.Telefono AS TelefonoAgricultor,
           dp.ProductoId, pr.Nombre AS Producto, pr.ImagenUrl,
           dp.Cantidad, um.Abreviatura AS Unidad, dp.PrecioUnitario, dp.Subtotal
    FROM Compra c
    INNER JOIN Pedido        ped ON ped.CompraId = c.CompraId
    INNER JOIN EstadoPedido  e   ON e.EstadoPedidoId = ped.EstadoPedidoId
    INNER JOIN Agricultor    a   ON a.AgricultorId = ped.AgricultorId
    INNER JOIN Usuario       ua  ON ua.UsuarioId = a.UsuarioId
    LEFT  JOIN Distrito      d   ON d.DistritoId = a.DistritoId
    INNER JOIN DetallePedido dp  ON dp.PedidoId = ped.PedidoId
    INNER JOIN Producto      pr  ON pr.ProductoId = dp.ProductoId
    INNER JOIN UnidadMedida  um  ON um.UnidadMedidaId = pr.UnidadMedidaId
    WHERE c.CompraId = @CompraId
      AND (@ClienteId IS NULL OR c.ClienteId = @ClienteId)
    ORDER BY ped.PedidoId, dp.DetallePedidoId;
END
GO


/* ---------------- Pedidos (lo que ve el agricultor) ---------------- */

DROP PROCEDURE IF EXISTS usp_Pedido_ListarPorAgricultor;
GO

CREATE PROCEDURE usp_Pedido_ListarPorAgricultor
    @AgricultorId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.PedidoId, p.CompraId, p.FechaPedido, p.Total, p.DireccionEntrega,
           e.Nombre AS Estado,
           us.Nombres + ' ' + us.Apellidos AS Cliente,
           us.Telefono AS TelefonoCliente,
           (SELECT COUNT(*) FROM DetallePedido d WHERE d.PedidoId = p.PedidoId) AS Items
    FROM Pedido p
    INNER JOIN EstadoPedido e  ON e.EstadoPedidoId = p.EstadoPedidoId
    INNER JOIN Cliente      c  ON c.ClienteId = p.ClienteId
    INNER JOIN Usuario      us ON us.UsuarioId = c.UsuarioId
    WHERE p.AgricultorId = @AgricultorId
    ORDER BY p.PedidoId DESC;
END
GO

DROP PROCEDURE IF EXISTS usp_Pedido_ObtenerDetalle;
GO

CREATE PROCEDURE usp_Pedido_ObtenerDetalle
    @PedidoId     INT,
    @AgricultorId INT = NULL    -- si viene, comprueba que el pedido es suyo
AS
BEGIN
    SET NOCOUNT ON;

    -- Mismo criterio que en la compra: si no es suyo, cero filas.
    SELECT dp.ProductoId, dp.Cantidad, dp.PrecioUnitario, dp.Subtotal,
           pr.Nombre AS Producto, pr.ImagenUrl, um.Abreviatura AS Unidad
    FROM DetallePedido dp
    INNER JOIN Pedido       p  ON p.PedidoId = dp.PedidoId
    INNER JOIN Producto     pr ON pr.ProductoId = dp.ProductoId
    INNER JOIN UnidadMedida um ON um.UnidadMedidaId = pr.UnidadMedidaId
    WHERE dp.PedidoId = @PedidoId
      AND (@AgricultorId IS NULL OR p.AgricultorId = @AgricultorId)
    ORDER BY dp.DetallePedidoId;
END
GO

DROP PROCEDURE IF EXISTS usp_Pedido_CambiarEstado;
GO

/* El agricultor mueve su pedido: Pendiente -> Confirmado -> Entregado,
   y puede cancelarlo mientras no lo haya entregado.

   Cancelar DEVUELVE el stock: el checkout lo descontó al comprar, y si la
   venta no se concreta ese stock tiene que volver a estar disponible.
   Por eso el procedimiento es transaccional. */
CREATE PROCEDURE usp_Pedido_CambiarEstado
    @PedidoId     INT,
    @AgricultorId INT,           -- viene de la sesión, no del formulario
    @NuevoEstado  VARCHAR(20),
    -- Con estos dos la aplicación sabe a quién avisar por SignalR
    @ClienteId    INT = NULL OUTPUT,
    @CompraId     INT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SELECT @ClienteId = ClienteId, @CompraId = CompraId
    FROM Pedido WHERE PedidoId = @PedidoId AND AgricultorId = @AgricultorId;

    DECLARE @EstadoActual VARCHAR(20) =
        (SELECT e.Nombre
         FROM Pedido p
         INNER JOIN EstadoPedido e ON e.EstadoPedidoId = p.EstadoPedidoId
         WHERE p.PedidoId = @PedidoId AND p.AgricultorId = @AgricultorId);

    IF @EstadoActual IS NULL
    BEGIN
        RAISERROR(N'El pedido no existe o no te pertenece.', 16, 1);
        RETURN;
    END

    DECLARE @IdNuevo INT = (SELECT EstadoPedidoId FROM EstadoPedido WHERE Nombre = @NuevoEstado);

    IF @IdNuevo IS NULL
    BEGIN
        RAISERROR(N'Ese estado no existe.', 16, 1);
        RETURN;
    END

    -- Transiciones permitidas. Entregado y Cancelado son finales.
    DECLARE @Permitida BIT = 0;
    IF @EstadoActual = 'Pendiente'  AND @NuevoEstado IN ('Confirmado', 'Cancelado') SET @Permitida = 1;
    IF @EstadoActual = 'Confirmado' AND @NuevoEstado IN ('Entregado',  'Cancelado') SET @Permitida = 1;

    IF @Permitida = 0
    BEGIN
        DECLARE @m NVARCHAR(300) =
            N'No se puede pasar de "' + @EstadoActual + N'" a "' + @NuevoEstado + N'".';
        RAISERROR(@m, 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE Pedido SET EstadoPedidoId = @IdNuevo WHERE PedidoId = @PedidoId;

        IF @NuevoEstado = 'Cancelado'
        BEGIN
            UPDATE pr
            SET pr.Stock = pr.Stock + dp.Cantidad
            FROM Producto pr
            INNER JOIN DetallePedido dp ON dp.ProductoId = pr.ProductoId
            WHERE dp.PedidoId = @PedidoId;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO


SET NOEXEC OFF;
GO

PRINT 'Procedimientos de Carrito y Pedido creados.';
GO
