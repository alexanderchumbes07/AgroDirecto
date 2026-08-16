/* ================================================================
   AGRODIRECTO - 09. Reportes (Fase 7)
   Ejecutar después de 08_ProcedimientosCarrito.sql.

   Los tres reportes que pide el alcance: ventas por periodo,
   productos más vendidos y ventas por agricultor. Todos con filtros
   de fecha; el primero además con paginación.

   Los pedidos cancelados no cuentan como venta.
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


/* ---------------- Ventas por periodo (detalle paginado) ---------------- */

DROP PROCEDURE IF EXISTS usp_Reporte_Ventas;
GO

CREATE PROCEDURE usp_Reporte_Ventas
    @FechaInicio  DATE = NULL,
    @FechaFin     DATE = NULL,
    @AgricultorId INT  = NULL,
    @Pagina       INT  = 1,
    @Tamano       INT  = 10,
    @Total        INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @Total = COUNT(*)
    FROM DetallePedido dp
    INNER JOIN Pedido       p  ON p.PedidoId = dp.PedidoId
    INNER JOIN Producto     pr ON pr.ProductoId = dp.ProductoId
    INNER JOIN EstadoPedido e  ON e.EstadoPedidoId = p.EstadoPedidoId
    WHERE e.Nombre <> 'Cancelado'
      AND (@FechaInicio  IS NULL OR CAST(p.FechaPedido AS DATE) >= @FechaInicio)
      AND (@FechaFin     IS NULL OR CAST(p.FechaPedido AS DATE) <= @FechaFin)
      AND (@AgricultorId IS NULL OR pr.AgricultorId = @AgricultorId);

    SELECT p.PedidoId, p.FechaPedido, e.Nombre AS Estado,
           pr.Nombre AS Producto, c.Nombre AS Categoria,
           dp.Cantidad, um.Abreviatura AS Unidad,
           dp.PrecioUnitario, dp.Subtotal,
           ISNULL(a.NombreComercial, ua.Nombres + ' ' + ua.Apellidos) AS Agricultor,
           uc.Nombres + ' ' + uc.Apellidos AS Cliente
    FROM DetallePedido dp
    INNER JOIN Pedido       p  ON p.PedidoId = dp.PedidoId
    INNER JOIN Producto     pr ON pr.ProductoId = dp.ProductoId
    INNER JOIN Categoria    c  ON c.CategoriaId = pr.CategoriaId
    INNER JOIN UnidadMedida um ON um.UnidadMedidaId = pr.UnidadMedidaId
    INNER JOIN EstadoPedido e  ON e.EstadoPedidoId = p.EstadoPedidoId
    INNER JOIN Agricultor   a  ON a.AgricultorId = pr.AgricultorId
    INNER JOIN Usuario      ua ON ua.UsuarioId = a.UsuarioId
    INNER JOIN Cliente      cl ON cl.ClienteId = p.ClienteId
    INNER JOIN Usuario      uc ON uc.UsuarioId = cl.UsuarioId
    WHERE e.Nombre <> 'Cancelado'
      AND (@FechaInicio  IS NULL OR CAST(p.FechaPedido AS DATE) >= @FechaInicio)
      AND (@FechaFin     IS NULL OR CAST(p.FechaPedido AS DATE) <= @FechaFin)
      AND (@AgricultorId IS NULL OR pr.AgricultorId = @AgricultorId)
    ORDER BY p.FechaPedido DESC, p.PedidoId DESC
    OFFSET (@Pagina - 1) * @Tamano ROWS
    FETCH NEXT @Tamano ROWS ONLY;
END
GO


/* ---------------- Resumen: totales del periodo ---------------- */

DROP PROCEDURE IF EXISTS usp_Reporte_Resumen;
GO

CREATE PROCEDURE usp_Reporte_Resumen
    @FechaInicio  DATE = NULL,
    @FechaFin     DATE = NULL,
    @AgricultorId INT  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ISNULL(SUM(dp.Subtotal), 0)            AS TotalVendido,
           ISNULL(SUM(dp.Cantidad), 0)            AS UnidadesVendidas,
           COUNT(DISTINCT p.PedidoId)             AS Pedidos
    FROM DetallePedido dp
    INNER JOIN Pedido       p  ON p.PedidoId = dp.PedidoId
    INNER JOIN Producto     pr ON pr.ProductoId = dp.ProductoId
    INNER JOIN EstadoPedido e  ON e.EstadoPedidoId = p.EstadoPedidoId
    WHERE e.Nombre <> 'Cancelado'
      AND (@FechaInicio  IS NULL OR CAST(p.FechaPedido AS DATE) >= @FechaInicio)
      AND (@FechaFin     IS NULL OR CAST(p.FechaPedido AS DATE) <= @FechaFin)
      AND (@AgricultorId IS NULL OR pr.AgricultorId = @AgricultorId);
END
GO


/* ---------------- Productos más vendidos ---------------- */

DROP PROCEDURE IF EXISTS usp_Reporte_ProductosMasVendidos;
GO

CREATE PROCEDURE usp_Reporte_ProductosMasVendidos
    @FechaInicio  DATE = NULL,
    @FechaFin     DATE = NULL,
    @AgricultorId INT  = NULL,
    @Top          INT  = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top)
           pr.ProductoId,
           pr.Nombre AS Producto,
           c.Nombre  AS Categoria,
           ISNULL(a.NombreComercial, ua.Nombres + ' ' + ua.Apellidos) AS Agricultor,
           SUM(dp.Cantidad) AS UnidadesVendidas,
           SUM(dp.Subtotal) AS TotalVendido
    FROM DetallePedido dp
    INNER JOIN Pedido       p  ON p.PedidoId = dp.PedidoId
    INNER JOIN Producto     pr ON pr.ProductoId = dp.ProductoId
    INNER JOIN Categoria    c  ON c.CategoriaId = pr.CategoriaId
    INNER JOIN EstadoPedido e  ON e.EstadoPedidoId = p.EstadoPedidoId
    INNER JOIN Agricultor   a  ON a.AgricultorId = pr.AgricultorId
    INNER JOIN Usuario      ua ON ua.UsuarioId = a.UsuarioId
    WHERE e.Nombre <> 'Cancelado'
      AND (@FechaInicio  IS NULL OR CAST(p.FechaPedido AS DATE) >= @FechaInicio)
      AND (@FechaFin     IS NULL OR CAST(p.FechaPedido AS DATE) <= @FechaFin)
      AND (@AgricultorId IS NULL OR pr.AgricultorId = @AgricultorId)
    GROUP BY pr.ProductoId, pr.Nombre, c.Nombre, a.NombreComercial, ua.Nombres, ua.Apellidos
    ORDER BY SUM(dp.Cantidad) DESC;
END
GO


/* ---------------- Ventas por agricultor ---------------- */

DROP PROCEDURE IF EXISTS usp_Reporte_VentasPorAgricultor;
GO

CREATE PROCEDURE usp_Reporte_VentasPorAgricultor
    @FechaInicio DATE = NULL,
    @FechaFin    DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT a.AgricultorId,
           ISNULL(a.NombreComercial, ua.Nombres + ' ' + ua.Apellidos) AS Agricultor,
           ISNULL(d.Nombre, '') AS Distrito,
           COUNT(DISTINCT p.PedidoId) AS Pedidos,
           SUM(dp.Cantidad)           AS UnidadesVendidas,
           SUM(dp.Subtotal)           AS TotalVendido
    FROM DetallePedido dp
    INNER JOIN Pedido       p  ON p.PedidoId = dp.PedidoId
    INNER JOIN Producto     pr ON pr.ProductoId = dp.ProductoId
    INNER JOIN EstadoPedido e  ON e.EstadoPedidoId = p.EstadoPedidoId
    INNER JOIN Agricultor   a  ON a.AgricultorId = pr.AgricultorId
    INNER JOIN Usuario      ua ON ua.UsuarioId = a.UsuarioId
    LEFT  JOIN Distrito     d  ON d.DistritoId = a.DistritoId
    WHERE e.Nombre <> 'Cancelado'
      AND (@FechaInicio IS NULL OR CAST(p.FechaPedido AS DATE) >= @FechaInicio)
      AND (@FechaFin    IS NULL OR CAST(p.FechaPedido AS DATE) <= @FechaFin)
    GROUP BY a.AgricultorId, a.NombreComercial, ua.Nombres, ua.Apellidos, d.Nombre
    ORDER BY SUM(dp.Subtotal) DESC;
END
GO

-- Lista simple de agricultores para el filtro del reporte
DROP PROCEDURE IF EXISTS usp_Agricultor_ListarTodos;
GO

CREATE PROCEDURE usp_Agricultor_ListarTodos
AS
BEGIN
    SET NOCOUNT ON;

    SELECT a.AgricultorId,
           ISNULL(a.NombreComercial, u.Nombres + ' ' + u.Apellidos) AS Agricultor
    FROM Agricultor a
    INNER JOIN Usuario u ON u.UsuarioId = a.UsuarioId
    ORDER BY Agricultor;
END
GO


SET NOEXEC OFF;
GO

PRINT 'Procedimientos de Reportes creados.';
GO
