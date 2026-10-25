/*
====================================================================
 CreditPulse - Script 04: Datos semilla
 Motor: Microsoft SQL Server Express 2022
====================================================================
 Crea: roles base, un usuario administrador de prueba, el catálogo
 de productos y un set de políticas por defecto (configurables luego
 desde la fase 2 / pantalla de administración).

 Usuario de prueba:
   Email:    admin@creditpulse.com
   Password: Admin#2026!
   (hash generado con PBKDF2-SHA256, 100000 iteraciones - ver
    BLL/Security/PasswordHasher.cs. Cumple la política de 10+
    caracteres con letras, números y símbolos.)
*/
-- (Azure SQL Database no soporta USE para cambiar de base: la conexión
-- ya está fija a la base elegida al conectar. Línea 'USE CreditPulseDB;'
-- removida para esta variante; el resto del script es idéntico.)
GO

------------------------------------------------------------
-- Roles
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Roles)
BEGIN
    INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES
        (N'Administrador',    N'Acceso total: administración de usuarios, productos y políticas.'),
        (N'Analista de Crédito', N'Captura y evalúa solicitudes de crédito.'),
        (N'Consulta',          N'Solo puede consultar evaluaciones ya generadas.');
END
GO

------------------------------------------------------------
-- Usuario administrador de prueba
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = N'admin@creditpulse.com')
BEGIN
    DECLARE @RolAdminId INT = (SELECT RolId FROM dbo.Roles WHERE Nombre = N'Administrador');

    INSERT INTO dbo.Usuarios (Email, PasswordHash, PasswordSalt, NombreCompleto, RolId, Activo)
    VALUES (
        N'admin@creditpulse.com',
        N'kkndUARTgcIXIq8gHMmMwvi8hMj27pHqsiyosRghxts=',  -- PasswordHash (Base64)
        N'070KnQqXpt73Ok72JKdXIQ==',                       -- PasswordSalt (Base64)
        N'Administrador CreditPulse',
        @RolAdminId,
        1
    );
END
GO

------------------------------------------------------------
-- Productos
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Productos)
BEGIN
    INSERT INTO dbo.Productos (Codigo, Nombre, Descripcion, RequiereIngresos, RequiereTipoEmpleo, RequiereAntiguedadLaboral) VALUES
        (N'TARJETA_CREDITO',      N'Tarjeta de Crédito Clásica', N'Tarjeta de crédito de consumo revolvente.', 1, 1, 0),
        (N'PRESTAMO_HIPOTECARIO', N'Préstamo Hipotecario',        N'Financiamiento para vivienda a largo plazo.', 1, 1, 1),
        (N'CREDITO_PERSONAL',     N'Crédito Personal',            N'Préstamo de libre disponibilidad a corto/mediano plazo.', 1, 1, 0),
        (N'CREDITO_VEHICULAR',    N'Crédito Vehicular',           N'Financiamiento para compra de vehículo.', 1, 1, 1);
END
GO

------------------------------------------------------------
-- Politicas (motor de reglas configurable)
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Politicas)
BEGIN
    DECLARE @Tarjeta   INT = (SELECT ProductoId FROM dbo.Productos WHERE Codigo = N'TARJETA_CREDITO');
    DECLARE @Hipoteca  INT = (SELECT ProductoId FROM dbo.Productos WHERE Codigo = N'PRESTAMO_HIPOTECARIO');
    DECLARE @Personal  INT = (SELECT ProductoId FROM dbo.Productos WHERE Codigo = N'CREDITO_PERSONAL');
    DECLARE @Vehicular INT = (SELECT ProductoId FROM dbo.Productos WHERE Codigo = N'CREDITO_VEHICULAR');

    -- Tarjeta de Crédito Clásica
    INSERT INTO dbo.Politicas (ProductoId, Codigo, Nombre, Descripcion, TipoRegla, ParametrosJson, Orden) VALUES
        (@Tarjeta, N'EDAD_MINIMA',       N'Edad Mínima',                        N'El solicitante debe ser mayor de edad.',                    N'EDAD_MINIMA',            N'{"min":18}',                                1),
        (@Tarjeta, N'LISTA_NEGRA',        N'Verificación en Listas de Riesgo SIB', N'El solicitante no debe figurar en listas negras.',          N'LISTA_NEGRA_SIB',        N'{}',                                          2),
        (@Tarjeta, N'SCORE_BURO',         N'Score de Buró de Crédito',            N'Score reportado por la Superintendencia de Bancos.',         N'SCORE_BURO_MINIMO',      N'{"verde":620,"amarillo":560}',               3),
        (@Tarjeta, N'ENDEUDAMIENTO',      N'Nivel de Endeudamiento',              N'Porcentaje de endeudamiento actual reportado.',              N'ENDEUDAMIENTO_MAXIMO',   N'{"verde":45,"amarillo":65}',                  4),
        (@Tarjeta, N'INGRESO_MINIMO',     N'Ingreso Mínimo Mensual (Q3,000)',     N'Ingreso mensual declarado por el solicitante.',              N'INGRESO_MINIMO',         N'{"minimo":3000,"toleranciaPorcentaje":10}',  5),
        (@Tarjeta, N'HISTORIAL_MORAS',    N'Historial de Moras (12 meses)',       N'Moras registradas en el historial crediticio reciente.',     N'HISTORIAL_MORAS',        N'{"verde":0,"amarillo":2}',                   6);

    -- Préstamo Hipotecario
    INSERT INTO dbo.Politicas (ProductoId, Codigo, Nombre, Descripcion, TipoRegla, ParametrosJson, Orden) VALUES
        (@Hipoteca, N'EDAD_MINIMA',       N'Edad Mínima',                         N'El solicitante debe ser mayor de edad.',                     N'EDAD_MINIMA',            N'{"min":21}',                                  1),
        (@Hipoteca, N'EDAD_MAXIMA',       N'Edad Máxima al Finalizar el Plazo',   N'Edad límite considerando el plazo del crédito.',             N'EDAD_MAXIMA',            N'{"max":70,"amarilloDesde":65}',              2),
        (@Hipoteca, N'LISTA_NEGRA',        N'Verificación en Listas de Riesgo SIB', N'El solicitante no debe figurar en listas negras.',          N'LISTA_NEGRA_SIB',        N'{}',                                          3),
        (@Hipoteca, N'SCORE_BURO',         N'Score de Buró de Crédito',            N'Score reportado por la Superintendencia de Bancos.',         N'SCORE_BURO_MINIMO',      N'{"verde":680,"amarillo":620}',               4),
        (@Hipoteca, N'ENDEUDAMIENTO',      N'Nivel de Endeudamiento',              N'Porcentaje de endeudamiento actual reportado.',              N'ENDEUDAMIENTO_MAXIMO',   N'{"verde":35,"amarillo":50}',                  5),
        (@Hipoteca, N'INGRESO_MINIMO',     N'Ingreso Mínimo Mensual (Q8,000)',     N'Ingreso mensual declarado por el solicitante.',              N'INGRESO_MINIMO',         N'{"minimo":8000,"toleranciaPorcentaje":5}',   6),
        (@Hipoteca, N'ANTIGUEDAD_LABORAL', N'Antigüedad Laboral Mínima (24 meses)',N'Tiempo continuo en el empleo actual.',                       N'ANTIGUEDAD_LABORAL_MINIMA', N'{"verdeMeses":24,"amarilloMeses":12}',    7),
        (@Hipoteca, N'HISTORIAL_MORAS',    N'Historial de Moras (12 meses)',       N'Moras registradas en el historial crediticio reciente.',     N'HISTORIAL_MORAS',        N'{"verde":0,"amarillo":1}',                   8);

    -- Crédito Personal
    INSERT INTO dbo.Politicas (ProductoId, Codigo, Nombre, Descripcion, TipoRegla, ParametrosJson, Orden) VALUES
        (@Personal, N'EDAD_MINIMA',       N'Edad Mínima',                        N'El solicitante debe ser mayor de edad.',                    N'EDAD_MINIMA',            N'{"min":18}',                                 1),
        (@Personal, N'LISTA_NEGRA',        N'Verificación en Listas de Riesgo SIB', N'El solicitante no debe figurar en listas negras.',         N'LISTA_NEGRA_SIB',        N'{}',                                         2),
        (@Personal, N'SCORE_BURO',         N'Score de Buró de Crédito',            N'Score reportado por la Superintendencia de Bancos.',        N'SCORE_BURO_MINIMO',      N'{"verde":600,"amarillo":540}',              3),
        (@Personal, N'ENDEUDAMIENTO',      N'Nivel de Endeudamiento',              N'Porcentaje de endeudamiento actual reportado.',             N'ENDEUDAMIENTO_MAXIMO',   N'{"verde":50,"amarillo":70}',                 4),
        (@Personal, N'INGRESO_MINIMO',     N'Ingreso Mínimo Mensual (Q2,500)',     N'Ingreso mensual declarado por el solicitante.',             N'INGRESO_MINIMO',         N'{"minimo":2500,"toleranciaPorcentaje":10}', 5),
        (@Personal, N'HISTORIAL_MORAS',    N'Historial de Moras (12 meses)',       N'Moras registradas en el historial crediticio reciente.',    N'HISTORIAL_MORAS',        N'{"verde":0,"amarillo":3}',                  6);

    -- Crédito Vehicular
    INSERT INTO dbo.Politicas (ProductoId, Codigo, Nombre, Descripcion, TipoRegla, ParametrosJson, Orden) VALUES
        (@Vehicular, N'EDAD_MINIMA',       N'Edad Mínima',                        N'El solicitante debe ser mayor de edad.',                    N'EDAD_MINIMA',            N'{"min":21}',                                 1),
        (@Vehicular, N'LISTA_NEGRA',        N'Verificación en Listas de Riesgo SIB', N'El solicitante no debe figurar en listas negras.',         N'LISTA_NEGRA_SIB',        N'{}',                                         2),
        (@Vehicular, N'SCORE_BURO',         N'Score de Buró de Crédito',            N'Score reportado por la Superintendencia de Bancos.',        N'SCORE_BURO_MINIMO',      N'{"verde":640,"amarillo":580}',              3),
        (@Vehicular, N'ENDEUDAMIENTO',      N'Nivel de Endeudamiento',              N'Porcentaje de endeudamiento actual reportado.',             N'ENDEUDAMIENTO_MAXIMO',   N'{"verde":40,"amarillo":60}',                 4),
        (@Vehicular, N'INGRESO_MINIMO',     N'Ingreso Mínimo Mensual (Q4,500)',     N'Ingreso mensual declarado por el solicitante.',             N'INGRESO_MINIMO',         N'{"minimo":4500,"toleranciaPorcentaje":10}', 5),
        (@Vehicular, N'ANTIGUEDAD_LABORAL', N'Antigüedad Laboral Mínima (12 meses)',N'Tiempo continuo en el empleo actual.',                      N'ANTIGUEDAD_LABORAL_MINIMA', N'{"verdeMeses":12,"amarilloMeses":6}',    6);
END
GO
