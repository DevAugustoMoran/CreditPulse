/*
====================================================================
 CreditPulse - Script 09: Batch asíncrono y en segundo plano
====================================================================
 Ejecutar DESPUÉS de 01..08. Es incremental.

 Motivación (ver README.md, sección de límites conocidos, para el
 análisis completo): con el flujo síncrono original, subir un Excel de
 10,000 clientes hacía ~8 idas y vueltas a SQL Server POR FILA (2
 lecturas de Producto/Políticas que nunca cambian durante el lote, más
 ~6 escrituras: 1 encabezado de Evaluación + 5 detalles de política
 para el producto "General"), es decir, unos 80,000 round trips solo
 para evaluar, TODO dentro de una sola solicitud HTTP, sin límite de
 tiempo visible para el usuario y sin ninguna forma de recuperarse de
 una caída a medio camino: el Lote (encabezado del reporte) recién se
 guardaba al final, así que un fallo a mitad de proceso dejaba miles
 de Evaluaciones ya escritas en la base de datos pero sin ningún Lote
 que las agrupara ("huérfanas"), y el usuario no veía ningún reporte.

 Este script agrega lo necesario para:
   1) Crear el Lote de inmediato, en estado 'Procesando', ANTES de
      evaluar la primera fila (BatchEvaluationService.ProcesarArchivo
      ahora corre en segundo plano -ver HostingEnvironment.QueueBackgroundWorkItem
      en Batch.aspx.cs-, así que la solicitud HTTP original ya no
      espera a que termine todo el archivo).
   2) Ir guardando el progreso en bloques (chunks), no todo al final:
      si el proceso se cae a mitad de camino, las filas que ya se
      guardaron quedan correctamente asociadas a su Lote, y el usuario
      ve un reporte parcial en vez de nada.
   3) Reducir drásticamente los round trips por fila con 2 técnicas:
        a) Producto y Políticas se consultan UNA sola vez por lote
           (BatchEvaluationService ya no las vuelve a pedir en cada
           fila).
        b) Los detalles de política de una evaluación (antes: un
           INSERT por política, ~5 por fila) y las filas de
           LoteEvaluaciones de un bloque (antes: un INSERT por fila)
           se insertan con Table-Valued Parameters (TVP): un solo
           viaje a SQL Server inserta muchas filas a la vez.
      El INSERT del encabezado de cada Evaluación se deja fila por
      fila (no se combina en un solo TVP multi-fila) porque cada
      evaluación necesita su propio EvaluacionId antes de poder
      guardar sus políticas; combinarlo también es posible (con
      MERGE...OUTPUT) pero se dejó fuera de este script a propósito,
      documentado como una optimización futura en el README, para no
      arriesgar una migración compleja sin un motor real donde
      probarla.
*/
-- (Azure SQL Database no soporta USE para cambiar de base: la conexión
-- ya está fija a la base elegida al conectar. Línea 'USE CreditPulseDB;'
-- removida para esta variante; el resto del script es idéntico.)
GO

------------------------------------------------------------
-- Lotes: estado y progreso
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Lotes') AND name = 'Estado')
BEGIN
    -- DEFAULT 'Completado' aplica también a los lotes que ya existan de
    -- antes de este script: se crearon con el flujo síncrono, que solo
    -- terminaba de insertar el Lote cuando el archivo ya estaba 100%
    -- procesado, así que "Completado" es correcto para ellos.
    ALTER TABLE dbo.Lotes ADD
        Estado            NVARCHAR(20) NOT NULL CONSTRAINT DF_Lotes_Estado DEFAULT ('Completado'),
        TotalProcesados   INT NOT NULL CONSTRAINT DF_Lotes_TotalProcesados DEFAULT (0),
        MensajeError      NVARCHAR(400) NULL;
END
GO

-- Instrucciones aparte (después de un GO nuevo) a propósito: SQL Server
-- compila TODO un batch antes de ejecutar cualquiera de sus
-- instrucciones. Si el CHECK y el UPDATE de abajo estuvieran en el
-- mismo batch que el ALTER TABLE ADD de arriba, el compilador
-- rechazaría la referencia a "Estado"/"TotalProcesados" con "Invalid
-- column name" porque, en el momento de compilar, esas columnas
-- todavía no existen en el catálogo -aunque la instrucción de arriba
-- las vaya a crear en la misma ejecución del script-, y el batch
-- entero fallaría sin ejecutar nada (ni siquiera el ALTER TABLE ADD).
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Lotes_Estado')
BEGIN
    ALTER TABLE dbo.Lotes ADD CONSTRAINT CK_Lotes_Estado CHECK (Estado IN ('Procesando', 'Completado', 'Error'));
END
GO

UPDATE dbo.Lotes SET TotalProcesados = TotalRegistros WHERE TotalProcesados = 0 AND TotalRegistros > 0;
GO

------------------------------------------------------------
-- Tipos de tabla (Table-Valued Parameters) para inserciones por lote
------------------------------------------------------------
IF TYPE_ID(N'dbo.EvaluacionPoliticaTVP') IS NULL
BEGIN
    EXEC('CREATE TYPE dbo.EvaluacionPoliticaTVP AS TABLE (
        PoliticaId      INT NULL,
        NombrePolitica  NVARCHAR(150) NOT NULL,
        Resultado       NVARCHAR(10) NOT NULL,
        Detalle         NVARCHAR(400) NULL,
        Orden           INT NOT NULL
    )');
END
GO

IF TYPE_ID(N'dbo.LoteEvaluacionTVP') IS NULL
BEGIN
    EXEC('CREATE TYPE dbo.LoteEvaluacionTVP AS TABLE (
        EvaluacionId            INT NULL,
        FilaExcel                INT NOT NULL,
        NombreCompletoOriginal   NVARCHAR(300) NULL,
        NitOriginal              NVARCHAR(20) NULL,
        DpiOriginal              NVARCHAR(20) NULL,
        Estado                   NVARCHAR(20) NOT NULL,
        MensajeError             NVARCHAR(400) NULL
    )');
END
GO

------------------------------------------------------------
-- Inserciones masivas (bulk) vía TVP
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_EvaluacionPoliticas_InsertarLote
    @EvaluacionId INT,
    @Filas        dbo.EvaluacionPoliticaTVP READONLY
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.EvaluacionPoliticas (EvaluacionId, PoliticaId, NombrePolitica, Resultado, Detalle, Orden)
    SELECT @EvaluacionId, PoliticaId, NombrePolitica, Resultado, Detalle, Orden
    FROM   @Filas;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_LoteEvaluaciones_InsertarLote
    @LoteId INT,
    @Filas  dbo.LoteEvaluacionTVP READONLY
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.LoteEvaluaciones (LoteId, EvaluacionId, FilaExcel, NombreCompletoOriginal, NitOriginal, DpiOriginal, Estado, MensajeError)
    SELECT @LoteId, EvaluacionId, FilaExcel, NombreCompletoOriginal, NitOriginal, DpiOriginal, Estado, MensajeError
    FROM   @Filas;
END
GO

------------------------------------------------------------
-- Ciclo de vida del Lote en segundo plano
------------------------------------------------------------

-- Se llama ANTES de evaluar la primera fila (ver Batch.aspx.cs):
-- crea el encabezado de inmediato, en estado 'Procesando', para que el
-- reporte exista y sea visible desde el primer momento.
CREATE OR ALTER PROCEDURE dbo.sp_Lotes_CrearPendiente
    @NombreArchivo   NVARCHAR(255),
    @UsuarioId       INT,
    @TotalRegistros  INT,
    @LoteId          INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Lotes (NombreArchivo, UsuarioId, TotalRegistros, Estado, TotalProcesados)
    VALUES (@NombreArchivo, @UsuarioId, @TotalRegistros, 'Procesando', 0);

    SET @LoteId = SCOPE_IDENTITY();
END
GO

-- Se llama al terminar cada bloque (chunk) de filas, para que el
-- reporte muestre avance real mientras el resto todavía se procesa.
CREATE OR ALTER PROCEDURE dbo.sp_Lotes_ActualizarProgreso
    @LoteId          INT,
    @TotalProcesados INT,
    @TotalVerde      INT,
    @TotalAmarillo   INT,
    @TotalRojo       INT,
    @TotalErrores    INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Lotes
    SET TotalProcesados = @TotalProcesados,
        TotalVerde = @TotalVerde,
        TotalAmarillo = @TotalAmarillo,
        TotalRojo = @TotalRojo,
        TotalErrores = @TotalErrores
    WHERE LoteId = @LoteId;
END
GO

-- Cierra el Lote en un estado terminal: 'Completado' cuando ya se
-- procesaron todas las filas, o 'Error' si el proceso en segundo plano
-- se interrumpió de forma irrecuperable (ver catch-all en
-- BatchEvaluationService.ProcesarEnSegundoPlano). Las filas que ya se
-- hayan guardado en LoteEvaluaciones antes del error NO se pierden:
-- siguen asociadas a este Lote y se siguen mostrando en el reporte.
CREATE OR ALTER PROCEDURE dbo.sp_Lotes_CambiarEstado
    @LoteId       INT,
    @Estado       NVARCHAR(20),
    @MensajeError NVARCHAR(400) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Lotes
    SET Estado = @Estado,
        MensajeError = @MensajeError
    WHERE LoteId = @LoteId;
END
GO

------------------------------------------------------------
-- Lecturas: ahora incluyen Estado / TotalProcesados / MensajeError
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Lotes_ObtenerPorId
    @LoteId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  l.LoteId, l.NombreArchivo, l.UsuarioId, u.NombreCompleto AS AnalistaNombre,
            l.TotalRegistros, l.TotalProcesados, l.TotalVerde, l.TotalAmarillo, l.TotalRojo, l.TotalErrores,
            l.Estado, l.MensajeError, l.FechaCreacion
    FROM    dbo.Lotes l
    INNER JOIN dbo.Usuarios u ON u.UsuarioId = l.UsuarioId
    WHERE   l.LoteId = @LoteId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Lotes_ObtenerRecientes
    @Top INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top)
            l.LoteId, l.NombreArchivo, u.NombreCompleto AS AnalistaNombre,
            l.TotalRegistros, l.TotalProcesados, l.TotalVerde, l.TotalAmarillo, l.TotalRojo, l.TotalErrores,
            l.Estado, l.MensajeError, l.FechaCreacion
    FROM    dbo.Lotes l
    INNER JOIN dbo.Usuarios u ON u.UsuarioId = l.UsuarioId
    ORDER BY l.FechaCreacion DESC;
END
GO
