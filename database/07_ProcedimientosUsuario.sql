/* ================================================================
   AGRODIRECTO - 07. Mantenimiento de Usuarios (Fase 3)
   Ejecutar después de 06_DatosPrueba.sql.

   No es un CRUD normal: los usuarios se crean al registrarse, nunca
   desde aquí, y las contraseñas jamás se leen ni se editan. El
   administrador solo activa/desactiva cuentas y aprueba agricultores.
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


/* ---------------- Listar con filtros y paginación ---------------- */

DROP PROCEDURE IF EXISTS usp_Usuario_Listar;
GO

CREATE PROCEDURE usp_Usuario_Listar
    @Buscar  VARCHAR(120) = NULL,
    @RolId   INT = NULL,
    @Pagina  INT = 1,
    @Tamano  INT = 8,
    @Total   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @Total = COUNT(*)
    FROM Usuario u
    WHERE (@RolId IS NULL OR u.RolId = @RolId)
      AND (@Buscar IS NULL OR u.Nombres LIKE '%' + @Buscar + '%'
                           OR u.Apellidos LIKE '%' + @Buscar + '%'
                           OR u.Email LIKE '%' + @Buscar + '%');

    SELECT u.UsuarioId, u.RolId, r.Nombre AS Rol,
           u.Nombres, u.Apellidos, u.Email, u.Telefono,
           u.Estado, u.FechaRegistro,
           a.AgricultorId, a.Aprobado, a.NombreComercial
    FROM Usuario u
    INNER JOIN Rol r        ON r.RolId = u.RolId
    LEFT  JOIN Agricultor a ON a.UsuarioId = u.UsuarioId
    WHERE (@RolId IS NULL OR u.RolId = @RolId)
      AND (@Buscar IS NULL OR u.Nombres LIKE '%' + @Buscar + '%'
                           OR u.Apellidos LIKE '%' + @Buscar + '%'
                           OR u.Email LIKE '%' + @Buscar + '%')
    ORDER BY u.UsuarioId
    OFFSET (@Pagina - 1) * @Tamano ROWS
    FETCH NEXT @Tamano ROWS ONLY;
END
GO


/* ---------------- Activar / desactivar una cuenta ----------------
   No se eliminan usuarios: tienen productos y pedidos asociados.
   Desactivar basta, porque el login ya comprueba Estado.            */

DROP PROCEDURE IF EXISTS usp_Usuario_CambiarEstado;
GO

CREATE PROCEDURE usp_Usuario_CambiarEstado
    @UsuarioId INT,
    @Estado    BIT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Usuario WHERE UsuarioId = @UsuarioId)
    BEGIN
        RAISERROR(N'El usuario no existe.', 16, 1);
        RETURN;
    END

    -- Nunca dejar el sistema sin ningún administrador activo
    IF @Estado = 0
       AND EXISTS (SELECT 1 FROM Usuario u JOIN Rol r ON r.RolId = u.RolId
                   WHERE u.UsuarioId = @UsuarioId AND r.Nombre = 'Administrador')
       AND (SELECT COUNT(*) FROM Usuario u JOIN Rol r ON r.RolId = u.RolId
            WHERE r.Nombre = 'Administrador' AND u.Estado = 1) <= 1
    BEGIN
        RAISERROR(N'No puedes desactivar al último administrador activo del sistema.', 16, 1);
        RETURN;
    END

    UPDATE Usuario SET Estado = @Estado WHERE UsuarioId = @UsuarioId;
END
GO


/* ---------------- Aprobar / desaprobar un agricultor ---------------- */

DROP PROCEDURE IF EXISTS usp_Agricultor_CambiarAprobacion;
GO

CREATE PROCEDURE usp_Agricultor_CambiarAprobacion
    @AgricultorId INT,
    @Aprobado     BIT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Agricultor WHERE AgricultorId = @AgricultorId)
    BEGIN
        RAISERROR(N'El agricultor no existe.', 16, 1);
        RETURN;
    END

    UPDATE Agricultor
    SET Aprobado        = @Aprobado,
        FechaAprobacion = CASE WHEN @Aprobado = 1 THEN GETDATE() ELSE NULL END
    WHERE AgricultorId = @AgricultorId;
END
GO


SET NOEXEC OFF;
GO

PRINT 'Procedimientos de Usuario creados.';
GO
