/*
====================================================================
 CreditPulse - Script 11: Cuenta de servicio para las APIs de integración
====================================================================
 Ejecutar DESPUÉS de 01..10. Es incremental.

 Las 3 APIs nuevas (Api/Controllers/EvaluacionesApiController.cs,
 SibApiController.cs) permiten que OTROS SISTEMAS del ecosistema
 bancario pidan una evaluación crediticia, no una persona autenticada
 desde el navegador. Pero Evaluaciones.UsuarioId es NOT NULL (todo
 registro debe quedar atribuido a "quién" generó la evaluación, para
 auditoría), así que se necesita un Usuario dedicado para atribuir las
 evaluaciones que entran por API, en vez de reutilizar el usuario de un
 analista humano.

 Este script crea:
   1) Un rol "Integración API" con el único permiso PuedeEvaluar = 1
      (no puede administrar catálogo ni usuarios, aunque alguien
      intentara iniciar sesión con esta cuenta).
   2) Un usuario de servicio (integracion@creditpulse.com) con ese rol.

 La contraseña de este usuario es un valor aleatorio largo generado
 una sola vez para este script; nadie la conoce ni la necesita, porque
 esta cuenta NUNCA inicia sesión por el formulario de Login -solo
 existe para que las evaluaciones creadas por API tengan un
 UsuarioId válido-. La autenticación real de las APIs es la API key
 (ver appSetting "ApiKeyIntegraciones" en Web.config y
 Api/Filters/ApiKeyAuthorizeAttribute.cs), no esta contraseña.
*/
USE CreditPulseDB;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = N'Integración API')
BEGIN
    INSERT INTO dbo.Roles (Nombre, Descripcion, PuedeEvaluar, PuedeAdministrarCatalogo, PuedeAdministrarUsuarios, Activo)
    VALUES (N'Integración API', N'Cuenta de servicio usada para atribuir evaluaciones creadas por las APIs de integración (no un usuario humano).', 1, 0, 0, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = N'integracion@creditpulse.com')
BEGIN
    DECLARE @RolIntegracion INT = (SELECT RolId FROM dbo.Roles WHERE Nombre = N'Integración API');

    INSERT INTO dbo.Usuarios (Email, PasswordHash, PasswordSalt, NombreCompleto, RolId, Activo)
    VALUES (
        N'integracion@creditpulse.com',
        N'8lsDHkOrhEGTgd/o5RZR5igX4ioogA56uxDFmIXm8tY=',  -- PasswordHash (Base64); ver nota arriba, nunca se usa para iniciar sesión.
        N'w3N9emvRnWhW+HXIILNpHg==',                       -- PasswordSalt (Base64)
        N'Cuenta de Servicio - Integración API',
        @RolIntegracion,
        1
    );
END
GO
