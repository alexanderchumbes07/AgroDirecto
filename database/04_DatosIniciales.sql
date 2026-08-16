/* ================================================================
   AGRODIRECTO - 04. Datos iniciales
   Ejecutar después de 03_ProcedimientosSeguridad.sql.

   Crea la cuenta de Administrador. Hace falta porque el formulario
   de registro solo permite crear perfiles Cliente y Agricultor;
   sin esta cuenta nadie podría entrar a los mantenimientos.

       Correo:      admin@agrodirecto.com
       Contraseña:  Admin123

   ATENCIÓN: es una contraseña de desarrollo y está a la vista en
   este archivo. Cambiarla antes de cualquier uso real.

   El valor de PasswordHash es PBKDF2-SHA256 con 100000 iteraciones,
   en el formato  iteraciones.sal.hash  que espera Seguridad/Password.cs.
   No se puede escribir a mano: si lo modificas, el login deja de validar.
   ================================================================ */

USE AgroDirectoDB_V2;
GO

IF DB_NAME() <> N'AgroDirectoDB_V2'
BEGIN
    RAISERROR(N'ERROR: no se pudo cambiar a AgroDirectoDB_V2. Ejecuta primero 01_CrearBaseDatos.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

IF EXISTS (SELECT 1 FROM Usuario WHERE Email = 'admin@agrodirecto.com')
BEGIN
    PRINT 'La cuenta de administrador ya existía. No se hizo ningún cambio.';
END
ELSE
BEGIN
    DECLARE @RolAdmin INT = (SELECT RolId FROM Rol WHERE Nombre = 'Administrador');

    INSERT INTO Usuario (RolId, Nombres, Apellidos, Email, PasswordHash, Telefono)
    VALUES (@RolAdmin, 'Administrador', 'AgroDirecto', 'admin@agrodirecto.com',
            '100000.fVEXu4LZIwIGKEMmnlHIgA==.cDdTYE2YmO/0WR+EIFa+M1XAmx/MVsJOn5hIqhKFx0c=',
            NULL);

    PRINT 'Cuenta de administrador creada: admin@agrodirecto.com / Admin123';
END
GO

SET NOEXEC OFF;
GO
