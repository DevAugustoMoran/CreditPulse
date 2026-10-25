/*
====================================================================
 CreditPulse - Script 06: Administración de Productos y Políticas
 Motor: Microsoft SQL Server Express 2022
====================================================================
 Ejecutar DESPUÉS de 01..05. Es incremental: solo agrega stored
 procedures nuevos, no crea ni modifica tablas (Productos y Politicas
 ya existen desde 02_CreateTables.sql).

 Estos procedimientos alimentan las pantallas Administracion/Productos.aspx
 y Administracion/Politicas.aspx, que permiten dar de alta/editar
 productos y sus políticas sin tocar la base de datos a mano.

 Nota: a diferencia de sp_Productos_ObtenerActivos / sp_Productos_ObtenerPorId
 (que solo devuelven registros con Activo = 1, porque los usa el flujo de
 evaluación), las versiones "...Admin" de este script devuelven TODOS los
 registros, para que el administrador también pueda ver y reactivar algo
 que desactivó por error.
*/
USE CreditPulseDB;
GO

------------------------------------------------------------
-- Productos (administración)
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Productos_ObtenerTodosAdmin
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  p.ProductoId, p.Codigo, p.Nombre, p.Descripcion,
            p.RequiereIngresos, p.RequiereTipoEmpleo, p.RequiereAntiguedadLaboral,
            p.Activo, p.FechaCreacion,
            (SELECT COUNT(*) FROM dbo.Politicas WHERE ProductoId = p.ProductoId AND Activo = 1) AS TotalPoliticas
    FROM    dbo.Productos p
    ORDER BY p.Nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Productos_ObtenerPorIdAdmin
    @ProductoId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ProductoId, Codigo, Nombre, Descripcion,
           RequiereIngresos, RequiereTipoEmpleo, RequiereAntiguedadLaboral, Activo
    FROM   dbo.Productos
    WHERE  ProductoId = @ProductoId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Productos_Crear
    @Codigo                     NVARCHAR(30),
    @Nombre                     NVARCHAR(150),
    @Descripcion                NVARCHAR(400) = NULL,
    @RequiereIngresos           BIT,
    @RequiereTipoEmpleo         BIT,
    @RequiereAntiguedadLaboral  BIT,
    @Activo                     BIT,
    @ProductoId                 INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Productos
        (Codigo, Nombre, Descripcion, RequiereIngresos, RequiereTipoEmpleo, RequiereAntiguedadLaboral, Activo)
    VALUES
        (@Codigo, @Nombre, @Descripcion, @RequiereIngresos, @RequiereTipoEmpleo, @RequiereAntiguedadLaboral, @Activo);

    SET @ProductoId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Productos_Actualizar
    @ProductoId                 INT,
    @Codigo                     NVARCHAR(30),
    @Nombre                     NVARCHAR(150),
    @Descripcion                NVARCHAR(400) = NULL,
    @RequiereIngresos           BIT,
    @RequiereTipoEmpleo         BIT,
    @RequiereAntiguedadLaboral  BIT,
    @Activo                     BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Productos
    SET Codigo = @Codigo,
        Nombre = @Nombre,
        Descripcion = @Descripcion,
        RequiereIngresos = @RequiereIngresos,
        RequiereTipoEmpleo = @RequiereTipoEmpleo,
        RequiereAntiguedadLaboral = @RequiereAntiguedadLaboral,
        Activo = @Activo
    WHERE ProductoId = @ProductoId;
END
GO

------------------------------------------------------------
-- Politicas (administración)
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Politicas_ObtenerTodosPorProducto
    @ProductoId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT PoliticaId, ProductoId, Codigo, Nombre, Descripcion,
           TipoRegla, ParametrosJson, Orden, Activo
    FROM   dbo.Politicas
    WHERE  ProductoId = @ProductoId
    ORDER BY Orden, PoliticaId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Politicas_ObtenerPorIdAdmin
    @PoliticaId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT PoliticaId, ProductoId, Codigo, Nombre, Descripcion,
           TipoRegla, ParametrosJson, Orden, Activo
    FROM   dbo.Politicas
    WHERE  PoliticaId = @PoliticaId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Politicas_Crear
    @ProductoId       INT,
    @Codigo           NVARCHAR(40),
    @Nombre           NVARCHAR(150),
    @Descripcion      NVARCHAR(400) = NULL,
    @TipoRegla        NVARCHAR(50),
    @ParametrosJson   NVARCHAR(500),
    @Orden            INT,
    @Activo           BIT,
    @PoliticaId       INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Politicas
        (ProductoId, Codigo, Nombre, Descripcion, TipoRegla, ParametrosJson, Orden, Activo)
    VALUES
        (@ProductoId, @Codigo, @Nombre, @Descripcion, @TipoRegla, @ParametrosJson, @Orden, @Activo);

    SET @PoliticaId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Politicas_Actualizar
    @PoliticaId       INT,
    @Codigo           NVARCHAR(40),
    @Nombre           NVARCHAR(150),
    @Descripcion      NVARCHAR(400) = NULL,
    @TipoRegla        NVARCHAR(50),
    @ParametrosJson   NVARCHAR(500),
    @Orden            INT,
    @Activo           BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Politicas
    SET Codigo = @Codigo,
        Nombre = @Nombre,
        Descripcion = @Descripcion,
        TipoRegla = @TipoRegla,
        ParametrosJson = @ParametrosJson,
        Orden = @Orden,
        Activo = @Activo
    WHERE PoliticaId = @PoliticaId;
END
GO
