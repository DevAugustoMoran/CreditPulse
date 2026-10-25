/*
 Usuario de la aplicación para el entorno Docker local (lo ejecuta Docker/db-init.sh).
 La contraseña llega por la variable de sqlcmd APP_PASSWORD (CP_APP_PASSWORD en docker-compose.yml).
 Es idempotente: se puede ejecutar varias veces.
*/
USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'cp_app')
BEGIN
    CREATE LOGIN cp_app WITH PASSWORD = N'$(APP_PASSWORD)', CHECK_POLICY = OFF;
END
GO

USE CreditPulseDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'cp_app')
BEGIN
    CREATE USER cp_app FOR LOGIN cp_app;
END
GO

ALTER ROLE db_owner ADD MEMBER cp_app;
GO
