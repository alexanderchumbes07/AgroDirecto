/* ================================================================
   AGRODIRECTO - 03. Procedimientos de seguridad
   Ejecutar después de 02_ProcedimientosAlmacenados.sql.
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


/* ---------------- ROL: Listar ---------------- */

DROP PROCEDURE IF EXISTS usp_Rol_Listar;
GO

CREATE PROCEDURE usp_Rol_Listar
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RolId, Nombre
    FROM Rol
    ORDER BY RolId;
END
GO


/* ---------------- USUARIO: Obtener por email ----------------
   Lo usa el login. Devuelve el hash para que la aplicación lo
   compare; la contraseña nunca viaja en claro por ningún lado. */

DROP PROCEDURE IF EXISTS usp_Usuario_ObtenerPorEmail;
GO

CREATE PROCEDURE usp_Usuario_ObtenerPorEmail
    @Email VARCHAR(120)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT u.UsuarioId, u.RolId, r.Nombre AS Rol,
           u.Nombres, u.Apellidos, u.Email, u.PasswordHash, u.Estado
    FROM Usuario u
    INNER JOIN Rol r ON r.RolId = u.RolId
    WHERE u.Email = @Email;
END
GO


/* ---------------- USUARIO: Registrar ----------------
   Crea el usuario y su perfil (Agricultor o Cliente) dentro de una
   transacción: si algo falla, no queda un usuario sin perfil. */

DROP PROCEDURE IF EXISTS usp_Usuario_Registrar;
GO

CREATE PROCEDURE usp_Usuario_Registrar
    @RolId         INT,
    @Nombres       VARCHAR(80),
    @Apellidos     VARCHAR(80),
    @Email         VARCHAR(120),
    @PasswordHash  VARCHAR(256),
    @Telefono      VARCHAR(20) = NULL,
    @UsuarioId     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS (SELECT 1 FROM Usuario WHERE Email = @Email)
    BEGIN
        RAISERROR(N'Ya existe una cuenta registrada con ese correo.', 16, 1);
        RETURN;
    END

    DECLARE @Rol VARCHAR(30) = (SELECT Nombre FROM Rol WHERE RolId = @RolId);

    IF @Rol IS NULL
    BEGIN
        RAISERROR(N'El perfil seleccionado no existe.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO Usuario (RolId, Nombres, Apellidos, Email, PasswordHash, Telefono)
        VALUES (@RolId, @Nombres, @Apellidos, @Email, @PasswordHash, @Telefono);

        SET @UsuarioId = SCOPE_IDENTITY();

        IF @Rol = 'Agricultor'
            INSERT INTO Agricultor (UsuarioId) VALUES (@UsuarioId);
        ELSE IF @Rol = 'Cliente'
            INSERT INTO Cliente (UsuarioId) VALUES (@UsuarioId);

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

PRINT 'Procedimientos de seguridad creados.';
GO
