/* ================================================================
   AGRODIRECTO - 02. Procedimientos almacenados
   Ejecutar después de 01_CrearBaseDatos.sql.
   Por ahora solo el CRUD de Categoría; los demás se agregan aquí.

   Se usa el prefijo 'usp_' y no 'sp_' porque SQL Server busca los
   nombres que empiezan en 'sp_' primero en la base master, lo que
   provoca errores confusos ("Msg 208: Invalid object name").
   ================================================================ */

USE AgroDirectoDB_V2;
GO

-- Corta la ejecución si el USE falló, para no crear los objetos en master.
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


/* ---------------- CATEGORÍA: Listar ----------------
   Devuelve una página de resultados y el total de filas que cumplen
   el filtro, por parámetro de salida. */

DROP PROCEDURE IF EXISTS usp_Categoria_Listar;
GO

CREATE PROCEDURE usp_Categoria_Listar
    @Buscar  VARCHAR(60) = NULL,
    @Pagina  INT = 1,
    @Tamano  INT = 5,
    @Total   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @Total = COUNT(*)
    FROM Categoria
    WHERE (@Buscar IS NULL OR Nombre LIKE '%' + @Buscar + '%');

    SELECT CategoriaId, Nombre, Descripcion, Estado
    FROM Categoria
    WHERE (@Buscar IS NULL OR Nombre LIKE '%' + @Buscar + '%')
    ORDER BY CategoriaId
    OFFSET (@Pagina - 1) * @Tamano ROWS
    FETCH NEXT @Tamano ROWS ONLY;
END
GO


/* ---------------- CATEGORÍA: Obtener por Id ---------------- */

DROP PROCEDURE IF EXISTS usp_Categoria_ObtenerPorId;
GO

CREATE PROCEDURE usp_Categoria_ObtenerPorId
    @CategoriaId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CategoriaId, Nombre, Descripcion, Estado
    FROM Categoria
    WHERE CategoriaId = @CategoriaId;
END
GO


/* ---------------- CATEGORÍA: Insertar ---------------- */

DROP PROCEDURE IF EXISTS usp_Categoria_Insertar;
GO

CREATE PROCEDURE usp_Categoria_Insertar
    @Nombre       VARCHAR(60),
    @Descripcion  VARCHAR(200) = NULL,
    @Estado       BIT = 1,
    @CategoriaId  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Categoria WHERE Nombre = @Nombre)
    BEGIN
        RAISERROR(N'Ya existe una categoría con ese nombre.', 16, 1);
        RETURN;
    END

    INSERT INTO Categoria (Nombre, Descripcion, Estado)
    VALUES (@Nombre, @Descripcion, @Estado);

    SET @CategoriaId = SCOPE_IDENTITY();
END
GO


/* ---------------- CATEGORÍA: Actualizar ---------------- */

DROP PROCEDURE IF EXISTS usp_Categoria_Actualizar;
GO

CREATE PROCEDURE usp_Categoria_Actualizar
    @CategoriaId  INT,
    @Nombre       VARCHAR(60),
    @Descripcion  VARCHAR(200) = NULL,
    @Estado       BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Categoria WHERE Nombre = @Nombre AND CategoriaId <> @CategoriaId)
    BEGIN
        RAISERROR(N'Ya existe otra categoría con ese nombre.', 16, 1);
        RETURN;
    END

    UPDATE Categoria
    SET Nombre      = @Nombre,
        Descripcion = @Descripcion,
        Estado      = @Estado
    WHERE CategoriaId = @CategoriaId;
END
GO


/* ---------------- CATEGORÍA: Eliminar ----------------
   Se valida antes de borrar para devolver un mensaje entendible en
   lugar del error de clave foránea de SQL Server. */

DROP PROCEDURE IF EXISTS usp_Categoria_Eliminar;
GO

CREATE PROCEDURE usp_Categoria_Eliminar
    @CategoriaId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Producto WHERE CategoriaId = @CategoriaId)
    BEGIN
        RAISERROR(N'No se puede eliminar: la categoría tiene productos asociados. Desactívala en lugar de borrarla.', 16, 1);
        RETURN;
    END

    DELETE FROM Categoria WHERE CategoriaId = @CategoriaId;
END
GO


/* ================================================================
   UNIDAD DE MEDIDA
   ================================================================ */

DROP PROCEDURE IF EXISTS usp_UnidadMedida_Listar;
GO

CREATE PROCEDURE usp_UnidadMedida_Listar
    @Buscar  VARCHAR(40) = NULL,
    @Pagina  INT = 1,
    @Tamano  INT = 5,
    @Total   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @Total = COUNT(*)
    FROM UnidadMedida
    WHERE (@Buscar IS NULL OR Nombre LIKE '%' + @Buscar + '%' OR Abreviatura LIKE '%' + @Buscar + '%');

    SELECT UnidadMedidaId, Nombre, Abreviatura
    FROM UnidadMedida
    WHERE (@Buscar IS NULL OR Nombre LIKE '%' + @Buscar + '%' OR Abreviatura LIKE '%' + @Buscar + '%')
    ORDER BY UnidadMedidaId
    OFFSET (@Pagina - 1) * @Tamano ROWS
    FETCH NEXT @Tamano ROWS ONLY;
END
GO

DROP PROCEDURE IF EXISTS usp_UnidadMedida_ObtenerPorId;
GO

CREATE PROCEDURE usp_UnidadMedida_ObtenerPorId
    @UnidadMedidaId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT UnidadMedidaId, Nombre, Abreviatura
    FROM UnidadMedida
    WHERE UnidadMedidaId = @UnidadMedidaId;
END
GO

DROP PROCEDURE IF EXISTS usp_UnidadMedida_Insertar;
GO

CREATE PROCEDURE usp_UnidadMedida_Insertar
    @Nombre          VARCHAR(40),
    @Abreviatura     VARCHAR(10),
    @UnidadMedidaId  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM UnidadMedida WHERE Nombre = @Nombre)
    BEGIN
        RAISERROR(N'Ya existe una unidad de medida con ese nombre.', 16, 1);
        RETURN;
    END

    INSERT INTO UnidadMedida (Nombre, Abreviatura)
    VALUES (@Nombre, @Abreviatura);

    SET @UnidadMedidaId = SCOPE_IDENTITY();
END
GO

DROP PROCEDURE IF EXISTS usp_UnidadMedida_Actualizar;
GO

CREATE PROCEDURE usp_UnidadMedida_Actualizar
    @UnidadMedidaId  INT,
    @Nombre          VARCHAR(40),
    @Abreviatura     VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM UnidadMedida WHERE Nombre = @Nombre AND UnidadMedidaId <> @UnidadMedidaId)
    BEGIN
        RAISERROR(N'Ya existe otra unidad de medida con ese nombre.', 16, 1);
        RETURN;
    END

    UPDATE UnidadMedida
    SET Nombre      = @Nombre,
        Abreviatura = @Abreviatura
    WHERE UnidadMedidaId = @UnidadMedidaId;
END
GO

DROP PROCEDURE IF EXISTS usp_UnidadMedida_Eliminar;
GO

CREATE PROCEDURE usp_UnidadMedida_Eliminar
    @UnidadMedidaId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Producto WHERE UnidadMedidaId = @UnidadMedidaId)
    BEGIN
        RAISERROR(N'No se puede eliminar: hay productos que usan esta unidad de medida.', 16, 1);
        RETURN;
    END

    DELETE FROM UnidadMedida WHERE UnidadMedidaId = @UnidadMedidaId;
END
GO


/* ================================================================
   DISTRITO
   ================================================================ */

DROP PROCEDURE IF EXISTS usp_Distrito_Listar;
GO

CREATE PROCEDURE usp_Distrito_Listar
    @Buscar  VARCHAR(80) = NULL,
    @Pagina  INT = 1,
    @Tamano  INT = 5,
    @Total   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @Total = COUNT(*)
    FROM Distrito
    WHERE (@Buscar IS NULL OR Nombre LIKE '%' + @Buscar + '%'
                           OR Provincia LIKE '%' + @Buscar + '%'
                           OR Departamento LIKE '%' + @Buscar + '%');

    SELECT DistritoId, Nombre, Provincia, Departamento
    FROM Distrito
    WHERE (@Buscar IS NULL OR Nombre LIKE '%' + @Buscar + '%'
                           OR Provincia LIKE '%' + @Buscar + '%'
                           OR Departamento LIKE '%' + @Buscar + '%')
    ORDER BY DistritoId
    OFFSET (@Pagina - 1) * @Tamano ROWS
    FETCH NEXT @Tamano ROWS ONLY;
END
GO

DROP PROCEDURE IF EXISTS usp_Distrito_ObtenerPorId;
GO

CREATE PROCEDURE usp_Distrito_ObtenerPorId
    @DistritoId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DistritoId, Nombre, Provincia, Departamento
    FROM Distrito
    WHERE DistritoId = @DistritoId;
END
GO

DROP PROCEDURE IF EXISTS usp_Distrito_Insertar;
GO

CREATE PROCEDURE usp_Distrito_Insertar
    @Nombre        VARCHAR(80),
    @Provincia     VARCHAR(80) = NULL,
    @Departamento  VARCHAR(80) = NULL,
    @DistritoId    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- El mismo nombre puede repetirse en otra provincia (hay varios
    -- "San Juan" en el Perú), por eso se valida el par nombre+provincia.
    IF EXISTS (SELECT 1 FROM Distrito
               WHERE Nombre = @Nombre AND ISNULL(Provincia, '') = ISNULL(@Provincia, ''))
    BEGIN
        RAISERROR(N'Ya existe ese distrito en la misma provincia.', 16, 1);
        RETURN;
    END

    INSERT INTO Distrito (Nombre, Provincia, Departamento)
    VALUES (@Nombre, @Provincia, @Departamento);

    SET @DistritoId = SCOPE_IDENTITY();
END
GO

DROP PROCEDURE IF EXISTS usp_Distrito_Actualizar;
GO

CREATE PROCEDURE usp_Distrito_Actualizar
    @DistritoId    INT,
    @Nombre        VARCHAR(80),
    @Provincia     VARCHAR(80) = NULL,
    @Departamento  VARCHAR(80) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Distrito
               WHERE Nombre = @Nombre
                 AND ISNULL(Provincia, '') = ISNULL(@Provincia, '')
                 AND DistritoId <> @DistritoId)
    BEGIN
        RAISERROR(N'Ya existe otro distrito con ese nombre en la misma provincia.', 16, 1);
        RETURN;
    END

    UPDATE Distrito
    SET Nombre       = @Nombre,
        Provincia    = @Provincia,
        Departamento = @Departamento
    WHERE DistritoId = @DistritoId;
END
GO

DROP PROCEDURE IF EXISTS usp_Distrito_Eliminar;
GO

CREATE PROCEDURE usp_Distrito_Eliminar
    @DistritoId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Agricultor WHERE DistritoId = @DistritoId)
    BEGIN
        RAISERROR(N'No se puede eliminar: hay agricultores registrados en este distrito.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM Cliente WHERE DistritoId = @DistritoId)
    BEGIN
        RAISERROR(N'No se puede eliminar: hay clientes registrados en este distrito.', 16, 1);
        RETURN;
    END

    DELETE FROM Distrito WHERE DistritoId = @DistritoId;
END
GO


SET NOEXEC OFF;
GO

PRINT 'Procedimientos creados: Categoría, UnidadMedida y Distrito.';
GO
