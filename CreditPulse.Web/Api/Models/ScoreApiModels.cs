using System;
using System.Collections.Generic;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Api.Models
{
    /// <summary>Una variable del modelo de score, con su valor para este cliente y el peso sugerido.</summary>
    public class VariableScoreApiDto
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public decimal Valor { get; set; }

        /// <summary>Unidad del campo "Valor": "puntos", "%", "Q", "meses", "años", "conteo".</summary>
        public string Unidad { get; set; }

        /// <summary>Peso sugerido de esta variable dentro del score (0-100, suman 100 entre todas).</summary>
        public decimal PesoSugeridoPorcentaje { get; set; }

        /// <summary>
        /// El mismo valor, pero llevado a una escala 0-100 donde 100 es
        /// "mejor para el cliente" y 0 es "peor", para que sea comparable
        /// entre variables con unidades distintas (puntos de buró vs.
        /// quetzales vs. meses). Es una normalización simple e ilustrativa
        /// (ver VariablesScoreCalculator); un sistema consumidor puede
        /// ignorarla por completo y calcular su propia normalización a
        /// partir de "Valor".
        /// </summary>
        public decimal ValorNormalizado0a100 { get; set; }

        /// <summary>Aclaración puntual sobre esta variable (ej. "sin ingreso declarado"), o null si no aplica.</summary>
        public string Nota { get; set; }
    }

    /// <summary>
    /// Respuesta de GET /api/v1/evaluaciones/{id}/variables-score: el valor
    /// de cada variable usada típicamente en un modelo de score crediticio
    /// ponderado, para que OTROS sistemas calculen el score final "a su
    /// manera" (con sus propios pesos, su propia fórmula, o los que aquí se
    /// sugieren) en vez de que CreditPulse les imponga un número ya
    /// calculado.
    /// </summary>
    public class VariablesScoreApiResponse
    {
        public int EvaluacionId { get; set; }

        /// <summary>
        /// false si el XML crudo de la SIB guardado en esta evaluación no
        /// se pudo interpretar (ver BLL.SibXmlParser): en ese caso, las
        /// variables que dependen de la SIB usan solo lo que quedó en las
        /// columnas planas (Score/Endeudamiento) y el resto se reporta en 0
        /// con una Nota explicando la limitación, en vez de fallar toda la
        /// respuesta.
        /// </summary>
        public bool DatosSibCompletos { get; set; }

        public List<VariableScoreApiDto> Variables { get; set; }

        /// <summary>Suma de PesoSugeridoPorcentaje de todas las variables; siempre 100 con el modelo por defecto.</summary>
        public decimal SumaPesos { get; set; }

        /// <summary>
        /// Ejemplo de cómo combinar las variables: suma de
        /// (ValorNormalizado0a100 × Peso/100) de cada una. Es SOLO una
        /// referencia de una posible fórmula, no "el score oficial" de
        /// CreditPulse -el sistema no calcula ni persiste un score propio,
        /// ver la Nota-.
        /// </summary>
        public decimal ScoreSugerido0a100 { get; set; }

        public string Nota { get; set; }
    }

    /// <summary>
    /// Modelo de 8 variables y pesos sugeridos para que otros sistemas
    /// arme su propio score ponderado. CreditPulse hoy NO calcula un score
    /// numérico propio: usa el Score de Buró que reporta la SIB tal cual, y
    /// un motor de políticas (BLL.PolicyEngine) que da un semáforo
    /// Verde/Amarillo/Rojo por cada regla configurada, no un número
    /// ponderado. Este modelo es un punto de partida razonable -variables y
    /// pesos típicos de un scorecard de crédito de consumo-, documentado
    /// como tal en el README para que se pueda recalibrar con datos reales
    /// más adelante.
    /// </summary>
    public static class VariablesScoreCalculator
    {
        public static VariablesScoreApiResponse Calcular(Evaluacion evaluacion, SibPerfil sib, bool datosSibCompletos)
        {
            int edadAnios = CalcularEdad(evaluacion.FechaNacimiento, DateTime.Today);
            decimal ingresosDeclarados = evaluacion.IngresosMensuales ?? 0m;
            int antiguedadMeses = evaluacion.AntiguedadLaboralMeses ?? 0;

            bool hayIngreso = ingresosDeclarados > 0m;
            decimal rci = hayIngreso
                ? Math.Round((sib.NivelEndeudamiento / 100m) * sib.IngresosReportados / ingresosDeclarados * 100m, 2)
                : 0m;
            string notaIngresoDependiente = hayIngreso ? null : "Esta evaluación no tiene un ingreso mensual declarado; no se pudo calcular.";

            var variables = new List<VariableScoreApiDto>
            {
                Variable("SCORE_BURO", "Score de Buró", sib.ScoreBuro, "puntos", 25m,
                    Normalizar(sib.ScoreBuro, 300, 850)),

                Variable("ENDEUDAMIENTO", "Nivel de Endeudamiento", sib.NivelEndeudamiento, "%", 15m,
                    Clamp(100m - sib.NivelEndeudamiento)),

                Variable("RCI_BANCARIO", "RCI Bancario (deudas / ingreso declarado)", rci, "%", 15m,
                    hayIngreso ? Clamp(100m - rci) : 50m, notaIngresoDependiente),

                Variable("MORAS_12M", "Moras en los Últimos 12 Meses", sib.MorasUltimos12Meses, "conteo", 15m,
                    Clamp(100m - sib.MorasUltimos12Meses * 20m)),

                Variable("DIAS_MORA_MAXIMO", "Días de Mora Máximo", sib.DiasMoraMaximo, "días", 5m,
                    Clamp(100m - (decimal)sib.DiasMoraMaximo / 120m * 100m)),

                Variable("INGRESOS_MENSUALES", "Ingresos Mensuales Declarados", ingresosDeclarados, "Q", 10m,
                    hayIngreso ? Clamp(ingresosDeclarados / 15000m * 100m) : 0m, notaIngresoDependiente),

                Variable("ANTIGUEDAD_LABORAL", "Antigüedad Laboral", antiguedadMeses, "meses", 10m,
                    Clamp((decimal)antiguedadMeses / 60m * 100m)),

                Variable("EDAD", "Edad", edadAnios, "años", 5m,
                    NormalizarEdad(edadAnios))
            };

            decimal sumaPesos = 0m;
            decimal scoreSugerido = 0m;
            foreach (var v in variables)
            {
                sumaPesos += v.PesoSugeridoPorcentaje;
                scoreSugerido += v.ValorNormalizado0a100 * (v.PesoSugeridoPorcentaje / 100m);
            }

            return new VariablesScoreApiResponse
            {
                EvaluacionId = evaluacion.EvaluacionId,
                DatosSibCompletos = datosSibCompletos,
                Variables = variables,
                SumaPesos = sumaPesos,
                ScoreSugerido0a100 = Math.Round(scoreSugerido, 2),
                Nota = "CreditPulse no calcula ni persiste un score numérico propio: este es un modelo de referencia " +
                       "(variables y pesos típicos de un scorecard de crédito de consumo) para que el sistema " +
                       "consumidor calcule su propio score, con sus propios pesos si lo prefiere. " +
                       "ScoreSugerido0a100 es solo un ejemplo de cómo combinarlas."
            };
        }

        private static VariableScoreApiDto Variable(string codigo, string nombre, decimal valor, string unidad,
            decimal peso, decimal normalizado, string nota = null)
        {
            return new VariableScoreApiDto
            {
                Codigo = codigo,
                Nombre = nombre,
                Valor = valor,
                Unidad = unidad,
                PesoSugeridoPorcentaje = peso,
                ValorNormalizado0a100 = normalizado,
                Nota = nota
            };
        }

        private static decimal Normalizar(decimal valor, decimal minimo, decimal maximo)
        {
            if (maximo <= minimo) return 50m;
            return Clamp((valor - minimo) / (maximo - minimo) * 100m);
        }

        private static decimal NormalizarEdad(int edad)
        {
            if (edad >= 25 && edad <= 60) return 100m;
            if (edad < 25) return Clamp(100m - (25 - edad) * 6m);
            return Clamp(100m - (edad - 60) * 4m);
        }

        private static decimal Clamp(decimal valor)
        {
            if (valor < 0m) return 0m;
            if (valor > 100m) return 100m;
            return valor;
        }

        private static int CalcularEdad(DateTime fechaNacimiento, DateTime hoy)
        {
            int edad = hoy.Year - fechaNacimiento.Year;
            if (fechaNacimiento.Date > hoy.AddYears(-edad)) edad--;
            return edad;
        }
    }
}
