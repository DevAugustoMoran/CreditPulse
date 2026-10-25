/*
====================================================================
 CreditPulse - Script 13: Módulo de reportes
====================================================================
 Ejecutar DESPUÉS de 01..12. Es incremental e idempotente.

 Solo agrega procedimientos almacenados de LECTURA (no toca tablas ni
 datos existentes):

   1) sp_Reporte_Evaluaciones / sp_Reporte_Evaluaciones_Resumen
        Evaluaciones de un rango de fechas (la aplicación manda el mes
        completo): color, cliente, score crediticio y fecha.
   2) sp_Reporte_Batch
        Clientes de un lote (batch): datos del cliente, información
        recolectada de la SIB, número de lote y fecha de evaluación.

 Las fechas se guardan en UTC. La aplicación calcula los límites del
 mes en hora de Guatemala, los convierte a UTC y los manda como
 @Desde (inclusive) y @Hasta (exclusivo).
*/
-- (Azure SQL Database no soporta USE para cambiar de base: la conexión
-- ya está fija a la base elegida al conectar. Línea 'USE CreditPulseDB;'
-- removida para esta variante; el resto del script es idéntico.)
GO

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_Evaluaciones
    @Desde      DATETIME2(0),
    @Hasta      DATETIME2(0),
    @Resultado  NVARCHAR(10) = NULL,          -- NULL = todos; 'VERDE' | 'AMARILLO' | 'ROJO'
    @Offset     INT = 0,
    @Limit      INT = 2147483647
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  e.EvaluacionId,
            CONCAT(e.PrimerNombre, ' ' + e.SegundoNombre, ' ' + e.TercerNombre,
                   ' ' + e.PrimerApellido, ' ' + e.SegundoApellido) AS ClienteNombre,
            e.Dpi,
            p.Nombre AS ProductoNombre,
            e.ResultadoGeneral,
            e.SibScoreBuro,
            e.FechaCreacion
    FROM    dbo.Evaluaciones e
    INNER JOIN dbo.Productos p ON p.ProductoId = e.ProductoId
    WHERE   e.FechaCreacion >= @Desde
      AND   e.FechaCreacion <  @Hasta
      AND   (@Resultado IS NULL OR e.ResultadoGeneral = @Resultado)
    ORDER BY e.FechaCreacion DESC, e.EvaluacionId DESC
    OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_Evaluaciones_Resumen
    @Desde      DATETIME2(0),
    @Hasta      DATETIME2(0),
    @Resultado  NVARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  COUNT(*)                                                       AS Total,
            COALESCE(SUM(CASE WHEN e.ResultadoGeneral = 'VERDE'    THEN 1 ELSE 0 END), 0) AS TotalVerde,
            COALESCE(SUM(CASE WHEN e.ResultadoGeneral = 'AMARILLO' THEN 1 ELSE 0 END), 0) AS TotalAmarillo,
            COALESCE(SUM(CASE WHEN e.ResultadoGeneral = 'ROJO'     THEN 1 ELSE 0 END), 0) AS TotalRojo,
            AVG(CAST(e.SibScoreBuro AS FLOAT))                             AS ScorePromedio
    FROM    dbo.Evaluaciones e
    WHERE   e.FechaCreacion >= @Desde
      AND   e.FechaCreacion <  @Hasta
      AND   (@Resultado IS NULL OR e.ResultadoGeneral = @Resultado);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Reporte_Batch
    @LoteId  INT,
    @Offset  INT = 0,
    @Limit   INT = 2147483647
AS
BEGIN
    SET NOCOUNT ON;

    -- LEFT JOIN: las filas del Excel que fallaron validación (sin
    -- evaluación asociada) también aparecen, con su motivo de error.
    SELECT  le.LoteId,
            le.FilaExcel,
            le.Estado          AS EstadoFila,
            le.MensajeError,
            le.EvaluacionId,
            le.NombreCompletoOriginal,
            le.NitOriginal,
            le.DpiOriginal,
            CONCAT(e.PrimerNombre, ' ' + e.SegundoNombre, ' ' + e.TercerNombre,
                   ' ' + e.PrimerApellido, ' ' + e.SegundoApellido) AS ClienteNombre,
            e.Nit,
            e.Dpi,
            e.FechaNacimiento,
            p.Nombre           AS ProductoNombre,
            e.ResultadoGeneral,
            e.SibScoreBuro,
            e.SibNivelEndeudamiento,
            e.SibEnListaNegra,
            e.SibRawXml,
            e.FechaCreacion    AS FechaEvaluacion
    FROM    dbo.LoteEvaluaciones le
    LEFT JOIN dbo.Evaluaciones e ON e.EvaluacionId = le.EvaluacionId
    LEFT JOIN dbo.Productos    p ON p.ProductoId   = e.ProductoId
    WHERE   le.LoteId = @LoteId
    ORDER BY le.FilaExcel
    OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Lotes_Listar
    @Top INT = 200
AS
BEGIN
    SET NOCOUNT ON;

    -- Para el selector de lote del reporte: más recientes primero.
    SELECT TOP (@Top)
            l.LoteId, l.NombreArchivo, u.NombreCompleto AS AnalistaNombre,
            l.TotalRegistros, l.TotalProcesados, l.TotalVerde, l.TotalAmarillo, l.TotalRojo, l.TotalErrores,
            l.Estado, l.MensajeError, l.FechaCreacion
    FROM    dbo.Lotes l
    INNER JOIN dbo.Usuarios u ON u.UsuarioId = l.UsuarioId
    ORDER BY l.LoteId DESC;
END
GO
