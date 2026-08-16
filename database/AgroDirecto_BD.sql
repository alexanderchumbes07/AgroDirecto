/* ================================================================
   AGRODIRECTO - Script de creación de base de datos
   Motor: Microsoft SQL Server
   Descripción: Plataforma de venta directa del agricultor al consumidor
   ================================================================ */

-- 1) Crear la base de datos (ejecutar solo si no existe)
IF DB_ID('AgroDirectoDB') IS NULL
    CREATE DATABASE AgroDirectoDB;
GO

USE AgroDirectoDB;
GO

/* ================================================================
   TABLAS MAESTRAS / CATÁLOGOS
   ================================================================ */

-- Rol: perfiles del sistema
CREATE TABLE Rol (
    RolId        INT IDENTITY(1,1) PRIMARY KEY,
    Nombre       VARCHAR(30) NOT NULL UNIQUE
);
GO

-- Distrito / Ubicación
CREATE TABLE Distrito (
    DistritoId    INT IDENTITY(1,1) PRIMARY KEY,
    Nombre        VARCHAR(80) NOT NULL,
    Provincia     VARCHAR(80) NULL,
    Departamento  VARCHAR(80) NULL
);
GO

-- Categoria de productos
CREATE TABLE Categoria (
    CategoriaId   INT IDENTITY(1,1) PRIMARY KEY,
    Nombre        VARCHAR(60) NOT NULL,
    Descripcion   VARCHAR(200) NULL,
    Estado        BIT NOT NULL DEFAULT 1        -- 1 = activo, 0 = inactivo
);
GO

-- Unidad de medida (kg, unidad, atado, etc.)
CREATE TABLE UnidadMedida (
    UnidadMedidaId INT IDENTITY(1,1) PRIMARY KEY,
    Nombre         VARCHAR(40) NOT NULL,
    Abreviatura    VARCHAR(10) NOT NULL
);
GO

-- Estado del pedido
CREATE TABLE EstadoPedido (
    EstadoPedidoId INT IDENTITY(1,1) PRIMARY KEY,
    Nombre         VARCHAR(30) NOT NULL UNIQUE
);
GO

/* ================================================================
   USUARIOS Y PERFILES
   ================================================================ */

-- Usuario: credenciales y datos generales de acceso
CREATE TABLE Usuario (
    UsuarioId     INT IDENTITY(1,1) PRIMARY KEY,
    RolId         INT NOT NULL,
    Nombres       VARCHAR(80) NOT NULL,
    Apellidos     VARCHAR(80) NOT NULL,
    Email         VARCHAR(120) NOT NULL UNIQUE,
    PasswordHash  VARCHAR(256) NOT NULL,
    Telefono      VARCHAR(20) NULL,
    Estado        BIT NOT NULL DEFAULT 1,
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Usuario_Rol FOREIGN KEY (RolId) REFERENCES Rol(RolId)
);
GO

-- Agricultor: datos del productor (1 a 1 con Usuario)
CREATE TABLE Agricultor (
    AgricultorId    INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId       INT NOT NULL UNIQUE,
    DistritoId      INT NULL,
    NombreComercial VARCHAR(120) NULL,
    Direccion       VARCHAR(200) NULL,
    Aprobado        BIT NOT NULL DEFAULT 0,   -- 0 = pendiente de aprobación del admin
    FechaAprobacion DATETIME NULL,
    CONSTRAINT FK_Agricultor_Usuario  FOREIGN KEY (UsuarioId)  REFERENCES Usuario(UsuarioId),
    CONSTRAINT FK_Agricultor_Distrito FOREIGN KEY (DistritoId) REFERENCES Distrito(DistritoId)
);
GO

-- Cliente: datos del consumidor (1 a 1 con Usuario)
CREATE TABLE Cliente (
    ClienteId  INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId  INT NOT NULL UNIQUE,
    DistritoId INT NULL,
    Direccion  VARCHAR(200) NULL,
    CONSTRAINT FK_Cliente_Usuario  FOREIGN KEY (UsuarioId)  REFERENCES Usuario(UsuarioId),
    CONSTRAINT FK_Cliente_Distrito FOREIGN KEY (DistritoId) REFERENCES Distrito(DistritoId)
);
GO

/* ================================================================
   PRODUCTOS
   ================================================================ */

CREATE TABLE Producto (
    ProductoId     INT IDENTITY(1,1) PRIMARY KEY,
    AgricultorId   INT NOT NULL,
    CategoriaId    INT NOT NULL,
    UnidadMedidaId INT NOT NULL,
    Nombre         VARCHAR(120) NOT NULL,
    Descripcion    VARCHAR(400) NULL,
    Precio         DECIMAL(10,2) NOT NULL CHECK (Precio >= 0),
    Stock          DECIMAL(10,2) NOT NULL DEFAULT 0 CHECK (Stock >= 0),
    ImagenUrl      VARCHAR(300) NULL,
    Estado         BIT NOT NULL DEFAULT 1,
    FechaRegistro  DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Producto_Agricultor   FOREIGN KEY (AgricultorId)   REFERENCES Agricultor(AgricultorId),
    CONSTRAINT FK_Producto_Categoria    FOREIGN KEY (CategoriaId)    REFERENCES Categoria(CategoriaId),
    CONSTRAINT FK_Producto_UnidadMedida FOREIGN KEY (UnidadMedidaId) REFERENCES UnidadMedida(UnidadMedidaId)
);
GO

/* ================================================================
   CARRITO DE COMPRAS
   ================================================================ */

CREATE TABLE Carrito (
    CarritoId     INT IDENTITY(1,1) PRIMARY KEY,
    ClienteId     INT NOT NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),
    Estado        VARCHAR(20) NOT NULL DEFAULT 'ACTIVO',  -- ACTIVO / PROCESADO
    CONSTRAINT FK_Carrito_Cliente FOREIGN KEY (ClienteId) REFERENCES Cliente(ClienteId)
);
GO

CREATE TABLE DetalleCarrito (
    DetalleCarritoId INT IDENTITY(1,1) PRIMARY KEY,
    CarritoId        INT NOT NULL,
    ProductoId       INT NOT NULL,
    Cantidad         DECIMAL(10,2) NOT NULL CHECK (Cantidad > 0),
    PrecioUnitario   DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_DetalleCarrito_Carrito  FOREIGN KEY (CarritoId)  REFERENCES Carrito(CarritoId),
    CONSTRAINT FK_DetalleCarrito_Producto FOREIGN KEY (ProductoId) REFERENCES Producto(ProductoId)
);
GO

/* ================================================================
   PEDIDOS
   ================================================================ */

CREATE TABLE Pedido (
    PedidoId        INT IDENTITY(1,1) PRIMARY KEY,
    ClienteId       INT NOT NULL,
    EstadoPedidoId  INT NOT NULL,
    FechaPedido     DATETIME NOT NULL DEFAULT GETDATE(),
    Total           DECIMAL(12,2) NOT NULL DEFAULT 0,
    DireccionEntrega VARCHAR(200) NULL,
    CONSTRAINT FK_Pedido_Cliente      FOREIGN KEY (ClienteId)      REFERENCES Cliente(ClienteId),
    CONSTRAINT FK_Pedido_EstadoPedido FOREIGN KEY (EstadoPedidoId) REFERENCES EstadoPedido(EstadoPedidoId)
);
GO

CREATE TABLE DetallePedido (
    DetallePedidoId INT IDENTITY(1,1) PRIMARY KEY,
    PedidoId        INT NOT NULL,
    ProductoId      INT NOT NULL,
    Cantidad        DECIMAL(10,2) NOT NULL CHECK (Cantidad > 0),
    PrecioUnitario  DECIMAL(10,2) NOT NULL,
    Subtotal        DECIMAL(12,2) NOT NULL,
    CONSTRAINT FK_DetallePedido_Pedido   FOREIGN KEY (PedidoId)   REFERENCES Pedido(PedidoId),
    CONSTRAINT FK_DetallePedido_Producto FOREIGN KEY (ProductoId) REFERENCES Producto(ProductoId)
);
GO

/* ================================================================
   ÍNDICES RECOMENDADOS (mejoran búsquedas y reportes)
   ================================================================ */
CREATE INDEX IX_Producto_Categoria   ON Producto(CategoriaId);
CREATE INDEX IX_Producto_Agricultor  ON Producto(AgricultorId);
CREATE INDEX IX_Pedido_Cliente       ON Pedido(ClienteId);
CREATE INDEX IX_Pedido_Fecha         ON Pedido(FechaPedido);
CREATE INDEX IX_DetallePedido_Pedido ON DetallePedido(PedidoId);
GO

/* ================================================================
   DATOS INICIALES (tablas maestras)
   ================================================================ */

INSERT INTO Rol (Nombre) VALUES ('Administrador'), ('Agricultor'), ('Cliente');

INSERT INTO EstadoPedido (Nombre) VALUES ('Pendiente'), ('Confirmado'), ('Entregado'), ('Cancelado');

INSERT INTO UnidadMedida (Nombre, Abreviatura) VALUES
    ('Kilogramo', 'kg'),
    ('Unidad', 'und'),
    ('Atado', 'atd'),
    ('Docena', 'doc'),
    ('Saco', 'sco');

INSERT INTO Categoria (Nombre, Descripcion) VALUES
    ('Verduras', 'Hortalizas y verduras frescas'),
    ('Frutas', 'Frutas de temporada'),
    ('Granos y menestras', 'Granos, cereales y legumbres'),
    ('Tubérculos', 'Papas, camotes y raíces'),
    ('Hierbas', 'Hierbas aromáticas y medicinales');
GO

PRINT 'Base de datos AgroDirecto creada correctamente.';
GO
