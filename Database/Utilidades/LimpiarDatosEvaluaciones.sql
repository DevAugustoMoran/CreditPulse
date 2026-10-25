/*
====================================================================
 CreditPulse - Utilidad: limpiar datos de evaluaciones/lotes
====================================================================
 Uso: cuando la base (por ejemplo, el plan gratuito de Somee con
 solo 30 MB) se llena de datos de PRUEBA -como un lote batch de
 1000 registros- y quieres recuperar el espacio.

 ADVERTENCIA: esto borra TODAS las evaluaciones y lotes existentes
 (no solo el último lote). Es seguro para un entorno de pruebas de
 curso, pero NO lo corras si ya tienes evaluaciones "reales" que
 quieras conservar para la entrega/demo -en ese caso, filtra los
 DELETE con "WHERE LoteId = <id>" / "WHERE EvaluacionId IN (...)"
 en vez de borrar todo.

 No toca Usuarios, Roles, Productos ni Politicas: el catálogo y las
 cuentas se quedan intactos, esto solo limpia datos transaccionales.
*/

-- 1) Detalle de lotes (depende de Lotes Y de Evaluaciones)
DELETE FROM dbo.LoteEvaluaciones;

-- 2) Detalle de políticas evaluadas (depende de Evaluaciones)
DELETE FROM dbo.EvaluacionPoliticas;

-- 3) Encabezados de evaluación
DELETE FROM dbo.Evaluaciones;

-- 4) Encabezados de lote
DELETE FROM dbo.Lotes;

-- 5) (Opcional) Reinicia los contadores para que el próximo registro
--    empiece en un número limpio, igual que en una base recién creada.
DBCC CHECKIDENT ('dbo.Evaluaciones', RESEED, 100000);       -- próxima = 100001
DBCC CHECKIDENT ('dbo.EvaluacionPoliticas', RESEED, 0);
DBCC CHECKIDENT ('dbo.Lotes', RESEED, 0);
DBCC CHECKIDENT ('dbo.LoteEvaluaciones', RESEED, 0);

-- 6) Verifica cuánto espacio quedó usado
EXEC sp_spaceused;

-- 7) (Opcional) Compacta físicamente el archivo de datos. Un DELETE
--    libera espacio para reutilizar DENTRO del archivo, pero el
--    archivo en disco no siempre se encoge solo -y la cuota de 30 MB
--    de Somee probablemente mide el tamaño del archivo, no solo lo
--    "usado". Reemplaza <NOMBRE_DE_TU_BASE> por el nombre real que
--    te dio Somee (no necesariamente "CreditPulseDB"). Si tu cuenta
--    gratuita no tiene permiso para correr esto, va a dar un error de
--    permisos -ignóralo, no es crítico, solo no recuperarás el
--    espacio en disco físico tan rápido-.
DBCC SHRINKDATABASE (N'<NOMBRE_DE_TU_BASE>');
