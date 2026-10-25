using System;
using Newtonsoft.Json.Linq;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.BLL
{
    /// <summary>
    /// Evalúa una <see cref="Politica"/> contra un <see cref="EvaluationContext"/>
    /// y devuelve el color de semáforo (Verde / Amarillo / Rojo) que le
    /// corresponde, junto con una explicación legible para el analista.
    ///
    /// Cada política es un dato en la base de datos (tabla Politicas), no un
    /// "if" fijo en el código: agregar un producto nuevo o ajustar un umbral
    /// (ej. subir el score mínimo de una tarjeta) es un INSERT/UPDATE, sin
    /// tocar el código fuente ni recompilar. Ver Database/04_SeedData.sql
    /// para el catálogo de políticas por defecto.
    /// </summary>
    public class PolicyEngine
    {
        public PolicyEvaluationResult Evaluar(EvaluationContext contexto, Politica politica)
        {
            JObject parametros = JObject.Parse(string.IsNullOrWhiteSpace(politica.ParametrosJson) ? "{}" : politica.ParametrosJson);

            switch (politica.TipoRegla)
            {
                case TiposDeRegla.EdadMinima:
                    return EvaluarEdadMinima(contexto, parametros);

                case TiposDeRegla.EdadMaxima:
                    return EvaluarEdadMaxima(contexto, parametros);

                case TiposDeRegla.ListaNegraSib:
                    return EvaluarListaNegra(contexto);

                case TiposDeRegla.ScoreBuroMinimo:
                    return EvaluarScoreBuro(contexto, parametros);

                case TiposDeRegla.EndeudamientoMaximo:
                    return EvaluarEndeudamiento(contexto, parametros);

                case TiposDeRegla.IngresoMinimo:
                    return EvaluarIngresoMinimo(contexto, parametros);

                case TiposDeRegla.HistorialMoras:
                    return EvaluarHistorialMoras(contexto, parametros);

                case TiposDeRegla.AntiguedadLaboralMinima:
                    return EvaluarAntiguedadLaboral(contexto, parametros);

                case TiposDeRegla.RciBancario:
                    return EvaluarRciBancario(contexto, parametros);

                default:
                    // Política mal configurada: no debe tumbar la evaluación completa,
                    // se marca en amarillo para que un analista la revise manualmente.
                    return new PolicyEvaluationResult(
                        ResultadoSemaforo.Amarillo,
                        $"Tipo de regla '{politica.TipoRegla}' no reconocido. Requiere revisión manual de configuración.");
            }
        }

        private static PolicyEvaluationResult EvaluarEdadMinima(EvaluationContext ctx, JObject parametros)
        {
            int minimo = parametros.Value<int?>("min") ?? 18;

            return ctx.EdadAnios >= minimo
                ? new PolicyEvaluationResult(ResultadoSemaforo.Verde, $"Edad {ctx.EdadAnios} años cumple el mínimo requerido ({minimo}).")
                : new PolicyEvaluationResult(ResultadoSemaforo.Rojo, $"Edad {ctx.EdadAnios} años es menor al mínimo requerido ({minimo}).");
        }

        private static PolicyEvaluationResult EvaluarEdadMaxima(EvaluationContext ctx, JObject parametros)
        {
            int maximo = parametros.Value<int?>("max") ?? 75;
            int amarilloDesde = parametros.Value<int?>("amarilloDesde") ?? (maximo - 5);

            if (ctx.EdadAnios > maximo)
                return new PolicyEvaluationResult(ResultadoSemaforo.Rojo, $"Edad {ctx.EdadAnios} años supera el máximo permitido ({maximo}).");

            if (ctx.EdadAnios >= amarilloDesde)
                return new PolicyEvaluationResult(ResultadoSemaforo.Amarillo, $"Edad {ctx.EdadAnios} años está cerca del límite permitido ({maximo}); requiere validación adicional.");

            return new PolicyEvaluationResult(ResultadoSemaforo.Verde, $"Edad {ctx.EdadAnios} años dentro del rango permitido.");
        }

        private static PolicyEvaluationResult EvaluarListaNegra(EvaluationContext ctx)
        {
            if (ctx.Sib.EnListaNegraLocal || ctx.Sib.EnListaNegraInternacional || ctx.Sib.EnListaOfac)
            {
                return new PolicyEvaluationResult(ResultadoSemaforo.Rojo,
                    "El solicitante aparece en al menos una lista de riesgo (local, internacional u OFAC).");
            }

            return new PolicyEvaluationResult(ResultadoSemaforo.Verde, "Sin coincidencias en listas de riesgo consultadas por la SIB.");
        }

        private static PolicyEvaluationResult EvaluarScoreBuro(EvaluationContext ctx, JObject parametros)
        {
            int verde = parametros.Value<int?>("verde") ?? 650;
            int amarillo = parametros.Value<int?>("amarillo") ?? 580;
            int score = ctx.Sib.ScoreBuro;

            if (score >= verde)
                return new PolicyEvaluationResult(ResultadoSemaforo.Verde, $"Score de buró {score} cumple el mínimo requerido ({verde}).");

            if (score >= amarillo)
                return new PolicyEvaluationResult(ResultadoSemaforo.Amarillo, $"Score de buró {score} está entre {amarillo} y {verde}; requiere revisión manual.");

            return new PolicyEvaluationResult(ResultadoSemaforo.Rojo, $"Score de buró {score} está por debajo del mínimo aceptable ({amarillo}).");
        }

        private static PolicyEvaluationResult EvaluarEndeudamiento(EvaluationContext ctx, JObject parametros)
        {
            decimal verde = parametros.Value<decimal?>("verde") ?? 40m;
            decimal amarillo = parametros.Value<decimal?>("amarillo") ?? 60m;
            decimal endeudamiento = ctx.Sib.NivelEndeudamiento;

            if (endeudamiento <= verde)
                return new PolicyEvaluationResult(ResultadoSemaforo.Verde, $"Endeudamiento {endeudamiento}% dentro del rango saludable (≤ {verde}%).");

            if (endeudamiento <= amarillo)
                return new PolicyEvaluationResult(ResultadoSemaforo.Amarillo, $"Endeudamiento {endeudamiento}% es elevado (entre {verde}% y {amarillo}%); requiere revisión manual.");

            return new PolicyEvaluationResult(ResultadoSemaforo.Rojo, $"Endeudamiento {endeudamiento}% supera el máximo permitido ({amarillo}%).");
        }

        private static PolicyEvaluationResult EvaluarIngresoMinimo(EvaluationContext ctx, JObject parametros)
        {
            decimal minimo = parametros.Value<decimal?>("minimo") ?? 3000m;
            decimal toleranciaPorcentaje = parametros.Value<decimal?>("toleranciaPorcentaje") ?? 10m;
            decimal umbralAmarillo = minimo * (1 - toleranciaPorcentaje / 100m);

            decimal ingresoDeclarado = ctx.Solicitud.IngresosMensuales ?? 0m;

            if (ingresoDeclarado >= minimo)
                return new PolicyEvaluationResult(ResultadoSemaforo.Verde, $"Ingreso declarado Q{ingresoDeclarado:N2} cumple el mínimo requerido (Q{minimo:N2}).");

            if (ingresoDeclarado >= umbralAmarillo)
                return new PolicyEvaluationResult(ResultadoSemaforo.Amarillo, $"Ingreso declarado Q{ingresoDeclarado:N2} está ligeramente por debajo del mínimo (Q{minimo:N2}); requiere validación de soportes.");

            return new PolicyEvaluationResult(ResultadoSemaforo.Rojo, $"Ingreso declarado Q{ingresoDeclarado:N2} no alcanza el mínimo requerido (Q{minimo:N2}).");
        }

        private static PolicyEvaluationResult EvaluarHistorialMoras(EvaluationContext ctx, JObject parametros)
        {
            int verde = parametros.Value<int?>("verde") ?? 0;
            int amarillo = parametros.Value<int?>("amarillo") ?? 2;
            int moras = ctx.Sib.MorasUltimos12Meses;

            if (moras <= verde)
                return new PolicyEvaluationResult(ResultadoSemaforo.Verde, $"{moras} mora(s) en los últimos 12 meses: historial limpio.");

            if (moras <= amarillo)
                return new PolicyEvaluationResult(ResultadoSemaforo.Amarillo, $"{moras} mora(s) en los últimos 12 meses: requiere revisión manual.");

            return new PolicyEvaluationResult(ResultadoSemaforo.Rojo, $"{moras} mora(s) en los últimos 12 meses: excede el máximo tolerado ({amarillo}).");
        }

        /// <summary>
        /// RCI (Relación Cuota/Ingreso): qué tan posible es que el cliente
        /// pague un crédito nuevo, dividiendo sus deudas entre sus ingresos.
        /// No se pide un monto de crédito solicitado; se estima la deuda
        /// mensual del cliente a partir de lo que ya reporta la SIB
        /// (NivelEndeudamiento% aplicado sobre los IngresosReportados al
        /// buró) y esa deuda se compara contra el ingreso que el cliente
        /// declaró en ESTA solicitud (Solicitud.IngresosMensuales).
        ///
        /// Es deliberadamente distinta de la política "Endeudamiento"
        /// (EvaluarEndeudamiento): esa solo compara el % crudo que reporta
        /// la SIB contra un umbral fijo, sin importar qué ingreso declaró el
        /// cliente hoy. El RCI, en cambio, cruza la deuda reportada por el
        /// buró contra el ingreso declarado en la solicitud actual: si
        /// ambos ingresos (el del buró y el declarado ahora) coinciden, el
        /// RCI coincide con el Endeudamiento; si el cliente declaró un
        /// ingreso distinto, el RCI se separa y refleja mejor su capacidad
        /// de pago real con el ingreso que está declarando en este momento.
        /// </summary>
        private static PolicyEvaluationResult EvaluarRciBancario(EvaluationContext ctx, JObject parametros)
        {
            decimal verde = parametros.Value<decimal?>("verde") ?? 35m;
            decimal amarillo = parametros.Value<decimal?>("amarillo") ?? 45m;

            decimal ingresosDeclarados = ctx.Solicitud.IngresosMensuales ?? 0m;
            if (ingresosDeclarados <= 0m)
            {
                return new PolicyEvaluationResult(ResultadoSemaforo.Amarillo,
                    "No se puede calcular el RCI sin un ingreso mensual declarado; requiere revisión manual.");
            }

            decimal deudaMensualEstimada = (ctx.Sib.NivelEndeudamiento / 100m) * ctx.Sib.IngresosReportados;
            decimal rci = Math.Round(deudaMensualEstimada / ingresosDeclarados * 100m, 2);

            if (rci <= verde)
                return new PolicyEvaluationResult(ResultadoSemaforo.Verde,
                    $"RCI de {rci}%: sus deudas estimadas representan una porción saludable de su ingreso declarado (≤ {verde}%).");

            if (rci <= amarillo)
                return new PolicyEvaluationResult(ResultadoSemaforo.Amarillo,
                    $"RCI de {rci}%: sus deudas estimadas representan una porción elevada de su ingreso declarado (entre {verde}% y {amarillo}%); requiere revisión manual.");

            return new PolicyEvaluationResult(ResultadoSemaforo.Rojo,
                $"RCI de {rci}%: sus deudas estimadas superan una capacidad de pago razonable frente a su ingreso declarado (> {amarillo}%).");
        }

        private static PolicyEvaluationResult EvaluarAntiguedadLaboral(EvaluationContext ctx, JObject parametros)
        {
            int verdeMeses = parametros.Value<int?>("verdeMeses") ?? 24;
            int amarilloMeses = parametros.Value<int?>("amarilloMeses") ?? 12;
            int antiguedad = ctx.Solicitud.AntiguedadLaboralMeses ?? 0;

            if (antiguedad >= verdeMeses)
                return new PolicyEvaluationResult(ResultadoSemaforo.Verde, $"Antigüedad laboral de {antiguedad} meses cumple el mínimo requerido ({verdeMeses}).");

            if (antiguedad >= amarilloMeses)
                return new PolicyEvaluationResult(ResultadoSemaforo.Amarillo, $"Antigüedad laboral de {antiguedad} meses es limitada (mínimo ideal: {verdeMeses}); requiere revisión manual.");

            return new PolicyEvaluationResult(ResultadoSemaforo.Rojo, $"Antigüedad laboral de {antiguedad} meses no alcanza el mínimo requerido ({amarilloMeses}).");
        }
    }

    /// <summary>Resultado de evaluar una política individual: color + explicación.</summary>
    public class PolicyEvaluationResult
    {
        public ResultadoSemaforo Resultado { get; }
        public string Detalle { get; }

        public PolicyEvaluationResult(ResultadoSemaforo resultado, string detalle)
        {
            Resultado = resultado;
            Detalle = detalle;
        }
    }
}
