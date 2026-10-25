/*
====================================================================
 CreditPulse - Script 14: Token de acceso (JWT) y refresh token
====================================================================
 Ejecutar DESPUÉS de 01..13. Es incremental e idempotente.

 Soporta la autenticación por token de las APIs (/api/v1/auth/...):
   - El token de acceso (JWT) es de corta duración y NO se guarda.
   - El refresh token (larga duración) sí se guarda, pero solo su
     HASH (SHA-256): si alguien lee esta tabla no obtiene tokens
     utilizables.
   - Cada refresh token se usa UNA sola vez (rotación): al renovar, el
     anterior queda revocado y se enlaza con su reemplazo.

 No modifica ninguna tabla ni procedimiento existente; la autenticación
 actual por X-Api-Key sigue funcionando igual.
*/
-- (Azure SQL Database no soporta USE para cambiar de base: la conexión
-- ya está fija a la base elegida al conectar. Línea 'USE CreditPulseDB;'
-- removida para esta variante; el resto del script es idéntico.)
GO

IF OBJECT_ID('dbo.RefreshTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens (
        RefreshTokenId      BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY,
        UsuarioId           INT          NOT NULL,
        TokenHash           CHAR(64)     NOT NULL,     -- SHA-256 en hexadecimal
        FechaCreacion       DATETIME2(0) NOT NULL CONSTRAINT DF_RefreshTokens_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        FechaExpira         DATETIME2(0) NOT NULL,
        FechaRevocado       DATETIME2(0) NULL,
        ReemplazadoPorHash  CHAR(64)     NULL,
        IpCreacion          NVARCHAR(45) NULL,
        CONSTRAINT FK_RefreshTokens_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RefreshTokens_TokenHash' AND object_id = OBJECT_ID('dbo.RefreshTokens'))
    CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash ON dbo.RefreshTokens (TokenHash);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_Usuario' AND object_id = OBJECT_ID('dbo.RefreshTokens'))
    CREATE INDEX IX_RefreshTokens_Usuario ON dbo.RefreshTokens (UsuarioId, FechaRevocado);
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuarios_ObtenerPorId
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Mismas columnas que sp_Usuarios_ObtenerPorEmail (incluye permisos del rol).
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
    WHERE   u.UsuarioId = @UsuarioId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_RefreshTokens_Crear
    @UsuarioId    INT,
    @TokenHash    CHAR(64),
    @FechaExpira  DATETIME2(0),
    @IpCreacion   NVARCHAR(45) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Limpieza oportunista: tokens de este usuario vencidos o revocados hace más de 30 días.
    DELETE FROM dbo.RefreshTokens
    WHERE  UsuarioId = @UsuarioId
      AND  (FechaExpira < DATEADD(DAY, -30, SYSUTCDATETIME())
            OR FechaRevocado < DATEADD(DAY, -30, SYSUTCDATETIME()));

    INSERT INTO dbo.RefreshTokens (UsuarioId, TokenHash, FechaExpira, IpCreacion)
    VALUES (@UsuarioId, @TokenHash, @FechaExpira, @IpCreacion);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_RefreshTokens_ObtenerPorHash
    @TokenHash CHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RefreshTokenId, UsuarioId, TokenHash, FechaCreacion, FechaExpira, FechaRevocado, ReemplazadoPorHash
    FROM   dbo.RefreshTokens
    WHERE  TokenHash = @TokenHash;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_RefreshTokens_Revocar
    @TokenHash           CHAR(64),
    @ReemplazadoPorHash  CHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.RefreshTokens
    SET    FechaRevocado = SYSUTCDATETIME(),
           ReemplazadoPorHash = @ReemplazadoPorHash
    WHERE  TokenHash = @TokenHash
      AND  FechaRevocado IS NULL;

    SELECT @@ROWCOUNT AS FilasAfectadas;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_RefreshTokens_RevocarTodosDeUsuario
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.RefreshTokens
    SET    FechaRevocado = SYSUTCDATETIME()
    WHERE  UsuarioId = @UsuarioId
      AND  FechaRevocado IS NULL;
END
GO
