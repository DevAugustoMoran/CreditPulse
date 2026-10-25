/*
====================================================================
 CreditPulse - Script 07: Usuarios de prueba para Roles y permisos
====================================================================
 Ejecutar DESPUÉS de 01..06. Es incremental: la tabla Roles ya se
 sembró en 04_SeedData.sql (Administrador, Analista de Crédito,
 Consulta); este script solo agrega un usuario de prueba para cada
 uno de los dos roles que todavía no tenían usuario (el Administrador
 de prueba ya existe desde 04_SeedData.sql).

 Restricción implementada (Fase 2, último entregable): "pantallas
 completas" por rol (BasePage.RolesPermitidos en el código C#):
   - Administrador:        ve todo, incluida Administración de
                            Productos/Políticas.
   - Analista de Crédito:  Nueva Evaluación, Evaluaciones Batch,
                            Dashboard y reportes. Sin Administración.
   - Consulta:              solo Dashboard y reportes (Resultado /
                            LoteResultado), en modo lectura. No ve los
                            botones ni los enlaces de creación.

 Usuarios de prueba (contraseñas cumplen la política: 10+ caracteres,
 letra + número + símbolo; hash PBKDF2-HMAC-SHA256, 100000
 iteraciones, igual que el resto de usuarios del sistema):

   Email: analista@creditpulse.com   Password: Analista#2026!
   Email: consulta@creditpulse.com   Password: Consulta#2026!
*/
USE CreditPulseDB;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = N'analista@creditpulse.com')
BEGIN
    DECLARE @RolAnalistaId INT = (SELECT RolId FROM dbo.Roles WHERE Nombre = N'Analista de Crédito');

    INSERT INTO dbo.Usuarios (Email, PasswordHash, PasswordSalt, NombreCompleto, RolId, Activo)
    VALUES (
        N'analista@creditpulse.com',
        N'+OsFkV2mGHtYhe+m4LBhvTq6HljMKbsD23zxf6oXTr0=',  -- PasswordHash (Base64)
        N'LY4g5u0wgXyRp7HFgIZNsg==',                       -- PasswordSalt (Base64)
        N'Analista de Crédito (Prueba)',
        @RolAnalistaId,
        1
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = N'consulta@creditpulse.com')
BEGIN
    DECLARE @RolConsultaId INT = (SELECT RolId FROM dbo.Roles WHERE Nombre = N'Consulta');

    INSERT INTO dbo.Usuarios (Email, PasswordHash, PasswordSalt, NombreCompleto, RolId, Activo)
    VALUES (
        N'consulta@creditpulse.com',
        N'2SDSuYZ3wOlvnT+tiWR/P+oL96ldwEUER+OW4aWCPhQ=',  -- PasswordHash (Base64)
        N'7TckSUBbPe5dUH0aSfkpOg==',                       -- PasswordSalt (Base64)
        N'Usuario de Consulta (Prueba)',
        @RolConsultaId,
        1
    );
END
GO
