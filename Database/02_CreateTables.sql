/*
====================================================================
 CreditPulse - Script 02: Creación de tablas
 Motor: Microsoft SQL Server Express 2022
====================================================================
 Diseño (Fase 1 - núcleo):
   Roles, Usuarios            -> autenticación básica (RBAC completo: fase 2)
   Productos, Politicas       -> catálogo de productos y motor de reglas configurable
   Evaluaciones                -> encabezado de cada evaluación crediticia
   EvaluacionPoliticas         -> detalle: resultado de cada política evaluada

 Notas de diseño:
   - Los NIT/DPI se guardan tal cual (texto). El documento de Viabilidad
     Operativa exige que los identificadores queden encriptados en las
     tablas maestras; ese cifrado a nivel de columna (ENCRYPTION o
     Always Encrypted) se implementa en la Fase 2 junto con el módulo
     de roles avanzados, para no bloquear la entrega del núcleo
     funcional. Se deja documentado en el manual técnico.
   - EvaluacionId inicia en 100001 para calzar con el formato de
     "Número de Evaluación" mostrado en las pantallas de referencia.
*/
USE CreditPulseDB;
GO

------------------------------------------------------------
-- Roles
------------------------------------------------------------
CREATE TABLE dbo.Roles (
    RolId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    Nombre       NVARCHAR(50)  NOT NULL,
    Descripcion  NVARCHAR(200) NULL,
    CONSTRAINT UQ_Roles_Nombre UNIQUE (Nombre)
);
GO

------------------------------------------------------------
-- Usuarios
------------------------------------------------------------
CREATE TABLE dbo.Usuarios (
    UsuarioId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,
    Email             NVARCHAR(150) NOT NULL,
    PasswordHash      NVARCHAR(200) NOT NULL,   -- hash PBKDF2 (Base64)
    PasswordSalt      NVARCHAR(200) NOT NULL,   -- salt aleatorio por usuario (Base64)
    NombreCompleto    NVARCHAR(200) NOT NULL,
    RolId             INT NOT NULL,
    Activo            BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
    IntentosFallidos  INT NOT NULL CONSTRAINT DF_Usuarios_Intentos DEFAULT (0),
    FechaCreacion     DATETIME2(0) NOT NULL CONSTRAINT DF_Usuarios_FechaCreacion DEFAULT (SYSUTCDATETIME()),
    UltimoLogin       DATETIME2(0) NULL,
    CONSTRAINT UQ_Usuarios_Email UNIQUE (Email),
    CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (RolId) REFERENCES dbo.Roles (RolId)
);
GO

------------------------------------------------------------
-- Productos (catálogo de productos bancarios evaluables)
------------------------------------------------------------
CREATE TABLE dbo.Productos (
    ProductoId                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Productos PRIMARY KEY,
    Codigo                      NVARCHAR(30)  NOT NULL,
    Nombre                      NVARCHAR(150) NOT NULL,
    Descripcion                 NVARCHAR(400) NULL,
    RequiereIngresos            BIT NOT NULL CONSTRAINT DF_Productos_ReqIngresos DEFAULT (1),
    RequiereTipoEmpleo          BIT NOT NULL CONSTRAINT DF_Productos_ReqEmpleo DEFAULT (1),
    RequiereAntiguedadLaboral   BIT NOT NULL CONSTRAINT DF_Productos_ReqAntiguedad DEFAULT (0),
    Activo                      BIT NOT NULL CONSTRAINT DF_Productos_Activo DEFAULT (1),
    FechaCreacion                DATETIME2(0) NOT NULL CONSTRAINT DF_Productos_FechaCreacion DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_Productos_Codigo UNIQUE (Codigo)
);
GO

------------------------------------------------------------
-- Politicas (reglas de negocio configurables por producto)
------------------------------------------------------------
-- TipoRegla identifica el algoritmo que ejecuta el PolicyEngine (BLL).
-- ParametrosJson guarda los umbrales de esa regla, ej: {"min":18}
CREATE TABLE dbo.Politicas (
    PoliticaId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Politicas PRIMARY KEY,
    ProductoId       INT NOT NULL,
    Codigo           NVARCHAR(40)  NOT NULL,
    Nombre           NVARCHAR(150) NOT NULL,
    Descripcion      NVARCHAR(400) NULL,
    TipoRegla        NVARCHAR(50)  NOT NULL,
    ParametrosJson   NVARCHAR(500) NOT NULL,
    Orden            INT NOT NULL CONSTRAINT DF_Politicas_Orden DEFAULT (0),
    Activo           BIT NOT NULL CONSTRAINT DF_Politicas_Activo DEFAULT (1),
    CONSTRAINT FK_Politicas_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (ProductoId),
    CONSTRAINT UQ_Politicas_Producto_Codigo UNIQUE (ProductoId, Codigo)
);
GO

------------------------------------------------------------
-- Evaluaciones (encabezado)
------------------------------------------------------------
CREATE TABLE dbo.Evaluaciones (
    EvaluacionId            INT IDENTITY(100001,1) NOT NULL CONSTRAINT PK_Evaluaciones PRIMARY KEY,
    ProductoId              INT NOT NULL,
    UsuarioId               INT NOT NULL,
    PrimerNombre            NVARCHAR(100) NOT NULL,
    SegundoNombre           NVARCHAR(100) NULL,
    PrimerApellido          NVARCHAR(100) NOT NULL,
    SegundoApellido         NVARCHAR(100) NULL,
    Nit                     NVARCHAR(20)  NOT NULL,
    Dpi                     NVARCHAR(20)  NOT NULL,
    FechaNacimiento         DATE NOT NULL,
    IngresosMensuales       DECIMAL(12,2) NULL,
    TipoEmpleo              NVARCHAR(150) NULL,
    AntiguedadLaboralMeses  INT NULL,
    ResultadoGeneral        NVARCHAR(10) NOT NULL,
    Estado                  NVARCHAR(30) NOT NULL CONSTRAINT DF_Evaluaciones_Estado DEFAULT ('Evaluada'),
    SibRawXml               XML NULL,
    SibScoreBuro            INT NULL,
    SibNivelEndeudamiento   DECIMAL(5,2) NULL,
    SibEnListaNegra         BIT NULL,
    FechaCreacion           DATETIME2(0) NOT NULL CONSTRAINT DF_Evaluaciones_FechaCreacion DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_Evaluaciones_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (ProductoId),
    CONSTRAINT FK_Evaluaciones_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId),
    CONSTRAINT CK_Evaluaciones_ResultadoGeneral CHECK (ResultadoGeneral IN ('VERDE','AMARILLO','ROJO'))
);
GO

CREATE INDEX IX_Evaluaciones_Dpi ON dbo.Evaluaciones (Dpi);
CREATE INDEX IX_Evaluaciones_FechaCreacion ON dbo.Evaluaciones (FechaCreacion DESC);
GO

------------------------------------------------------------
-- EvaluacionPoliticas (detalle: resultado por política)
------------------------------------------------------------
CREATE TABLE dbo.EvaluacionPoliticas (
    EvaluacionPoliticaId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EvaluacionPoliticas PRIMARY KEY,
    EvaluacionId          INT NOT NULL,
    PoliticaId            INT NULL,               -- puede quedar NULL si la política se elimina luego
    NombrePolitica        NVARCHAR(150) NOT NULL, -- snapshot: nombre al momento de evaluar
    Resultado              NVARCHAR(10) NOT NULL,
    Detalle                NVARCHAR(400) NULL,
    Orden                  INT NOT NULL CONSTRAINT DF_EvaluacionPoliticas_Orden DEFAULT (0),
    CONSTRAINT FK_EvaluacionPoliticas_Evaluaciones FOREIGN KEY (EvaluacionId) REFERENCES dbo.Evaluaciones (EvaluacionId),
    CONSTRAINT FK_EvaluacionPoliticas_Politicas FOREIGN KEY (PoliticaId) REFERENCES dbo.Politicas (PoliticaId),
    CONSTRAINT CK_EvaluacionPoliticas_Resultado CHECK (Resultado IN ('VERDE','AMARILLO','ROJO'))
);
GO

CREATE INDEX IX_EvaluacionPoliticas_EvaluacionId ON dbo.EvaluacionPoliticas (EvaluacionId);
GO
