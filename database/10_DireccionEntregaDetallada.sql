/* ================================================================
   AGRODIRECTO - Dirección de entrega con 3 campos
   Se ejecuta DESPUÉS de los scripts 01 al 09, sin tocarlos.

   Hoy "Dirección de entrega" es un solo campo de texto libre
   (DireccionEntrega). Se agregan dos columnas más, en el mismo
   patrón que ya usa DireccionEntrega (repetida en Compra y en
   cada Pedido):

     - DistritoId  - vinculado a la tabla Distrito que ya existe
                      (selector, no texto libre)
     - Referencia  - texto libre nuevo

   DireccionEntrega se queda igual, como el campo "Dirección".
   Ambas columnas nuevas son NULL: no rompe nada de lo que ya
   funciona ni de los datos que ya cargaste con 06_DatosPrueba.
   ================================================================ */

USE AgroDirectoDB_V2;
GO

ALTER TABLE Compra ADD
    DistritoId INT NULL,
    Referencia VARCHAR(200) NULL;
GO

ALTER TABLE Compra ADD CONSTRAINT FK_Compra_Distrito
    FOREIGN KEY (DistritoId) REFERENCES Distrito(DistritoId);
GO

ALTER TABLE Pedido ADD
    DistritoId INT NULL,
    Referencia VARCHAR(200) NULL;
GO

ALTER TABLE Pedido ADD CONSTRAINT FK_Pedido_Distrito
    FOREIGN KEY (DistritoId) REFERENCES Distrito(DistritoId);
GO


/* ================================================================
   usp_Pedido_Registrar: agrega @DistritoId y @Referencia.
   El resto del procedimiento (validación de stock, transacción,
   reparto por agricultor) queda exactamente igual.
   ================================================================ */

DROP PROCEDURE IF EXISTS usp_Pedido_Registrar;
GO

CREATE PROCEDURE usp_Pedido_Registrar
    @ClienteId        INT,
    @DireccionEntrega VARCHAR(200),
    @DistritoId       INT           = NULL,
    @Referencia       VARCHAR(200)  = NULL,
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

        INSERT INTO Compra (ClienteId, Total, DireccionEntrega, DistritoId, Referencia)
        VALUES (@ClienteId, @Total, @DireccionEntrega, @DistritoId, @Referencia);

        SET @CompraId = SCOPE_IDENTITY();

        DECLARE @Reparto TABLE (PedidoId INT, AgricultorId INT);

        INSERT INTO Pedido (CompraId, ClienteId, AgricultorId, EstadoPedidoId, Total,
                             DireccionEntrega, DistritoId, Referencia)
        OUTPUT INSERTED.PedidoId, INSERTED.AgricultorId INTO @Reparto (PedidoId, AgricultorId)
        SELECT @CompraId, @ClienteId, pr.AgricultorId, @Pendiente,
               SUM(dc.Cantidad * dc.PrecioUnitario), @DireccionEntrega, @DistritoId, @Referencia
        FROM DetalleCarrito dc
        INNER JOIN Producto pr ON pr.ProductoId = dc.ProductoId
        WHERE dc.CarritoId = @CarritoId
        GROUP BY pr.AgricultorId;

        INSERT INTO DetallePedido (PedidoId, ProductoId, Cantidad, PrecioUnitario, Subtotal)
        SELECT r.PedidoId, dc.ProductoId, dc.Cantidad, dc.PrecioUnitario,
               dc.Cantidad * dc.PrecioUnitario
        FROM DetalleCarrito dc
        INNER JOIN Producto pr ON pr.ProductoId = dc.ProductoId
        INNER JOIN @Reparto r  ON r.AgricultorId = pr.AgricultorId
        WHERE dc.CarritoId = @CarritoId;

        UPDATE p
        SET p.Stock = p.Stock - dc.Cantidad
        FROM Producto p
        INNER JOIN DetalleCarrito dc ON dc.ProductoId = p.ProductoId
        WHERE dc.CarritoId = @CarritoId;

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


/* ================================================================
   Lectura: se agrega el nombre del distrito y la referencia a las
   3 consultas que ya devolvían DireccionEntrega.
   ================================================================ */

DROP PROCEDURE IF EXISTS usp_Compra_ListarPorCliente;
GO

CREATE PROCEDURE usp_Compra_ListarPorCliente
    @ClienteId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT c.CompraId, c.FechaCompra, c.Total, c.DireccionEntrega,
           ISNULL(d.Nombre, '') AS Distrito, c.Referencia,
           (SELECT COUNT(*) FROM Pedido p WHERE p.CompraId = c.CompraId) AS Proveedores,
           (SELECT COUNT(*) FROM DetallePedido dd
            INNER JOIN Pedido p2 ON p2.PedidoId = dd.PedidoId
            WHERE p2.CompraId = c.CompraId) AS Items,
           ISNULL((SELECT TOP 1 e.Nombre
                   FROM Pedido p3
                   INNER JOIN EstadoPedido e ON e.EstadoPedidoId = p3.EstadoPedidoId
                   WHERE p3.CompraId = c.CompraId AND e.Nombre <> 'Cancelado'
                   ORDER BY p3.EstadoPedidoId), 'Cancelado') AS Estado
    FROM Compra c
    LEFT JOIN Distrito d ON d.DistritoId = c.DistritoId
    WHERE c.ClienteId = @ClienteId
    ORDER BY c.CompraId DESC;
END
GO

DROP PROCEDURE IF EXISTS usp_Compra_ObtenerDetalle;
GO

CREATE PROCEDURE usp_Compra_ObtenerDetalle
    @CompraId  INT,
    @ClienteId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT c.CompraId, c.FechaCompra, c.Total AS TotalCompra, c.DireccionEntrega,
           ISNULL(dc.Nombre, '') AS Distrito, c.Referencia,
           ped.PedidoId, ped.AgricultorId, ped.Total AS TotalPedido,
           e.Nombre AS Estado,
           ISNULL(a.NombreComercial, ua.Nombres + ' ' + ua.Apellidos) AS Agricultor,
           ISNULL(d.Nombre, '') AS DistritoAgricultor,
           ua.Telefono AS TelefonoAgricultor,
           dp.ProductoId, pr.Nombre AS Producto, pr.ImagenUrl,
           dp.Cantidad, um.Abreviatura AS Unidad, dp.PrecioUnitario, dp.Subtotal
    FROM Compra c
    LEFT  JOIN Distrito      dc  ON dc.DistritoId = c.DistritoId
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

DROP PROCEDURE IF EXISTS usp_Pedido_ListarPorAgricultor;
GO

CREATE PROCEDURE usp_Pedido_ListarPorAgricultor
    @AgricultorId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.PedidoId, p.CompraId, p.FechaPedido, p.Total, p.DireccionEntrega,
           ISNULL(d.Nombre, '') AS Distrito, p.Referencia,
           e.Nombre AS Estado,
           us.Nombres + ' ' + us.Apellidos AS Cliente,
           us.Telefono AS TelefonoCliente,
           (SELECT COUNT(*) FROM DetallePedido dd WHERE dd.PedidoId = p.PedidoId) AS Items
    FROM Pedido p
    LEFT  JOIN Distrito      d  ON d.DistritoId = p.DistritoId
    INNER JOIN EstadoPedido e  ON e.EstadoPedidoId = p.EstadoPedidoId
    INNER JOIN Cliente      c  ON c.ClienteId = p.ClienteId
    INNER JOIN Usuario      us ON us.UsuarioId = c.UsuarioId
    WHERE p.AgricultorId = @AgricultorId
    ORDER BY p.PedidoId DESC;
END
GO

PRINT 'Dirección de entrega actualizada: Distrito + Dirección + Referencia.';
GO
