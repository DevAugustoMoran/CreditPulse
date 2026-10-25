/*
====================================================================
 CreditPulse - Script 10: Política RCI Bancario (Tarjeta de Crédito)
====================================================================
 Ejecutar DESPUÉS de 01..09. Es incremental.

 RCI = Relación Cuota/Ingreso: un indicador porcentual de qué tan
 posible es que el cliente pague un crédito, dividiendo sus deudas
 entre sus ingresos (ver BLL/PolicyEngine.cs -> EvaluarRciBancario
 para el detalle exacto del cálculo). A diferencia de la política
 "Endeudamiento" (que solo compara el % crudo que reporta la SIB
 contra un umbral), el RCI cruza la deuda que reporta la SIB contra
 el ingreso que el cliente declaró en la solicitud actual, sin pedir
 ningún monto de crédito nuevo.
*/
-- (Azure SQL Database no soporta USE para cambiar de base: la conexión
-- ya está fija a la base elegida al conectar. Línea 'USE CreditPulseDB;'
-- removida para esta variante; el resto del script es idéntico.)
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Politicas WHERE Codigo = N'RCI_BANCARIO'
               AND ProductoId = (SELECT ProductoId FROM dbo.Productos WHERE Codigo = N'TARJETA_CREDITO'))
BEGIN
    DECLARE @Tarjeta INT = (SELECT ProductoId FROM dbo.Productos WHERE Codigo = N'TARJETA_CREDITO');

    -- Se agrega al final del set de políticas ya sembradas para Tarjeta de
    -- Crédito Clásica (Orden 1..6 en 04_SeedData.sql), como Orden 7.
    INSERT INTO dbo.Politicas (ProductoId, Codigo, Nombre, Descripcion, TipoRegla, ParametrosJson, Orden)
    VALUES (@Tarjeta, N'RCI_BANCARIO', N'RCI Bancario (Relación Cuota/Ingreso)',
            N'Qué tan posible es que el cliente pague un crédito, dividiendo sus deudas estimadas entre su ingreso declarado.',
            N'RCI_BANCARIO', N'{"verde":35,"amarillo":45}', 7);
END
GO
