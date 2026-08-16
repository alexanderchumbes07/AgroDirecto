/* ================================================================
   AGRODIRECTO - 05. Procedimientos de Producto y catálogo
   Fases 4 (productos del agricultor) y 5 (catálogo del cliente).
   Ejecutar después de 04_DatosIniciales.sql.
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


/* ================================================================
   APOYO: listas para los desplegables y el perfil del agricultor
   ================================================================ */

-- Devuelve el AgricultorId a partir del usuario que inició sesión.
DROP PROCEDURE IF EXISTS usp_Agricultor_ObtenerPorUsuario;
GO

CREATE PROCEDURE usp_Agricultor_ObtenerPorUsuario
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT AgricultorId, UsuarioId, DistritoId, NombreComercial, Direccion, Aprobado
    FROM Agricultor
    WHERE UsuarioId = @UsuarioId;
END
GO

-- Solo las categorías activas: no tiene sentido publicar en una desactivada.
DROP PROCEDURE IF EXISTS usp_Categoria_ListarActivas;
GO

CREATE PROCEDURE usp_Categoria_ListarActivas
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CategoriaId, Nombre, Descripcion, Estado
    FROM Categoria
    WHERE Estado = 1
    ORDER BY Nombre;
END
GO

DROP PROCEDURE IF EXISTS usp_UnidadMedida_ListarTodas;
GO

CREATE PROCEDURE usp_UnidadMedida_ListarTodas
AS
BEGIN
    SET NOCOUNT ON;

    SELECT UnidadMedidaId, Nombre, Abreviatura
    FROM UnidadMedida
    ORDER BY Nombre;
END
GO

DROP PROCEDURE IF EXISTS usp_Distrito_ListarTodos;
GO

CREATE PROCEDURE usp_Distrito_ListarTodos
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DistritoId, Nombre, Provincia, Departamento
    FROM Distrito
    ORDER BY Nombre;
END
GO


/* ================================================================
   FASE 4 - PRODUCTOS DEL AGRICULTOR
   ================================================================ */

-- Los productos de UN agricultor, con búsqueda y paginación.
DROP PROCEDURE IF EXISTS usp_Producto_ListarPorAgricultor;
GO

CREATE PROCEDURE usp_Producto_ListarPorAgricultor
    @AgricultorId INT,
    @Buscar       VARCHAR(120) = NULL,
    @Pagina       INT = 1,
    @Tamano       INT = 5,
    @Total        INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @Total = COUNT(*)
    FROM Producto
    WHERE AgricultorId = @AgricultorId
      AND (@Buscar IS NULL OR Nombre LIKE '%' + @Buscar + '%');

    SELECT p.ProductoId, p.AgricultorId, p.CategoriaId, p.UnidadMedidaId,
           p.Nombre, p.Descripcion, p.Precio, p.Stock, p.MontoMinimo, p.ImagenUrl,
           p.Estado, p.FechaRegistro,
           c.Nombre AS Categoria, u.Abreviatura AS Unidad
    FROM Producto p
    INNER JOIN Categoria c    ON c.CategoriaId = p.CategoriaId
    INNER JOIN UnidadMedida u ON u.UnidadMedidaId = p.UnidadMedidaId
    WHERE p.AgricultorId = @AgricultorId
      AND (@Buscar IS NULL OR p.Nombre LIKE '%' + @Buscar + '%')
    ORDER BY p.ProductoId DESC
    OFFSET (@Pagina - 1) * @Tamano ROWS
    FETCH NEXT @Tamano ROWS ONLY;
END
GO

DROP PROCEDURE IF EXISTS usp_Producto_ObtenerPorId;
GO

CREATE PROCEDURE usp_Producto_ObtenerPorId
    @ProductoId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.ProductoId, p.AgricultorId, p.CategoriaId, p.UnidadMedidaId,
           p.Nombre, p.Descripcion, p.Precio, p.Stock, p.MontoMinimo, p.ImagenUrl,
           p.Estado, p.FechaRegistro,
           c.Nombre AS Categoria, u.Abreviatura AS Unidad,
           ISNULL(a.NombreComercial, us.Nombres + ' ' + us.Apellidos) AS Agricultor,
           ISNULL(d.Nombre, '') AS Distrito
    FROM Producto p
    INNER JOIN Categoria c     ON c.CategoriaId = p.CategoriaId
    INNER JOIN UnidadMedida u  ON u.UnidadMedidaId = p.UnidadMedidaId
    INNER JOIN Agricultor a    ON a.AgricultorId = p.AgricultorId
    INNER JOIN Usuario us      ON us.UsuarioId = a.UsuarioId
    LEFT  JOIN Distrito d      ON d.DistritoId = a.DistritoId
    WHERE p.ProductoId = @ProductoId;
END
GO

DROP PROCEDURE IF EXISTS usp_Producto_Insertar;
GO

CREATE PROCEDURE usp_Producto_Insertar
    @AgricultorId   INT,
    @CategoriaId    INT,
    @UnidadMedidaId INT,
    @Nombre         VARCHAR(120),
    @Descripcion    VARCHAR(400) = NULL,
    @Precio         DECIMAL(10,2),
    @Stock          DECIMAL(10,2),
    @MontoMinimo    DECIMAL(10,2) = 0.00, 
    @ImagenUrl      VARCHAR(300) = NULL,
    @Estado         BIT = 1,
    @ProductoId     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Agricultor WHERE AgricultorId = @AgricultorId AND Aprobado = 1)
    BEGIN
        RAISERROR(N'Tu cuenta de agricultor todavía no ha sido aprobada por el administrador.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM Producto WHERE AgricultorId = @AgricultorId AND Nombre = @Nombre)
    BEGIN
        RAISERROR(N'Ya tienes un producto publicado con ese nombre.', 16, 1);
        RETURN;
    END

    INSERT INTO Producto (AgricultorId, CategoriaId, UnidadMedidaId, Nombre,
                          Descripcion, Precio, Stock, MontoMinimo, ImagenUrl, Estado)
    VALUES (@AgricultorId, @CategoriaId, @UnidadMedidaId, @Nombre,
            @Descripcion, @Precio, @Stock, @MontoMinimo, @ImagenUrl, @Estado);

    SET @ProductoId = SCOPE_IDENTITY();
END
GO

DROP PROCEDURE IF EXISTS usp_Producto_Actualizar;
GO

CREATE PROCEDURE usp_Producto_Actualizar
    @ProductoId     INT,
    @AgricultorId   INT,
    @CategoriaId    INT,
    @UnidadMedidaId INT,
    @Nombre         VARCHAR(120),
    @Descripcion    VARCHAR(400) = NULL,
    @Precio         DECIMAL(10,2),
    @Stock          DECIMAL(10,2),
    @MontoMinimo    DECIMAL(10,2) = 0.00,
    @ImagenUrl      VARCHAR(300) = NULL,
    @Estado         BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Producto
                   WHERE ProductoId = @ProductoId AND AgricultorId = @AgricultorId)
    BEGIN
        RAISERROR(N'El producto no existe o no te pertenece.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM Producto
               WHERE AgricultorId = @AgricultorId AND Nombre = @Nombre AND ProductoId <> @ProductoId)
    BEGIN
        RAISERROR(N'Ya tienes otro producto publicado con ese nombre.', 16, 1);
        RETURN;
    END

    UPDATE Producto
    SET CategoriaId    = @CategoriaId,
        UnidadMedidaId = @UnidadMedidaId,
        Nombre         = @Nombre,
        Descripcion    = @Descripcion,
        Precio         = @Precio,
        Stock          = @Stock,
        MontoMinimo    = @MontoMinimo, 
        ImagenUrl      = @ImagenUrl,
        Estado         = @Estado
    WHERE ProductoId = @ProductoId;
END
GO

DROP PROCEDURE IF EXISTS usp_Producto_Eliminar;
GO

CREATE PROCEDURE usp_Producto_Eliminar
    @ProductoId   INT,
    @AgricultorId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Producto
                   WHERE ProductoId = @ProductoId AND AgricultorId = @AgricultorId)
    BEGIN
        RAISERROR(N'El producto no existe o no te pertenece.', 16, 1);
        RETURN;
    END

    -- Un producto ya vendido no se borra: quedaría un pedido sin detalle.
    IF EXISTS (SELECT 1 FROM DetallePedido WHERE ProductoId = @ProductoId)
    BEGIN
        RAISERROR(N'No se puede eliminar: el producto ya forma parte de pedidos. Desactívalo en lugar de borrarlo.', 16, 1);
        RETURN;
    END

    DELETE FROM DetalleCarrito WHERE ProductoId = @ProductoId;
    DELETE FROM Producto       WHERE ProductoId = @ProductoId;
END
GO


/* ================================================================
   FASE 5 - CATÁLOGO PÚBLICO
   Lo consume el Web API por AJAX. Solo muestra productos activos,
   con stock y de categorías activas.
   ================================================================ */

DROP PROCEDURE IF EXISTS usp_Producto_Catalogo;
GO

CREATE PROCEDURE usp_Producto_Catalogo
    @Buscar      VARCHAR(120)  = NULL,
    @CategoriaId INT           = NULL,
    @DistritoId  INT           = NULL,
    @PrecioMax   DECIMAL(10,2) = NULL,
    @Pagina      INT = 1,
    @Tamano      INT = 8,
    @Total       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @Total = COUNT(*)
    FROM Producto p
    INNER JOIN Categoria  c ON c.CategoriaId = p.CategoriaId
    INNER JOIN Agricultor a ON a.AgricultorId = p.AgricultorId
    WHERE p.Estado = 1
      AND c.Estado = 1
      AND a.Aprobado = 1          -- si al agricultor le retiran la aprobación, sale del catálogo
      AND p.Stock > 0
      AND (@Buscar      IS NULL OR p.Nombre LIKE '%' + @Buscar + '%')
      AND (@CategoriaId IS NULL OR p.CategoriaId = @CategoriaId)
      AND (@DistritoId  IS NULL OR a.DistritoId  = @DistritoId)
      AND (@PrecioMax   IS NULL OR p.Precio     <= @PrecioMax);

    SELECT p.ProductoId, p.Nombre, p.Descripcion, p.Precio, p.Stock, p.MontoMinimo, p.ImagenUrl,
           c.CategoriaId, c.Nombre AS Categoria,
           u.Abreviatura AS Unidad,
           ISNULL(a.NombreComercial, us.Nombres + ' ' + us.Apellidos) AS Agricultor,
           ISNULL(d.Nombre, '') AS Distrito
    FROM Producto p 
    INNER JOIN Categoria    c  ON c.CategoriaId = p.CategoriaId
    INNER JOIN UnidadMedida u  ON u.UnidadMedidaId = p.UnidadMedidaId
    INNER JOIN Agricultor   a  ON a.AgricultorId = p.AgricultorId
    INNER JOIN Usuario      us ON us.UsuarioId = a.UsuarioId
    LEFT  JOIN Distrito     d  ON d.DistritoId = a.DistritoId
    WHERE p.Estado = 1
      AND c.Estado = 1
      AND a.Aprobado = 1
      AND p.Stock > 0
      AND (@Buscar      IS NULL OR p.Nombre LIKE '%' + @Buscar + '%')
      AND (@CategoriaId IS NULL OR p.CategoriaId = @CategoriaId)
      AND (@DistritoId  IS NULL OR a.DistritoId  = @DistritoId)
      AND (@PrecioMax   IS NULL OR p.Precio     <= @PrecioMax)
    ORDER BY p.Nombre
    OFFSET (@Pagina - 1) * @Tamano ROWS
    FETCH NEXT @Tamano ROWS ONLY;
END
GO


SET NOEXEC OFF;
GO

PRINT 'Procedimientos de Producto y catálogo creados.';
GO
