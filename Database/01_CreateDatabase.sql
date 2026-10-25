/*
====================================================================
 CreditPulse - Script 01: Creación de la base de datos
 Motor: Microsoft SQL Server Express 2022
====================================================================
 Ejecutar este script primero, conectado al motor (master).
*/
USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'CreditPulseDB')
BEGIN
    CREATE DATABASE CreditPulseDB
    COLLATE Modern_Spanish_CI_AS;
END
GO

ALTER DATABASE CreditPulseDB SET RECOVERY SIMPLE;
GO

USE CreditPulseDB;
GO
