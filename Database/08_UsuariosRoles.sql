/*
====================================================================
 CreditPulse - Script 08: Administración de Usuarios y Roles
====================================================================
 Ejecutar DESPUÉS de 01..07. Es incremental.

 Hasta ahora, "Roles y Permisos" (Fase 2) restringía pantallas
 completas comparando el NOMBRE del rol contra 3 constantes fijas en
 código (Models.RolesDeSistema: "Administrador", "Analista de
 Crédito", "Consulta" -> BasePage.RolesPermitidos en cada página).
 Eso funciona, pero tiene un problema para este script: si un
 administrador pudiera "crear roles" libremente desde una pantalla,
 un rol nuevo (p.ej. "Supervisor") no tendría forma de obtener acceso
 a ninguna pantalla restringida, porque el código nunca lo menciona
 por nombre.

 Este script convierte el modelo de "nombre de rol fijo" a un modelo
 de PERMISOS por rol (RBAC simple): cada rol ahora declara qué puede
 hacer mediante 3 columnas booleanas. BasePage ya no compara nombres,
 compara permisos (ver BasePage.cs / Models/Permiso.cs). Así, un rol
 nuevo creado desde Administracion/RolForm.aspx sí puede recibir
 acceso real a las pantallas correspondientes, sin tocar código.

 Los 3 roles sembrados en 04_SeedData.sql conservan exactamente el
 mismo comportamiento que tenían antes de este script (Administrador
 con todo, Analista de Crédito solo evaluación, Consulta solo
 lectura), solo que ahora ese comportamiento está declarado con datos
 en vez de con nombres hardcodeados.
*/
USE CreditPulseDB;
GO

------------------------------------------------------------
-- Columnas de permisos en Roles
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Roles') AND name = 'PuedeEvaluar')
BEGIN
    ALTER TABLE dbo.Roles ADD
        PuedeEvaluar              BIT NOT NULL CONSTRAINT DF_Roles_PuedeEvaluar DEFAULT (0),
        PuedeAdministrarCatalogo  BIT NOT NULL CONSTRAINT DF_Roles_PuedeAdministrarCatalogo DEFAULT (0),
        PuedeAdministrarUsuarios  BIT NOT NULL CONSTRAINT DF_Roles_PuedeAdministrarUsuarios DEFAULT (0),
        Activo                    BIT NOT NULL CONSTRAINT DF_Roles_Activo DEFAULT (1);
END
GO

-- Alinea los permisos de los 3 roles sembrados con el comportamiento que
-- ya tenían por código antes de este script (ver Models/RolesDeSistema.cs).
UPDATE dbo.Roles SET PuedeEvaluar = 1, PuedeAdministrarCatalogo = 1, PuedeAdministrarUsuarios = 1 WHERE Nombre = N'Administrador';
UPDATE dbo.Roles SET PuedeEvaluar = 1, PuedeAdministrarCatalogo = 0, PuedeAdministrarUsuarios = 0 WHERE Nombre = N'Analista de Crédito';
UPDATE dbo.Roles SET PuedeEvaluar = 0, PuedeAdministrarCatalogo = 0, PuedeAdministrarUsuarios = 0 WHERE Nombre = N'Consulta';
GO

------------------------------------------------------------
-- Login: ahora también devuelve los permisos del rol
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
            u.IntentosFallidos,
            r.PuedeEvaluar,
            r.PuedeAdministrarCatalogo,
            r.PuedeAdministrarUsuarios
    FROM    dbo.Usuarios u
    INNER JOIN dbo.Roles r ON r.RolId = u.RolId
    WHERE   u.Email = @Email;
END
GO

------------------------------------------------------------
-- Roles (administración)
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Roles_ObtenerTodosAdmin
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  r.RolId, r.Nombre, r.Descripcion,
            r.PuedeEvaluar, r.PuedeAdministrarCatalogo, r.PuedeAdministrarUsuarios, r.Activo,
            (SELECT COUNT(*) FROM dbo.Usuarios WHERE RolId = r.RolId AND Activo = 1) AS TotalUsuariosActivos
    FROM    dbo.Roles r
    ORDER BY r.Nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Roles_ObtenerActivos
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RolId, Nombre, Descripcion, PuedeEvaluar, PuedeAdministrarCatalogo, PuedeAdministrarUsuarios, Activo
    FROM   dbo.Roles
    WHERE  Activo = 1
    ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Roles_ObtenerPorIdAdmin
    @RolId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RolId, Nombre, Descripcion, PuedeEvaluar, PuedeAdministrarCatalogo, PuedeAdministrarUsuarios, Activo
    FROM   dbo.Roles
    WHERE  RolId = @RolId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Roles_Crear
    @Nombre                     NVARCHAR(50),
    @Descripcion                NVARCHAR(200) = NULL,
    @PuedeEvaluar               BIT,
    @PuedeAdministrarCatalogo   BIT,
    @PuedeAdministrarUsuarios   BIT,
    @Activo                     BIT,
    @RolId                      INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Roles (Nombre, Descripcion, PuedeEvaluar, PuedeAdministrarCatalogo, PuedeAdministrarUsuarios, Activo)
    VALUES (@Nombre, @Descripcion, @PuedeEvaluar, @PuedeAdministrarCatalogo, @PuedeAdministrarUsuarios, @Activo);

    SET @RolId = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Roles_Actualizar
    @RolId                      INT,
    @Nombre                     NVARCHAR(50),
    @Descripcion                NVARCHAR(200) = NULL,
    @PuedeEvaluar               BIT,
    @PuedeAdministrarCatalogo   BIT,
    @PuedeAdministrarUsuarios   BIT,
    @Activo                     BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Roles
    SET Nombre = @Nombre,
        Descripcion = @Descripcion,
        PuedeEvaluar = @PuedeEvaluar,
        PuedeAdministrarCatalogo = @PuedeAdministrarCatalogo,
        PuedeAdministrarUsuarios = @PuedeAdministrarUsuarios,
        Activo = @Activo
    WHERE RolId = @RolId;
END
GO

-- Usado antes de desactivar un rol (o cambiarle los permisos de forma
-- drástica) para advertir/bloquear si todavía hay usuarios activos
-- dependiendo de él.
CREATE OR ALTER PROCEDURE dbo.sp_Roles_ContarUsuariosActivos
    @RolId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS Total FROM dbo.Usuarios WHERE RolId = @RolId AND Activo = 1;
END
GO

------------------------------------------------------------
-- Usuarios (administración)
------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Usuarios_ObtenerTodosAdmin
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  u.UsuarioId, u.Email, u.NombreCompleto, u.RolId, r.Nombre AS RolNombre,
            u.Activo, u.IntentosFallidos, u.FechaCreacion, u.UltimoLogin
    FROM    dbo.Usuarios u
    INNER JOIN dbo.Roles r ON r.RolId = u.RolId
    ORDER BY u.NombreCompleto;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuarios_ObtenerPorIdAdmin
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT UsuarioId, Email, NombreCompleto, RolId, Activo, IntentosFallidos
    FROM   dbo.Usuarios
    WHERE  UsuarioId = @UsuarioId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuarios_Crear
    @Email            NVARCHAR(150),
    @PasswordHash     NVARCHAR(200),
    @PasswordSalt     NVARCHAR(200),
    @NombreCompleto   NVARCHAR(200),
    @RolId            INT,
    @Activo           BIT,
    @UsuarioId        INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Usuarios (Email, PasswordHash, PasswordSalt, NombreCompleto, RolId, Activo)
    VALUES (@Email, @PasswordHash, @PasswordSalt, @NombreCompleto, @RolId, @Activo);

    SET @UsuarioId = SCOPE_IDENTITY();
END
GO

-- Actualiza los datos del usuario sin tocar la contraseña (ver
-- sp_Usuarios_ActualizarPassword para el flujo separado de reseteo).
CREATE OR ALTER PROCEDURE dbo.sp_Usuarios_Actualizar
    @UsuarioId        INT,
    @Email            NVARCHAR(150),
    @NombreCompleto   NVARCHAR(200),
    @RolId             INT,
    @Activo            BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Usuarios
    SET Email = @Email,
        NombreCompleto = @NombreCompleto,
        RolId = @RolId,
        Activo = @Activo
    WHERE UsuarioId = @UsuarioId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuarios_ActualizarPassword
    @UsuarioId       INT,
    @PasswordHash    NVARCHAR(200),
    @PasswordSalt    NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    -- Resetear la contraseña también desbloquea la cuenta (limpia los
    -- intentos fallidos): es el mecanismo con el que un administrador
    -- recupera a un usuario que llegó a CuentaBloqueada (ver AuthService).
    UPDATE dbo.Usuarios
    SET PasswordHash = @PasswordHash,
        PasswordSalt = @PasswordSalt,
        IntentosFallidos = 0
    WHERE UsuarioId = @UsuarioId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuarios_CambiarEstado
    @UsuarioId  INT,
    @Activo     BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Usuarios
    SET Activo = @Activo,
        IntentosFallidos = CASE WHEN @Activo = 1 THEN 0 ELSE IntentosFallidos END
    WHERE UsuarioId = @UsuarioId;
END
GO
