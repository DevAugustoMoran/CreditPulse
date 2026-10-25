/*
====================================================================
 CreditPulse - Script 03: Procedimientos almacenados
 Motor: Microsoft SQL Server Express 2022
====================================================================
 RNF03 (Arquitectura de Datos Optimizada) exige que las consultas
 principales del motor de evaluación pasen por procedimientos
 almacenados en lugar de SQL dinámico embebido en la aplicación.
*/
-- (Azure SQL Database no soporta USE para cambiar de base: la conexión
-- ya está fija a la base elegida al conectar. Línea 'USE CreditPulseDB;'
-- removida para esta variante; el resto del script es idéntico.)
GO

------------------------------------------------------------
-- Autenticación
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Usuarios_ObtenerPorEmail
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  u.UsuarioId,
            u.Email,
            u.PasswordHash,
            u.PasswordSalt,
            u.NombreCompleto,
            u.RolId,
            r.Nombre AS RolNombre,
            u.Activo,
            u.IntentosFallidos
    FROM    dbo.Usuarios u
    INNER JOIN dbo.Roles r ON r.RolId = u.RolId
    WHERE   u.Email = @Email;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuarios_RegistrarLoginExitoso
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Usuarios
    SET UltimoLogin = SYSUTCDATETIME(),
        IntentosFallidos = 0
    WHERE UsuarioId = @UsuarioId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuarios_RegistrarLoginFallido
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Usuarios
    SET IntentosFallidos = IntentosFallidos + 1
    WHERE Email = @Email;
END
GO

------------------------------------------------------------
-- Productos
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Productos_ObtenerActivos
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ProductoId, Codigo, Nombre, Descripcion,
           RequiereIngresos, RequiereTipoEmpleo, RequiereAntiguedadLaboral
    FROM   dbo.Productos
    WHERE  Activo = 1
    ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Productos_ObtenerPorId
    @ProductoId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ProductoId, Codigo, Nombre, Descripcion,
           RequiereIngresos, RequiereTipoEmpleo, RequiereAntiguedadLaboral
    FROM   dbo.Productos
    WHERE  ProductoId = @ProductoId;
END
GO

------------------------------------------------------------
-- Politicas
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Politicas_ObtenerPorProducto
    @ProductoId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT PoliticaId, ProductoId, Codigo, Nombre, Descripcion,
           TipoRegla, ParametrosJson, Orden
    FROM   dbo.Politicas
    WHERE  ProductoId = @ProductoId
      AND  Activo = 1
    ORDER BY Orden, PoliticaId;
END
GO

------------------------------------------------------------
-- Evaluaciones
------------------------------------------------------------
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
    @EvaluacionId             INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Evaluaciones
        (ProductoId, UsuarioId, PrimerNombre, SegundoNombre, PrimerApellido, SegundoApellido,
         Nit, Dpi, FechaNacimiento, IngresosMensuales, TipoEmpleo, AntiguedadLaboralMeses,
         ResultadoGeneral, SibRawXml, SibScoreBuro, SibNivelEndeudamiento, SibEnListaNegra)
    VALUES
        (@ProductoId, @UsuarioId, @PrimerNombre, @SegundoNombre, @PrimerApellido, @SegundoApellido,
         @Nit, @Dpi, @FechaNacimiento, @IngresosMensuales, @TipoEmpleo, @AntiguedadLaboralMeses,
         @ResultadoGeneral, @SibRawXml, @SibScoreBuro, @SibNivelEndeudamiento, @SibEnListaNegra);

    SET @EvaluacionId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_EvaluacionPoliticas_Insertar
    @EvaluacionId    INT,
    @PoliticaId      INT = NULL,
    @NombrePolitica  NVARCHAR(150),
    @Resultado       NVARCHAR(10),
    @Detalle         NVARCHAR(400) = NULL,
    @Orden           INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.EvaluacionPoliticas
        (EvaluacionId, PoliticaId, NombrePolitica, Resultado, Detalle, Orden)
    VALUES
        (@EvaluacionId, @PoliticaId, @NombrePolitica, @Resultado, @Detalle, @Orden);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Evaluaciones_ObtenerPorId
    @EvaluacionId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  e.EvaluacionId, e.ProductoId, p.Nombre AS ProductoNombre,
            e.UsuarioId, u.NombreCompleto AS AnalistaNombre,
            e.PrimerNombre, e.SegundoNombre, e.PrimerApellido, e.SegundoApellido,
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

CREATE OR ALTER PROCEDURE dbo.sp_EvaluacionPoliticas_ObtenerPorEvaluacion
    @EvaluacionId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT EvaluacionPoliticaId, EvaluacionId, PoliticaId, NombrePolitica, Resultado, Detalle, Orden
    FROM   dbo.EvaluacionPoliticas
    WHERE  EvaluacionId = @EvaluacionId
    ORDER BY Orden, EvaluacionPoliticaId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Evaluaciones_ObtenerRecientes
    @Top INT = 15
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top)
            e.EvaluacionId,
            e.PrimerNombre + ' ' + e.PrimerApellido AS ClienteNombre,
            p.Nombre AS ProductoNombre,
            e.ResultadoGeneral,
            e.FechaCreacion
    FROM    dbo.Evaluaciones e
    INNER JOIN dbo.Productos p ON p.ProductoId = e.ProductoId
    ORDER BY e.FechaCreacion DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Evaluaciones_ObtenerResumenDashboard
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(*)                                                   AS TotalEvaluaciones,
        SUM(CASE WHEN ResultadoGeneral = 'VERDE'    THEN 1 ELSE 0 END) AS TotalVerde,
        SUM(CASE WHEN ResultadoGeneral = 'AMARILLO' THEN 1 ELSE 0 END) AS TotalAmarillo,
        SUM(CASE WHEN ResultadoGeneral = 'ROJO'     THEN 1 ELSE 0 END) AS TotalRojo
    FROM dbo.Evaluaciones;
END
GO
