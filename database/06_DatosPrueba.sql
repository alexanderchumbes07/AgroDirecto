/* ================================================================
   AGRODIRECTO - 06. Datos de prueba (OPCIONAL)
   Ejecutar después de 05_ProcedimientosProducto.sql.

   Crea 3 agricultores, 1 cliente y 16 productos, para poder probar
   el catálogo y los filtros sin cargar todo a mano.

   Todas las cuentas usan la misma contraseña: Agro123

       agricultor1@agrodirecto.com   Chacra Los Andes    (Huaral)
       agricultor2@agrodirecto.com   Fundo San Miguel    (Cañete)
       agricultor3@agrodirecto.com   Huerta Verde        (Lurín)
       cliente1@agrodirecto.com      Cliente de prueba

   Se puede volver a ejecutar: si ya existen, no hace nada.
   ================================================================ */

USE AgroDirectoDB_V2;
GO

IF DB_NAME() <> N'AgroDirectoDB_V2'
BEGIN
    RAISERROR(N'ERROR: no se pudo cambiar a AgroDirectoDB_V2. Ejecuta primero 01_CrearBaseDatos.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT 1 FROM Usuario WHERE Email = 'agricultor1@agrodirecto.com')
BEGIN
    PRINT 'Los datos de prueba ya estaban cargados. No se hizo ningun cambio.';
END
ELSE
BEGIN
    DECLARE @RolAgr INT = (SELECT RolId FROM Rol WHERE Nombre = 'Agricultor');
    DECLARE @RolCli INT = (SELECT RolId FROM Rol WHERE Nombre = 'Cliente');

    DECLARE @dHuaral INT = (SELECT TOP 1 DistritoId FROM Distrito WHERE Nombre = 'Huaral');
    DECLARE @dCanete INT = (SELECT TOP 1 DistritoId FROM Distrito WHERE Nombre = 'Cañete');
    DECLARE @dLurin  INT = (SELECT TOP 1 DistritoId FROM Distrito WHERE Nombre = 'Lurín');

    DECLARE @cVerd INT = (SELECT CategoriaId FROM Categoria WHERE Nombre = 'Verduras');
    DECLARE @cFrut INT = (SELECT CategoriaId FROM Categoria WHERE Nombre = 'Frutas');
    DECLARE @cGran INT = (SELECT CategoriaId FROM Categoria WHERE Nombre = 'Granos y menestras');
    DECLARE @cTube INT = (SELECT CategoriaId FROM Categoria WHERE Nombre = 'Tubérculos');
    DECLARE @cHier INT = (SELECT CategoriaId FROM Categoria WHERE Nombre = 'Hierbas');

    DECLARE @uKg   INT = (SELECT UnidadMedidaId FROM UnidadMedida WHERE Abreviatura = 'kg');
    DECLARE @uUnd  INT = (SELECT UnidadMedidaId FROM UnidadMedida WHERE Abreviatura = 'und');
    DECLARE @uAtd  INT = (SELECT UnidadMedidaId FROM UnidadMedida WHERE Abreviatura = 'atd');
    DECLARE @uSco  INT = (SELECT UnidadMedidaId FROM UnidadMedida WHERE Abreviatura = 'sco');

    /* ---------------- Agricultor 1 ---------------- */
    INSERT INTO Usuario (RolId, Nombres, Apellidos, Email, PasswordHash, Telefono)
    VALUES (@RolAgr, 'Manuel', 'Quispe', 'agricultor1@agrodirecto.com',
            '100000.Okc1Kdtt6cuvogJ1BxDYIg==.WUo8KeKuzWvZvxjt/3el3tHuXo4wbh3cEFb02YjfzIs=', '987111222');
    DECLARE @u1 INT = SCOPE_IDENTITY();

    INSERT INTO Agricultor (UsuarioId, DistritoId, NombreComercial, Direccion, Aprobado, FechaAprobacion)
    VALUES (@u1, @dHuaral, 'Chacra Los Andes', 'Km 62 Panamericana Norte', 1, GETDATE());
    DECLARE @a1 INT = SCOPE_IDENTITY();

    /* ---------------- Agricultor 2 ---------------- */
    INSERT INTO Usuario (RolId, Nombres, Apellidos, Email, PasswordHash, Telefono)
    VALUES (@RolAgr, 'Rosa', 'Huamán', 'agricultor2@agrodirecto.com',
            '100000.wkB3OdDvUDLoqVz6gsMHDQ==.eL9Jf2SgqFpTlgv1attoRFaV8+czLlfdsMd1py0SoJ4=', '987333444');
    DECLARE @u2 INT = SCOPE_IDENTITY();

    INSERT INTO Agricultor (UsuarioId, DistritoId, NombreComercial, Direccion, Aprobado, FechaAprobacion)
    VALUES (@u2, @dCanete, 'Fundo San Miguel', 'Valle de Cañete s/n', 1, GETDATE());
    DECLARE @a2 INT = SCOPE_IDENTITY();

    /* ---------------- Agricultor 3 ---------------- */
    INSERT INTO Usuario (RolId, Nombres, Apellidos, Email, PasswordHash, Telefono)
    VALUES (@RolAgr, 'Julio', 'Ccahuana', 'agricultor3@agrodirecto.com',
            '100000.r7hsk8DggMh82m6Nbivwvw==.GZFvgKSts91LyH5qFbnU9fqv8L9WmJaoDCjbdawDae8=', '987555666');
    DECLARE @u3 INT = SCOPE_IDENTITY();

    INSERT INTO Agricultor (UsuarioId, DistritoId, NombreComercial, Direccion, Aprobado, FechaAprobacion)
    VALUES (@u3, @dLurin, 'Huerta Verde', 'Antigua Panamericana Sur Km 40', 1, GETDATE());
    DECLARE @a3 INT = SCOPE_IDENTITY();

    /* ---------------- Cliente ---------------- */
    INSERT INTO Usuario (RolId, Nombres, Apellidos, Email, PasswordHash, Telefono)
    VALUES (@RolCli, 'Lucía', 'Ramos', 'cliente1@agrodirecto.com',
            '100000.gI3R75FFisdFyAObF6Ipjg==.OIM1rKOiX/NmqsKlpLq4QtIRQPMtwgG1USxmnAokjMw=', '912345678');
    DECLARE @u4 INT = SCOPE_IDENTITY();

    INSERT INTO Cliente (UsuarioId, DistritoId, Direccion)
    VALUES (@u4, @dLurin, 'Av. Arequipa 1234, Lince');

    /* ---------------- Productos ----------------
       Variados a propósito: distintas categorías, unidades, precios y
       distritos, para que los filtros del catálogo tengan qué filtrar.
       Dos quedan desactivados y uno sin stock, para comprobar que el
       catálogo público NO los muestra.                                */

    /* Las fotos viven en wwwroot/img/productos/ y vienen con el proyecto, así
       que el catálogo se ve igual en cualquier máquina y sin internet.
       El origen y la licencia de cada una están en CREDITOS.md de esa carpeta. */

	INSERT INTO Producto (AgricultorId, CategoriaId, UnidadMedidaId, Nombre, Descripcion, Precio, Stock, MontoMinimo, ImagenUrl, Estado)
	VALUES
	-- Chacra Los Andes (Huaral)
	(@a1, @cFrut, @uKg,  'Palta Fuerte',     'Palta de pulpa cremosa, cosechada esta semana.',       8.50, 120, 25.00, '/img/productos/palta.jpg',       1),
	(@a1, @cFrut, @uKg,  'Fresa de Huaral',   'Fresa fresca y dulce, seleccionada a mano.',          12.00,  60, 24.00, '/img/productos/fresa.jpg',       1),
	(@a1, @cVerd, @uUnd, 'Lechuga americana', 'Lechuga hidropónica, libre de pesticidas.',            3.00,  80, 15.00, '/img/productos/lechuga.jpg',     1),
	(@a1, @cVerd, @uKg,  'Tomate italiano',   'Ideal para salsas y ensaladas.',                        4.20,  95, 20.00, '/img/productos/tomate.jpg',      1),
	(@a1, @cHier, @uAtd, 'Culantro',          'Atado fresco, cortado el mismo día.',                  1.50, 200, 10.00, '/img/productos/culantro.jpg',    1),
	(@a1, @cTube, @uSco, 'Papa amarilla',     'Papa amarilla tumbay, saco de 5 kg.',                 18.00,  40, 36.00, '/img/productos/papa.jpg',        1),

	-- Fundo San Miguel (Cañete)
	(@a2, @cFrut, @uKg,  'Mandarina Satsuma', 'Sin pepa, muy jugosa. Cosecha de temporada.',          5.50, 150, 20.00, '/img/productos/mandarina.jpg',   1),
	(@a2, @cFrut, @uUnd, 'Sandía',            'Pieza entera de unos 6 kg.',                          14.00,  25, 14.00, '/img/productos/sandia.jpg',      1),
	(@a2, @cVerd, @uKg,  'Zapallo macre',     'Ideal para sopas y purés.',                            3.80, 110, 15.00, '/img/productos/zapallo.jpg',     1),
	(@a2, @cGran, @uKg,  'Quinua orgánica',   'Quinua blanca lavada, certificada orgánica.',         15.50,  35, 31.00, '/img/productos/quinua.jpg',      1),
	(@a2, @cGran, @uKg,  'Frijol canario',    'Grano seleccionado, cosecha reciente.',                9.20,  70, 25.00, '/img/productos/frijol.jpg',      1),

	-- Huerta Verde (Lurín)
	(@a3, @cVerd, @uAtd, 'Espinaca',          'Atado de hoja tierna.',                                2.50, 130, 10.00, '/img/productos/espinaca.jpg',    1),
	(@a3, @cVerd, @uUnd, 'Brócoli',           'Cabeza compacta, verde intenso.',                      4.00,  90, 12.00, '/img/productos/brocoli.jpg',     1),
	(@a3, @cTube, @uKg,  'Camote morado',     'Alto en antioxidantes.',                               5.00,  55, 15.00, '/img/productos/camote.jpg',      1),
	(@a3, @cHier, @uAtd, 'Hierba luisa',      'Aromática, para infusiones.',                          1.80, 160,  9.00, '/img/productos/hierbaluisa.jpg', 1),

	-- Casos límite para probar el catálogo
	(@a3, @cFrut, @uKg,  'Maracuyá',          'AGOTADO: no debe aparecer en el catálogo.',            7.00,   0, 20.00, '/img/productos/maracuya.jpg',    1),
	(@a3, @cVerd, @uKg,  'Apio',              'DESACTIVADO: no debe aparecer en el catálogo.',        2.20,  50, 10.00, '/img/productos/apio.jpg',        0);
	
	PRINT 'Datos de prueba cargados: 3 agricultores, 1 cliente y 17 productos.';
    PRINT 'Todas las cuentas usan la contrasena: Agro123';
END
GO

SET NOEXEC OFF;
GO
