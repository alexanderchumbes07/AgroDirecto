/* ================================================================
   AGRODIRECTO - 01. Base de datos y tablas
   Ejecutar primero, luego 02_ProcedimientosAlmacenados.sql.

   La base se llama AgroDirectoDB_V2 porque el nombre AgroDirectoDB
   está ocupado por una versión anterior del proyecto. Para
   renombrarla basta con cambiarla aquí y en appsettings.json.
   ================================================================ */

IF DB_ID('AgroDirectoDB_V2') IS NULL
    CREATE DATABASE AgroDirectoDB_V2;
GO

USE AgroDirectoDB_V2;
GO


/* ---------------- TABLAS MAESTRAS ---------------- */

CREATE TABLE Rol (
    RolId   INT IDENTITY(1,1) PRIMARY KEY,
    Nombre  VARCHAR(30) NOT NULL UNIQUE
);
GO

CREATE TABLE Distrito (
    DistritoId    INT IDENTITY(1,1) PRIMARY KEY,
    Nombre        VARCHAR(80) NOT NULL,
    Provincia     VARCHAR(80) NULL,
    Departamento  VARCHAR(80) NULL
);
GO

CREATE TABLE Categoria (
    CategoriaId  INT IDENTITY(1,1) PRIMARY KEY,
    Nombre       VARCHAR(60)  NOT NULL,
    Descripcion  VARCHAR(200) NULL,
    Estado       BIT NOT NULL DEFAULT 1        -- 1 = activo, 0 = inactivo
);
GO

CREATE TABLE UnidadMedida (
    UnidadMedidaId  INT IDENTITY(1,1) PRIMARY KEY,
    Nombre          VARCHAR(40) NOT NULL,
    Abreviatura     VARCHAR(10) NOT NULL
);
GO

CREATE TABLE EstadoPedido (
    EstadoPedidoId  INT IDENTITY(1,1) PRIMARY KEY,
    Nombre          VARCHAR(30) NOT NULL UNIQUE
);
GO


/* ---------------- USUARIOS Y PERFILES ---------------- */

CREATE TABLE Usuario (
    UsuarioId      INT IDENTITY(1,1) PRIMARY KEY,
    RolId          INT NOT NULL,
    Nombres        VARCHAR(80)  NOT NULL,
    Apellidos      VARCHAR(80)  NOT NULL,
    Email          VARCHAR(120) NOT NULL UNIQUE,
    PasswordHash   VARCHAR(256) NOT NULL,
    Telefono       VARCHAR(20)  NULL,
    Estado         BIT NOT NULL DEFAULT 1,
    FechaRegistro  DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Usuario_Rol FOREIGN KEY (RolId) REFERENCES Rol(RolId)
);
GO

CREATE TABLE Agricultor (
    AgricultorId     INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId        INT NOT NULL UNIQUE,
    DistritoId       INT NULL,
    NombreComercial  VARCHAR(120) NULL,
    Direccion        VARCHAR(200) NULL,
    Aprobado         BIT NOT NULL DEFAULT 0,   -- lo aprueba el administrador
    FechaAprobacion  DATETIME NULL,
    CONSTRAINT FK_Agricultor_Usuario  FOREIGN KEY (UsuarioId)  REFERENCES Usuario(UsuarioId),
    CONSTRAINT FK_Agricultor_Distrito FOREIGN KEY (DistritoId) REFERENCES Distrito(DistritoId)
);
GO
 
CREATE TABLE Cliente (
    ClienteId   INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId   INT NOT NULL UNIQUE,
    DistritoId  INT NULL,
    Direccion   VARCHAR(200) NULL,
    CONSTRAINT FK_Cliente_Usuario  FOREIGN KEY (UsuarioId)  REFERENCES Usuario(UsuarioId),
    CONSTRAINT FK_Cliente_Distrito FOREIGN KEY (DistritoId) REFERENCES Distrito(DistritoId)
);
GO


/* ---------------- PRODUCTOS ---------------- */

CREATE TABLE Producto (
    ProductoId      INT IDENTITY(1,1) PRIMARY KEY,
    AgricultorId    INT NOT NULL,
    CategoriaId     INT NOT NULL,
    UnidadMedidaId  INT NOT NULL,
    Nombre          VARCHAR(120) NOT NULL,
    Descripcion     VARCHAR(400) NULL,
    Precio          DECIMAL(10,2) NOT NULL CHECK (Precio >= 0),
    Stock           DECIMAL(10,2) NOT NULL DEFAULT 0 CHECK (Stock >= 0),
	MontoMinimo    DECIMAL(10,2) NOT NULL DEFAULT 0 CHECK (MontoMinimo >= 0),
    ImagenUrl       VARCHAR(300) NULL,
    Estado          BIT NOT NULL DEFAULT 1,
    FechaRegistro   DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Producto_Agricultor   FOREIGN KEY (AgricultorId)   REFERENCES Agricultor(AgricultorId),
    CONSTRAINT FK_Producto_Categoria    FOREIGN KEY (CategoriaId)    REFERENCES Categoria(CategoriaId),
    CONSTRAINT FK_Producto_UnidadMedida FOREIGN KEY (UnidadMedidaId) REFERENCES UnidadMedida(UnidadMedidaId)
);
GO


/* ---------------- CARRITO ---------------- */

CREATE TABLE Carrito (
    CarritoId      INT IDENTITY(1,1) PRIMARY KEY,
    ClienteId      INT NOT NULL,
    FechaCreacion  DATETIME NOT NULL DEFAULT GETDATE(),
    Estado         VARCHAR(20) NOT NULL DEFAULT 'ACTIVO',  -- ACTIVO / PROCESADO
    CONSTRAINT FK_Carrito_Cliente FOREIGN KEY (ClienteId) REFERENCES Cliente(ClienteId)
);
GO

CREATE TABLE DetalleCarrito (
    DetalleCarritoId  INT IDENTITY(1,1) PRIMARY KEY,
    CarritoId         INT NOT NULL,
    ProductoId        INT NOT NULL,
    Cantidad          DECIMAL(10,2) NOT NULL CHECK (Cantidad > 0),
    PrecioUnitario    DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_DetalleCarrito_Carrito  FOREIGN KEY (CarritoId)  REFERENCES Carrito(CarritoId),
    CONSTRAINT FK_DetalleCarrito_Producto FOREIGN KEY (ProductoId) REFERENCES Producto(ProductoId)
);
GO


/* ---------------- PEDIDOS ----------------

   El cliente hace UNA compra, pero esa compra puede llevar productos de
   varios agricultores. Como cada agricultor prepara y entrega lo suyo por
   su cuenta, la compra se parte en un Pedido por agricultor:

       Compra  1 ── N  Pedido  1 ── N  DetallePedido
       (lo que ve            (lo que ve
        el cliente)           cada agricultor)

   Así cada agricultor confirma, entrega o cancela su parte sin decidir por
   los demás, y el cliente sigue viendo su compra completa con el total. */

CREATE TABLE Compra (
    CompraId          INT IDENTITY(1,1) PRIMARY KEY,
    ClienteId         INT NOT NULL,
    FechaCompra       DATETIME NOT NULL DEFAULT GETDATE(),
    Total             DECIMAL(12,2) NOT NULL DEFAULT 0,
    DireccionEntrega  VARCHAR(200) NULL,
    CONSTRAINT FK_Compra_Cliente FOREIGN KEY (ClienteId) REFERENCES Cliente(ClienteId)
);
GO

CREATE TABLE Pedido (
    PedidoId          INT IDENTITY(1,1) PRIMARY KEY,
    CompraId          INT NOT NULL,
    ClienteId         INT NOT NULL,
    AgricultorId      INT NOT NULL,   -- un pedido pertenece a un solo agricultor
    EstadoPedidoId    INT NOT NULL,
    FechaPedido       DATETIME NOT NULL DEFAULT GETDATE(),
    Total             DECIMAL(12,2) NOT NULL DEFAULT 0,
    DireccionEntrega  VARCHAR(200) NULL,
    CONSTRAINT FK_Pedido_Compra       FOREIGN KEY (CompraId)       REFERENCES Compra(CompraId),
    CONSTRAINT FK_Pedido_Cliente      FOREIGN KEY (ClienteId)      REFERENCES Cliente(ClienteId),
    CONSTRAINT FK_Pedido_Agricultor   FOREIGN KEY (AgricultorId)   REFERENCES Agricultor(AgricultorId),
    CONSTRAINT FK_Pedido_EstadoPedido FOREIGN KEY (EstadoPedidoId) REFERENCES EstadoPedido(EstadoPedidoId),
    -- Un agricultor no puede tener dos pedidos dentro de la misma compra
    CONSTRAINT UQ_Pedido_Compra_Agricultor UNIQUE (CompraId, AgricultorId)
);
GO

CREATE TABLE DetallePedido (
    DetallePedidoId  INT IDENTITY(1,1) PRIMARY KEY,
    PedidoId         INT NOT NULL,
    ProductoId       INT NOT NULL,
    Cantidad         DECIMAL(10,2) NOT NULL CHECK (Cantidad > 0),
    PrecioUnitario   DECIMAL(10,2) NOT NULL,
    Subtotal         DECIMAL(12,2) NOT NULL,
    CONSTRAINT FK_DetallePedido_Pedido   FOREIGN KEY (PedidoId)   REFERENCES Pedido(PedidoId),
    CONSTRAINT FK_DetallePedido_Producto FOREIGN KEY (ProductoId) REFERENCES Producto(ProductoId)
);
GO


/* ---------------- ÍNDICES ---------------- */

CREATE INDEX IX_Producto_Categoria   ON Producto(CategoriaId);
CREATE INDEX IX_Producto_Agricultor  ON Producto(AgricultorId);
CREATE INDEX IX_Compra_Cliente       ON Compra(ClienteId);
CREATE INDEX IX_Pedido_Cliente       ON Pedido(ClienteId);
CREATE INDEX IX_Pedido_Agricultor    ON Pedido(AgricultorId);
CREATE INDEX IX_Pedido_Compra        ON Pedido(CompraId);
CREATE INDEX IX_Pedido_Fecha         ON Pedido(FechaPedido);
CREATE INDEX IX_DetallePedido_Pedido ON DetallePedido(PedidoId);
GO


/* ---------------- DATOS MAESTROS ---------------- */

INSERT INTO Rol (Nombre) VALUES ('Administrador'), ('Agricultor'), ('Cliente');

INSERT INTO EstadoPedido (Nombre) VALUES ('Pendiente'), ('Confirmado'), ('Entregado'), ('Cancelado');

INSERT INTO UnidadMedida (Nombre, Abreviatura) VALUES
    ('Kilogramo', 'kg'),
    ('Unidad',    'und'),
    ('Atado',     'atd'),
    ('Docena',    'doc'),
    ('Saco',      'sco');

INSERT INTO Categoria (Nombre, Descripcion) VALUES
    ('Verduras',           'Hortalizas y verduras frescas'),
    ('Frutas',             'Frutas de temporada'),
    ('Granos y menestras', 'Granos, cereales y legumbres'),
    ('Tubérculos',         'Papas, camotes y raíces'),
    ('Hierbas',            'Hierbas aromáticas y medicinales');

INSERT INTO Distrito (Nombre, Provincia, Departamento) VALUES
    ('Huaral',     'Huaral', 'Lima'),
    ('Cañete',     'Cañete', 'Lima'),
    ('Lurín',      'Lima',   'Lima'),
    ('Pachacámac', 'Lima',   'Lima');
GO


PRINT 'Base AgroDirectoDB_V2 creada: 13 tablas y datos maestros cargados.';
GO
