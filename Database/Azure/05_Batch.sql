/*
====================================================================
 CreditPulse - Script 05: Evaluaciones Batch (Fase 2)
 Motor: Microsoft SQL Server Express 2022
====================================================================
 Ejecutar DESPUÉS de 01_CreateDatabase.sql .. 04_SeedData.sql.
 Es incremental e idempotente: si ya tienes la base de datos de la
 Fase 1 corriendo, solo necesitas correr este script nuevo (no hace
 falta recrear nada de lo anterior).

 Contenido:
   - Producto "General": para precalificación masiva. Sus políticas
     solo dependen de datos básicos (nombre, NIT, DPI, fecha de
     nacimiento) + lo que devuelve la SIB, así el Excel de carga
     batch tiene siempre las mismas columnas sin importar qué tan
     distintos sean los demás productos de la app.
   - Lotes / LoteEvaluaciones: encabezado y detalle de cada carga
     batch. Una fila de Excel que falla validación (DPI vacío, fecha
     inválida, etc.) se guarda igual en LoteEvaluaciones con su
     mensaje de error, sin tener una Evaluacion asociada, para que el
     reporte del lote muestre TODAS las filas del archivo.
*/
-- (Azure SQL Database no soporta USE para cambiar de base: la conexión
-- ya está fija a la base elegida al conectar. Línea 'USE CreditPulseDB;'
-- removida para esta variante; el resto del script es idéntico.)
GO

------------------------------------------------------------
-- Producto "General" (precalificación batch)
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Productos WHERE Codigo = N'GENERAL')
BEGIN
    INSERT INTO dbo.Productos (Codigo, Nombre, Descripcion, RequiereIngresos, RequiereTipoEmpleo, RequiereAntiguedadLaboral)
    VALUES (N'GENERAL', N'Precalificación General', N'Producto usado por Evaluaciones Batch: solo requiere datos básicos del solicitante.', 0, 0, 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Politicas WHERE ProductoId = (SELECT ProductoId FROM dbo.Productos WHERE Codigo = N'GENERAL'))
BEGIN
    DECLARE @General INT = (SELECT ProductoId FROM dbo.Productos WHERE Codigo = N'GENERAL');

    INSERT INTO dbo.Politicas (ProductoId, Codigo, Nombre, Descripcion, TipoRegla, ParametrosJson, Orden) VALUES
        (@General, N'EDAD_MINIMA',    N'Edad Mínima',                          N'El solicitante debe ser mayor de edad.',                N'EDAD_MINIMA',          N'{"min":18}',                    1),
        (@General, N'LISTA_NEGRA',     N'Verificación en Listas de Riesgo SIB', N'El solicitante no debe figurar en listas negras.',      N'LISTA_NEGRA_SIB',      N'{}',                            2),
        (@General, N'SCORE_BURO',      N'Score de Buró de Crédito',             N'Score reportado por la Superintendencia de Bancos.',    N'SCORE_BURO_MINIMO',    N'{"verde":600,"amarillo":540}', 3),
        (@General, N'ENDEUDAMIENTO',   N'Nivel de Endeudamiento',               N'Porcentaje de endeudamiento actual reportado.',         N'ENDEUDAMIENTO_MAXIMO', N'{"verde":50,"amarillo":70}',   4),
        (@General, N'HISTORIAL_MORAS', N'Historial de Moras (12 meses)',        N'Moras registradas en el historial crediticio reciente.',N'HISTORIAL_MORAS',      N'{"verde":0,"amarillo":2}',     5);
END
GO

------------------------------------------------------------
-- Lotes (encabezado de una carga batch)
------------------------------------------------------------
IF OBJECT_ID('dbo.Lotes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Lotes (
        LoteId           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Lotes PRIMARY KEY,
        NombreArchivo    NVARCHAR(255) NOT NULL,
        UsuarioId        INT NOT NULL,
        TotalRegistros   INT NOT NULL CONSTRAINT DF_Lotes_TotalRegistros DEFAULT (0),
        TotalVerde       INT NOT NULL CONSTRAINT DF_Lotes_TotalVerde DEFAULT (0),
        TotalAmarillo    INT NOT NULL CONSTRAINT DF_Lotes_TotalAmarillo DEFAULT (0),
        TotalRojo        INT NOT NULL CONSTRAINT DF_Lotes_TotalRojo DEFAULT (0),
        TotalErrores     INT NOT NULL CONSTRAINT DF_Lotes_TotalErrores DEFAULT (0),
        FechaCreacion    DATETIME2(0) NOT NULL CONSTRAINT DF_Lotes_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Lotes_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId)
    );

    CREATE INDEX IX_Lotes_FechaCreacion ON dbo.Lotes (FechaCreacion DESC);
END
GO

------------------------------------------------------------
-- LoteEvaluaciones (una fila del Excel = una fila aquí)
------------------------------------------------------------
IF OBJECT_ID('dbo.LoteEvaluaciones', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoteEvaluaciones (
        LoteEvaluacionId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LoteEvaluaciones PRIMARY KEY,
        LoteId                  INT NOT NULL,
        EvaluacionId             INT NULL,               -- NULL si la fila falló antes de poder evaluarse
        FilaExcel                 INT NOT NULL,
        NombreCompletoOriginal   NVARCHAR(300) NULL,
        NitOriginal               NVARCHAR(20) NULL,
        DpiOriginal               NVARCHAR(20) NULL,
        Estado                     NVARCHAR(20) NOT NULL, -- 'Evaluado' | 'Error'
        MensajeError               NVARCHAR(400) NULL,
        CONSTRAINT FK_LoteEvaluaciones_Lotes FOREIGN KEY (LoteId) REFERENCES dbo.Lotes (LoteId),
        CONSTRAINT FK_LoteEvaluaciones_Evaluaciones FOREIGN KEY (EvaluacionId) REFERENCES dbo.Evaluaciones (EvaluacionId),
        CONSTRAINT CK_LoteEvaluaciones_Estado CHECK (Estado IN ('Evaluado','Error'))
    );

    CREATE INDEX IX_LoteEvaluaciones_LoteId ON dbo.LoteEvaluaciones (LoteId);
END
GO

------------------------------------------------------------
-- Procedimientos almacenados
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Lotes_Crear
    @NombreArchivo   NVARCHAR(255),
    @UsuarioId       INT,
    @TotalRegistros  INT,
    @TotalVerde      INT,
    @TotalAmarillo   INT,
    @TotalRojo       INT,
    @TotalErrores    INT,
    @LoteId          INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Lotes (NombreArchivo, UsuarioId, TotalRegistros, TotalVerde, TotalAmarillo, TotalRojo, TotalErrores)
    VALUES (@NombreArchivo, @UsuarioId, @TotalRegistros, @TotalVerde, @TotalAmarillo, @TotalRojo, @TotalErrores);

    SET @LoteId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LoteEvaluaciones_Insertar
    @LoteId                  INT,
    @EvaluacionId             INT = NULL,
    @FilaExcel                 INT,
    @NombreCompletoOriginal   NVARCHAR(300) = NULL,
    @NitOriginal               NVARCHAR(20) = NULL,
    @DpiOriginal               NVARCHAR(20) = NULL,
    @Estado                     NVARCHAR(20),
    @MensajeError               NVARCHAR(400) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.LoteEvaluaciones
        (LoteId, EvaluacionId, FilaExcel, NombreCompletoOriginal, NitOriginal, DpiOriginal, Estado, MensajeError)
    VALUES
        (@LoteId, @EvaluacionId, @FilaExcel, @NombreCompletoOriginal, @NitOriginal, @DpiOriginal, @Estado, @MensajeError);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Lotes_ObtenerPorId
    @LoteId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  l.LoteId, l.NombreArchivo, l.UsuarioId, u.NombreCompleto AS AnalistaNombre,
            l.TotalRegistros, l.TotalVerde, l.TotalAmarillo, l.TotalRojo, l.TotalErrores, l.FechaCreacion
    FROM    dbo.Lotes l
    INNER JOIN dbo.Usuarios u ON u.UsuarioId = l.UsuarioId
    WHERE   l.LoteId = @LoteId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LoteEvaluaciones_ObtenerPorLote
    @LoteId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  le.LoteEvaluacionId, le.LoteId, le.EvaluacionId, le.FilaExcel,
            le.NombreCompletoOriginal, le.NitOriginal, le.DpiOriginal, le.Estado, le.MensajeError,
            e.ResultadoGeneral
    FROM    dbo.LoteEvaluaciones le
    LEFT JOIN dbo.Evaluaciones e ON e.EvaluacionId = le.EvaluacionId
    WHERE   le.LoteId = @LoteId
    ORDER BY le.FilaExcel;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Productos_ObtenerPorCodigo
    @Codigo NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ProductoId, Codigo, Nombre, Descripcion,
           RequiereIngresos, RequiereTipoEmpleo, RequiereAntiguedadLaboral
    FROM   dbo.Productos
    WHERE  Codigo = @Codigo
      AND  Activo = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Lotes_ObtenerRecientes
    @Top INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top)
            l.LoteId, l.NombreArchivo, u.NombreCompleto AS AnalistaNombre,
            l.TotalRegistros, l.TotalVerde, l.TotalAmarillo, l.TotalRojo, l.TotalErrores, l.FechaCreacion
    FROM    dbo.Lotes l
    INNER JOIN dbo.Usuarios u ON u.UsuarioId = l.UsuarioId
    ORDER BY l.FechaCreacion DESC;
END
GO
