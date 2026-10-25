/*
====================================================================
 CreditPulse - Base de datos completa para MySQL 8.0+
 Equivalente en MySQL de los scripts 01..11 de SQL Server.
====================================================================
 Incluye: esquema (8 tablas, llaves foráneas, índices, CHECK) y los
 datos semilla (roles, usuarios, productos y políticas).

 Pensado para: MySQL Workbench -> Database -> Reverse Engineer
 (Ctrl+R) y obtener el diagrama entidad-relación.

 Equivalencias de tipos respecto a SQL Server:
   NVARCHAR(n)      -> VARCHAR(n)  (utf8mb4)
   BIT              -> TINYINT (0/1)
   DATETIME2(0)     -> DATETIME    (UTC)
   XML              -> LONGTEXT    (MySQL no tiene tipo XML)
   IDENTITY(1,1)    -> AUTO_INCREMENT
   IDENTITY(100001) -> AUTO_INCREMENT=100001

 Los procedimientos almacenados y los tipos de tabla (TVP) de SQL
 Server NO se incluyen: no aportan al diagrama y son específicos de
 T-SQL.

 Usuarios de prueba (misma contraseña que en SQL Server):
   admin@creditpulse.com      Admin#2026!
   analista@creditpulse.com   Analista#2026!
   consulta@creditpulse.com   Consulta#2026!
====================================================================
*/

SET NAMES utf8mb4;

CREATE DATABASE IF NOT EXISTS CreditPulseDB
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;

USE CreditPulseDB;

-- ------------------------------------------------------------
-- Roles
-- ------------------------------------------------------------
CREATE TABLE Roles (
    RolId                     INT AUTO_INCREMENT NOT NULL,
    Nombre                    VARCHAR(50)  NOT NULL,
    Descripcion               VARCHAR(200) NULL,
    PuedeEvaluar              TINYINT      NOT NULL DEFAULT 0,
    PuedeAdministrarCatalogo  TINYINT      NOT NULL DEFAULT 0,
    PuedeAdministrarUsuarios  TINYINT      NOT NULL DEFAULT 0,
    Activo                    TINYINT      NOT NULL DEFAULT 1,
    CONSTRAINT PK_Roles PRIMARY KEY (RolId),
    CONSTRAINT UQ_Roles_Nombre UNIQUE (Nombre)
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- Usuarios
-- ------------------------------------------------------------
CREATE TABLE Usuarios (
    UsuarioId         INT AUTO_INCREMENT NOT NULL,
    Email             VARCHAR(150) NOT NULL,
    PasswordHash      VARCHAR(200) NOT NULL,   -- hash PBKDF2 (Base64)
    PasswordSalt      VARCHAR(200) NOT NULL,   -- salt aleatorio por usuario (Base64)
    NombreCompleto    VARCHAR(200) NOT NULL,
    RolId             INT          NOT NULL,
    Activo            TINYINT      NOT NULL DEFAULT 1,
    IntentosFallidos  INT          NOT NULL DEFAULT 0,
    FechaCreacion     DATETIME     NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UltimoLogin       DATETIME     NULL,
    CONSTRAINT PK_Usuarios PRIMARY KEY (UsuarioId),
    CONSTRAINT UQ_Usuarios_Email UNIQUE (Email),
    CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (RolId) REFERENCES Roles (RolId)
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- Productos
-- ------------------------------------------------------------
CREATE TABLE Productos (
    ProductoId                 INT AUTO_INCREMENT NOT NULL,
    Codigo                     VARCHAR(30)  NOT NULL,
    Nombre                     VARCHAR(150) NOT NULL,
    Descripcion                VARCHAR(400) NULL,
    RequiereIngresos           TINYINT      NOT NULL DEFAULT 1,
    RequiereTipoEmpleo         TINYINT      NOT NULL DEFAULT 1,
    RequiereAntiguedadLaboral  TINYINT      NOT NULL DEFAULT 0,
    Activo                     TINYINT      NOT NULL DEFAULT 1,
    FechaCreacion              DATETIME     NOT NULL DEFAULT (UTC_TIMESTAMP()),
    CONSTRAINT PK_Productos PRIMARY KEY (ProductoId),
    CONSTRAINT UQ_Productos_Codigo UNIQUE (Codigo)
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- Politicas
-- ------------------------------------------------------------
CREATE TABLE Politicas (
    PoliticaId       INT AUTO_INCREMENT NOT NULL,
    ProductoId       INT          NOT NULL,
    Codigo           VARCHAR(40)  NOT NULL,
    Nombre           VARCHAR(150) NOT NULL,
    Descripcion      VARCHAR(400) NULL,
    TipoRegla        VARCHAR(50)  NOT NULL,
    ParametrosJson   VARCHAR(500) NOT NULL,
    Orden            INT          NOT NULL DEFAULT 0,
    Activo           TINYINT      NOT NULL DEFAULT 1,
    CONSTRAINT PK_Politicas PRIMARY KEY (PoliticaId),
    CONSTRAINT FK_Politicas_Productos FOREIGN KEY (ProductoId) REFERENCES Productos (ProductoId),
    CONSTRAINT UQ_Politicas_Producto_Codigo UNIQUE (ProductoId, Codigo)
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- Evaluaciones (encabezado)
-- ------------------------------------------------------------
CREATE TABLE Evaluaciones (
    EvaluacionId            INT AUTO_INCREMENT NOT NULL,
    ProductoId              INT           NOT NULL,
    UsuarioId               INT           NOT NULL,
    PrimerNombre            VARCHAR(100)  NOT NULL,
    SegundoNombre           VARCHAR(100)  NULL,
    PrimerApellido          VARCHAR(100)  NOT NULL,
    SegundoApellido         VARCHAR(100)  NULL,
    Nit                     VARCHAR(20)   NOT NULL,
    Dpi                     VARCHAR(20)   NOT NULL,
    FechaNacimiento         DATE          NOT NULL,
    IngresosMensuales       DECIMAL(12,2) NULL,
    TipoEmpleo              VARCHAR(150)  NULL,
    AntiguedadLaboralMeses  INT           NULL,
    ResultadoGeneral        VARCHAR(10)   NOT NULL,
    Estado                  VARCHAR(30)   NOT NULL DEFAULT 'Evaluada',
    SibRawXml               LONGTEXT      NULL,   -- XML crudo devuelto por la SIB
    SibScoreBuro            INT           NULL,
    SibNivelEndeudamiento   DECIMAL(5,2)  NULL,
    SibEnListaNegra         TINYINT       NULL,
    FechaCreacion           DATETIME      NOT NULL DEFAULT (UTC_TIMESTAMP()),
    CONSTRAINT PK_Evaluaciones PRIMARY KEY (EvaluacionId),
    CONSTRAINT FK_Evaluaciones_Productos FOREIGN KEY (ProductoId) REFERENCES Productos (ProductoId),
    CONSTRAINT FK_Evaluaciones_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Usuarios (UsuarioId),
    CONSTRAINT CK_Evaluaciones_ResultadoGeneral CHECK (ResultadoGeneral IN ('VERDE','AMARILLO','ROJO')),
    INDEX IX_Evaluaciones_Dpi (Dpi),
    INDEX IX_Evaluaciones_FechaCreacion (FechaCreacion DESC)
) ENGINE=InnoDB AUTO_INCREMENT=100001;

-- ------------------------------------------------------------
-- EvaluacionPoliticas (detalle: resultado por política)
-- ------------------------------------------------------------
CREATE TABLE EvaluacionPoliticas (
    EvaluacionPoliticaId  INT AUTO_INCREMENT NOT NULL,
    EvaluacionId          INT          NOT NULL,
    PoliticaId            INT          NULL,      -- NULL si la política se elimina luego
    NombrePolitica        VARCHAR(150) NOT NULL,  -- copia del nombre al momento de evaluar
    Resultado             VARCHAR(10)  NOT NULL,
    Detalle               VARCHAR(400) NULL,
    Orden                 INT          NOT NULL DEFAULT 0,
    CONSTRAINT PK_EvaluacionPoliticas PRIMARY KEY (EvaluacionPoliticaId),
    CONSTRAINT FK_EvaluacionPoliticas_Evaluaciones FOREIGN KEY (EvaluacionId) REFERENCES Evaluaciones (EvaluacionId),
    CONSTRAINT FK_EvaluacionPoliticas_Politicas FOREIGN KEY (PoliticaId) REFERENCES Politicas (PoliticaId),
    CONSTRAINT CK_EvaluacionPoliticas_Resultado CHECK (Resultado IN ('VERDE','AMARILLO','ROJO')),
    INDEX IX_EvaluacionPoliticas_EvaluacionId (EvaluacionId)
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- Lotes (encabezado de una carga batch)
-- ------------------------------------------------------------
CREATE TABLE Lotes (
    LoteId           INT AUTO_INCREMENT NOT NULL,
    NombreArchivo    VARCHAR(255) NOT NULL,
    UsuarioId        INT          NOT NULL,
    TotalRegistros   INT          NOT NULL DEFAULT 0,
    TotalProcesados  INT          NOT NULL DEFAULT 0,
    TotalVerde       INT          NOT NULL DEFAULT 0,
    TotalAmarillo    INT          NOT NULL DEFAULT 0,
    TotalRojo        INT          NOT NULL DEFAULT 0,
    TotalErrores     INT          NOT NULL DEFAULT 0,
    Estado           VARCHAR(20)  NOT NULL DEFAULT 'Completado',
    MensajeError     VARCHAR(400) NULL,
    FechaCreacion    DATETIME     NOT NULL DEFAULT (UTC_TIMESTAMP()),
    CONSTRAINT PK_Lotes PRIMARY KEY (LoteId),
    CONSTRAINT FK_Lotes_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Usuarios (UsuarioId),
    CONSTRAINT CK_Lotes_Estado CHECK (Estado IN ('Procesando','Completado','Error')),
    INDEX IX_Lotes_FechaCreacion (FechaCreacion DESC)
) ENGINE=InnoDB;

-- ------------------------------------------------------------
-- LoteEvaluaciones (una fila del Excel = una fila aquí)
-- ------------------------------------------------------------
CREATE TABLE LoteEvaluaciones (
    LoteEvaluacionId        INT AUTO_INCREMENT NOT NULL,
    LoteId                  INT          NOT NULL,
    EvaluacionId            INT          NULL,    -- NULL si la fila falló antes de evaluarse
    FilaExcel               INT          NOT NULL,
    NombreCompletoOriginal  VARCHAR(300) NULL,
    NitOriginal             VARCHAR(20)  NULL,
    DpiOriginal             VARCHAR(20)  NULL,
    Estado                  VARCHAR(20)  NOT NULL, -- 'Evaluado' | 'Error'
    MensajeError            VARCHAR(400) NULL,
    CONSTRAINT PK_LoteEvaluaciones PRIMARY KEY (LoteEvaluacionId),
    CONSTRAINT FK_LoteEvaluaciones_Lotes FOREIGN KEY (LoteId) REFERENCES Lotes (LoteId),
    CONSTRAINT FK_LoteEvaluaciones_Evaluaciones FOREIGN KEY (EvaluacionId) REFERENCES Evaluaciones (EvaluacionId),
    CONSTRAINT CK_LoteEvaluaciones_Estado CHECK (Estado IN ('Evaluado','Error')),
    INDEX IX_LoteEvaluaciones_LoteId (LoteId)
) ENGINE=InnoDB;


/*
====================================================================
 DATOS SEMILLA
====================================================================
*/

-- ------------------------------------------------------------
-- Roles
-- ------------------------------------------------------------
INSERT INTO Roles (Nombre, Descripcion, PuedeEvaluar, PuedeAdministrarCatalogo, PuedeAdministrarUsuarios, Activo) VALUES
    ('Administrador',       'Acceso total: administración de usuarios, productos y políticas.', 1, 1, 1, 1),
    ('Analista de Crédito', 'Captura y evalúa solicitudes de crédito.',                         1, 0, 0, 1),
    ('Consulta',            'Solo puede consultar evaluaciones ya generadas.',                  0, 0, 0, 1),
    ('Integración API',     'Cuenta de servicio usada para atribuir evaluaciones creadas por las APIs de integración (no un usuario humano).', 1, 0, 0, 1);

-- ------------------------------------------------------------
-- Usuarios (hash PBKDF2-HMAC-SHA256, 100000 iteraciones)
-- ------------------------------------------------------------
SET @RolAdmin       = (SELECT RolId FROM Roles WHERE Nombre = 'Administrador');
SET @RolAnalista    = (SELECT RolId FROM Roles WHERE Nombre = 'Analista de Crédito');
SET @RolConsulta    = (SELECT RolId FROM Roles WHERE Nombre = 'Consulta');
SET @RolIntegracion = (SELECT RolId FROM Roles WHERE Nombre = 'Integración API');

INSERT INTO Usuarios (Email, PasswordHash, PasswordSalt, NombreCompleto, RolId, Activo) VALUES
    ('admin@creditpulse.com',       'kkndUARTgcIXIq8gHMmMwvi8hMj27pHqsiyosRghxts=', '070KnQqXpt73Ok72JKdXIQ==', 'Administrador CreditPulse',            @RolAdmin,       1),
    ('analista@creditpulse.com',    '+OsFkV2mGHtYhe+m4LBhvTq6HljMKbsD23zxf6oXTr0=', 'LY4g5u0wgXyRp7HFgIZNsg==', 'Analista de Crédito (Prueba)',        @RolAnalista,    1),
    ('consulta@creditpulse.com',    '2SDSuYZ3wOlvnT+tiWR/P+oL96ldwEUER+OW4aWCPhQ=', '7TckSUBbPe5dUH0aSfkpOg==', 'Usuario de Consulta (Prueba)',        @RolConsulta,    1),
    ('integracion@creditpulse.com', '8lsDHkOrhEGTgd/o5RZR5igX4ioogA56uxDFmIXm8tY=', 'w3N9emvRnWhW+HXIILNpHg==', 'Cuenta de Servicio - Integración API', @RolIntegracion, 1);

-- ------------------------------------------------------------
-- Productos
-- ------------------------------------------------------------
INSERT INTO Productos (Codigo, Nombre, Descripcion, RequiereIngresos, RequiereTipoEmpleo, RequiereAntiguedadLaboral) VALUES
    ('TARJETA_CREDITO',      'Tarjeta de Crédito Clásica', 'Tarjeta de crédito de consumo revolvente.',                      1, 1, 0),
    ('PRESTAMO_HIPOTECARIO', 'Préstamo Hipotecario',       'Financiamiento para vivienda a largo plazo.',                    1, 1, 1),
    ('CREDITO_PERSONAL',     'Crédito Personal',           'Préstamo de libre disponibilidad a corto/mediano plazo.',        1, 1, 0),
    ('CREDITO_VEHICULAR',    'Crédito Vehicular',          'Financiamiento para compra de vehículo.',                        1, 1, 1),
    ('GENERAL',              'Precalificación General',    'Producto usado por Evaluaciones Batch: solo requiere datos básicos del solicitante.', 0, 0, 0);

-- ------------------------------------------------------------
-- Politicas
-- ------------------------------------------------------------
SET @Tarjeta   = (SELECT ProductoId FROM Productos WHERE Codigo = 'TARJETA_CREDITO');
SET @Hipoteca  = (SELECT ProductoId FROM Productos WHERE Codigo = 'PRESTAMO_HIPOTECARIO');
SET @Personal  = (SELECT ProductoId FROM Productos WHERE Codigo = 'CREDITO_PERSONAL');
SET @Vehicular = (SELECT ProductoId FROM Productos WHERE Codigo = 'CREDITO_VEHICULAR');
SET @General   = (SELECT ProductoId FROM Productos WHERE Codigo = 'GENERAL');

-- Tarjeta de Crédito Clásica
INSERT INTO Politicas (ProductoId, Codigo, Nombre, Descripcion, TipoRegla, ParametrosJson, Orden) VALUES
    (@Tarjeta, 'EDAD_MINIMA',    'Edad Mínima',                           'El solicitante debe ser mayor de edad.',                 'EDAD_MINIMA',          '{"min":18}',                                 1),
    (@Tarjeta, 'LISTA_NEGRA',    'Verificación en Listas de Riesgo SIB',  'El solicitante no debe figurar en listas negras.',       'LISTA_NEGRA_SIB',      '{}',                                         2),
    (@Tarjeta, 'SCORE_BURO',     'Score de Buró de Crédito',              'Score reportado por la Superintendencia de Bancos.',     'SCORE_BURO_MINIMO',    '{"verde":620,"amarillo":560}',               3),
    (@Tarjeta, 'ENDEUDAMIENTO',  'Nivel de Endeudamiento',                'Porcentaje de endeudamiento actual reportado.',          'ENDEUDAMIENTO_MAXIMO', '{"verde":45,"amarillo":65}',                 4),
    (@Tarjeta, 'INGRESO_MINIMO', 'Ingreso Mínimo Mensual (Q3,000)',       'Ingreso mensual declarado por el solicitante.',          'INGRESO_MINIMO',       '{"minimo":3000,"toleranciaPorcentaje":10}',  5),
    (@Tarjeta, 'HISTORIAL_MORAS','Historial de Moras (12 meses)',         'Moras registradas en el historial crediticio reciente.', 'HISTORIAL_MORAS',      '{"verde":0,"amarillo":2}',                   6),
    (@Tarjeta, 'RCI_BANCARIO',   'RCI Bancario (Relación Cuota/Ingreso)', 'Qué tan posible es que el cliente pague un crédito, dividiendo sus deudas estimadas entre su ingreso declarado.', 'RCI_BANCARIO', '{"verde":35,"amarillo":45}', 7);

-- Préstamo Hipotecario
INSERT INTO Politicas (ProductoId, Codigo, Nombre, Descripcion, TipoRegla, ParametrosJson, Orden) VALUES
    (@Hipoteca, 'EDAD_MINIMA',        'Edad Mínima',                          'El solicitante debe ser mayor de edad.',                 'EDAD_MINIMA',               '{"min":21}',                                1),
    (@Hipoteca, 'EDAD_MAXIMA',        'Edad Máxima al Finalizar el Plazo',    'Edad límite considerando el plazo del crédito.',         'EDAD_MAXIMA',               '{"max":70,"amarilloDesde":65}',             2),
    (@Hipoteca, 'LISTA_NEGRA',        'Verificación en Listas de Riesgo SIB', 'El solicitante no debe figurar en listas negras.',       'LISTA_NEGRA_SIB',           '{}',                                        3),
    (@Hipoteca, 'SCORE_BURO',         'Score de Buró de Crédito',             'Score reportado por la Superintendencia de Bancos.',     'SCORE_BURO_MINIMO',         '{"verde":680,"amarillo":620}',              4),
    (@Hipoteca, 'ENDEUDAMIENTO',      'Nivel de Endeudamiento',               'Porcentaje de endeudamiento actual reportado.',          'ENDEUDAMIENTO_MAXIMO',      '{"verde":35,"amarillo":50}',                5),
    (@Hipoteca, 'INGRESO_MINIMO',     'Ingreso Mínimo Mensual (Q8,000)',      'Ingreso mensual declarado por el solicitante.',          'INGRESO_MINIMO',            '{"minimo":8000,"toleranciaPorcentaje":5}',  6),
    (@Hipoteca, 'ANTIGUEDAD_LABORAL', 'Antigüedad Laboral Mínima (24 meses)', 'Tiempo continuo en el empleo actual.',                   'ANTIGUEDAD_LABORAL_MINIMA', '{"verdeMeses":24,"amarilloMeses":12}',      7),
    (@Hipoteca, 'HISTORIAL_MORAS',    'Historial de Moras (12 meses)',        'Moras registradas en el historial crediticio reciente.', 'HISTORIAL_MORAS',           '{"verde":0,"amarillo":1}',                  8);

-- Crédito Personal
INSERT INTO Politicas (ProductoId, Codigo, Nombre, Descripcion, TipoRegla, ParametrosJson, Orden) VALUES
    (@Personal, 'EDAD_MINIMA',     'Edad Mínima',                          'El solicitante debe ser mayor de edad.',                 'EDAD_MINIMA',          '{"min":18}',                                 1),
    (@Personal, 'LISTA_NEGRA',     'Verificación en Listas de Riesgo SIB', 'El solicitante no debe figurar en listas negras.',       'LISTA_NEGRA_SIB',      '{}',                                         2),
    (@Personal, 'SCORE_BURO',      'Score de Buró de Crédito',             'Score reportado por la Superintendencia de Bancos.',     'SCORE_BURO_MINIMO',    '{"verde":600,"amarillo":540}',               3),
    (@Personal, 'ENDEUDAMIENTO',   'Nivel de Endeudamiento',               'Porcentaje de endeudamiento actual reportado.',          'ENDEUDAMIENTO_MAXIMO', '{"verde":50,"amarillo":70}',                 4),
    (@Personal, 'INGRESO_MINIMO',  'Ingreso Mínimo Mensual (Q2,500)',      'Ingreso mensual declarado por el solicitante.',          'INGRESO_MINIMO',       '{"minimo":2500,"toleranciaPorcentaje":10}',  5),
    (@Personal, 'HISTORIAL_MORAS', 'Historial de Moras (12 meses)',        'Moras registradas en el historial crediticio reciente.', 'HISTORIAL_MORAS',      '{"verde":0,"amarillo":3}',                   6);

-- Crédito Vehicular
INSERT INTO Politicas (ProductoId, Codigo, Nombre, Descripcion, TipoRegla, ParametrosJson, Orden) VALUES
    (@Vehicular, 'EDAD_MINIMA',        'Edad Mínima',                          'El solicitante debe ser mayor de edad.',                 'EDAD_MINIMA',               '{"min":21}',                                1),
    (@Vehicular, 'LISTA_NEGRA',        'Verificación en Listas de Riesgo SIB', 'El solicitante no debe figurar en listas negras.',       'LISTA_NEGRA_SIB',           '{}',                                        2),
    (@Vehicular, 'SCORE_BURO',         'Score de Buró de Crédito',             'Score reportado por la Superintendencia de Bancos.',     'SCORE_BURO_MINIMO',         '{"verde":640,"amarillo":580}',              3),
    (@Vehicular, 'ENDEUDAMIENTO',      'Nivel de Endeudamiento',               'Porcentaje de endeudamiento actual reportado.',          'ENDEUDAMIENTO_MAXIMO',      '{"verde":40,"amarillo":60}',                4),
    (@Vehicular, 'INGRESO_MINIMO',     'Ingreso Mínimo Mensual (Q4,500)',      'Ingreso mensual declarado por el solicitante.',          'INGRESO_MINIMO',            '{"minimo":4500,"toleranciaPorcentaje":10}', 5),
    (@Vehicular, 'ANTIGUEDAD_LABORAL', 'Antigüedad Laboral Mínima (12 meses)', 'Tiempo continuo en el empleo actual.',                   'ANTIGUEDAD_LABORAL_MINIMA', '{"verdeMeses":12,"amarilloMeses":6}',       6);

-- Precalificación General (usado por Evaluaciones Batch)
INSERT INTO Politicas (ProductoId, Codigo, Nombre, Descripcion, TipoRegla, ParametrosJson, Orden) VALUES
    (@General, 'EDAD_MINIMA',     'Edad Mínima',                          'El solicitante debe ser mayor de edad.',                 'EDAD_MINIMA',          '{"min":18}',                  1),
    (@General, 'LISTA_NEGRA',     'Verificación en Listas de Riesgo SIB', 'El solicitante no debe figurar en listas negras.',       'LISTA_NEGRA_SIB',      '{}',                          2),
    (@General, 'SCORE_BURO',      'Score de Buró de Crédito',             'Score reportado por la Superintendencia de Bancos.',     'SCORE_BURO_MINIMO',    '{"verde":600,"amarillo":540}',3),
    (@General, 'ENDEUDAMIENTO',   'Nivel de Endeudamiento',               'Porcentaje de endeudamiento actual reportado.',          'ENDEUDAMIENTO_MAXIMO', '{"verde":50,"amarillo":70}',  4),
    (@General, 'HISTORIAL_MORAS', 'Historial de Moras (12 meses)',        'Moras registradas en el historial crediticio reciente.', 'HISTORIAL_MORAS',      '{"verde":0,"amarillo":2}',    5);
