/*
====================================================================
 CreditPulse - Script 12: Tercer nombre del cliente
====================================================================
 Ejecutar DESPUÉS de 01..11. Es incremental e idempotente (se puede
 ejecutar más de una vez sin dañar nada).

 Agrega la columna opcional Evaluaciones.TercerNombre, para clientes
 con tres nombres (por ejemplo "José Luis Armando Pérez López").

 Compatibilidad con las APIs de integración (/api/v1/...): las APIs no
 envían ni devuelven el tercer nombre, así que NO cambian. El parámetro
 nuevo del procedimiento almacenado tiene valor por defecto NULL: una
 llamada que no lo envía se comporta exactamente igual que antes.
 Las evaluaciones ya existentes quedan con TercerNombre = NULL.
*/
USE CreditPulseDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Evaluaciones') AND name = 'TercerNombre')
BEGIN
    ALTER TABLE dbo.Evaluaciones ADD TercerNombre NVARCHAR(100) NULL;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Evaluaciones_Crear
    @ProductoId               INT,
    @UsuarioId                INT,
    @PrimerNombre             NVARCHAR(100),
    @SegundoNombre            NVARCHAR(100) = NULL,
    @PrimerApellido           NVARCHAR(100),
    @SegundoApellido          NVARCHAR(100) = NULL,
    @Nit                      NVARCHAR(20),
    @Dpi                      NVARCHAR(20),
    @FechaNacimiento          DATE,
    @IngresosMensuales        DECIMAL(12,2) = NULL,
    @TipoEmpleo               NVARCHAR(150) = NULL,
    @AntiguedadLaboralMeses   INT = NULL,
    @ResultadoGeneral         NVARCHAR(10),
    @SibRawXml                XML = NULL,
    @SibScoreBuro             INT = NULL,
    @SibNivelEndeudamiento    DECIMAL(5,2) = NULL,
    @SibEnListaNegra          BIT = NULL,
    @EvaluacionId             INT OUTPUT,
    @TercerNombre             NVARCHAR(100) = NULL   -- nuevo, opcional (al final: no altera llamadas existentes)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Evaluaciones
        (ProductoId, UsuarioId, PrimerNombre, SegundoNombre, TercerNombre, PrimerApellido, SegundoApellido,
         Nit, Dpi, FechaNacimiento, IngresosMensuales, TipoEmpleo, AntiguedadLaboralMeses,
         ResultadoGeneral, SibRawXml, SibScoreBuro, SibNivelEndeudamiento, SibEnListaNegra)
    VALUES
        (@ProductoId, @UsuarioId, @PrimerNombre, @SegundoNombre, @TercerNombre, @PrimerApellido, @SegundoApellido,
         @Nit, @Dpi, @FechaNacimiento, @IngresosMensuales, @TipoEmpleo, @AntiguedadLaboralMeses,
         @ResultadoGeneral, @SibRawXml, @SibScoreBuro, @SibNivelEndeudamiento, @SibEnListaNegra);

    SET @EvaluacionId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Evaluaciones_ObtenerPorId
    @EvaluacionId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  e.EvaluacionId, e.ProductoId, p.Nombre AS ProductoNombre,
            e.UsuarioId, u.NombreCompleto AS AnalistaNombre,
            e.PrimerNombre, e.SegundoNombre, e.TercerNombre, e.PrimerApellido, e.SegundoApellido,
            e.Nit, e.Dpi, e.FechaNacimiento, e.IngresosMensuales, e.TipoEmpleo,
            e.AntiguedadLaboralMeses, e.ResultadoGeneral, e.Estado,
            e.SibRawXml, e.SibScoreBuro, e.SibNivelEndeudamiento, e.SibEnListaNegra,
            e.FechaCreacion
    FROM    dbo.Evaluaciones e
    INNER JOIN dbo.Productos p ON p.ProductoId = e.ProductoId
    INNER JOIN dbo.Usuarios  u ON u.UsuarioId  = e.UsuarioId
    WHERE   e.EvaluacionId = @EvaluacionId;
END
GO
