using System;

namespace CreditPulse.Web.Models
{
    /// <summary>
    /// Resultado del semáforo de evaluación. El valor numérico se usa
    /// únicamente para poder comparar severidad (Rojo &gt; Amarillo &gt; Verde)
    /// al calcular el resultado general de una evaluación.
    /// </summary>
    public enum ResultadoSemaforo
    {
        Verde = 0,
        Amarillo = 1,
        Rojo = 2
    }

    public static class ResultadoSemaforoExtensions
    {
        /// <summary>Convierte el enum al literal que se guarda en la base de datos.</summary>
        public static string ToDbValue(this ResultadoSemaforo resultado)
        {
            switch (resultado)
            {
                case ResultadoSemaforo.Verde: return "VERDE";
                case ResultadoSemaforo.Amarillo: return "AMARILLO";
                case ResultadoSemaforo.Rojo: return "ROJO";
                default: throw new ArgumentOutOfRangeException(nameof(resultado));
            }
        }

        public static ResultadoSemaforo FromDbValue(string valor)
        {
            switch ((valor ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "VERDE": return ResultadoSemaforo.Verde;
                case "AMARILLO": return ResultadoSemaforo.Amarillo;
                case "ROJO": return ResultadoSemaforo.Rojo;
                default: throw new ArgumentException("Valor de semáforo desconocido: " + valor);
            }
        }

        /// <summary>Clase CSS del badge de semáforo usada en las páginas .aspx.</summary>
        public static string CssClass(this ResultadoSemaforo resultado)
        {
            switch (resultado)
            {
                case ResultadoSemaforo.Verde: return "badge-verde";
                case ResultadoSemaforo.Amarillo: return "badge-amarillo";
                case ResultadoSemaforo.Rojo: return "badge-rojo";
                default: return "badge-neutro";
            }
        }

        public static string Etiqueta(this ResultadoSemaforo resultado)
        {
            switch (resultado)
            {
                case ResultadoSemaforo.Verde: return "Aprobado";
                case ResultadoSemaforo.Amarillo: return "Requiere Revisión";
                case ResultadoSemaforo.Rojo: return "Rechazado";
                default: return "Desconocido";
            }
        }
    }

    /// <summary>
    /// Tipos de regla que entiende el PolicyEngine. El código (string) de cada
    /// política en la tabla Politicas debe coincidir con uno de estos valores.
    /// </summary>
    public static class TiposDeRegla
    {
        public const string EdadMinima = "EDAD_MINIMA";
        public const string EdadMaxima = "EDAD_MAXIMA";
        public const string ListaNegraSib = "LISTA_NEGRA_SIB";
        public const string ScoreBuroMinimo = "SCORE_BURO_MINIMO";
        public const string EndeudamientoMaximo = "ENDEUDAMIENTO_MAXIMO";
        public const string IngresoMinimo = "INGRESO_MINIMO";
        public const string HistorialMoras = "HISTORIAL_MORAS";
        public const string AntiguedadLaboralMinima = "ANTIGUEDAD_LABORAL_MINIMA";
        public const string RciBancario = "RCI_BANCARIO";

        /// <summary>
        /// Catálogo de tipos de regla reconocidos por el PolicyEngine, con un
        /// nombre amigable y un ejemplo de ParametrosJson. Alimenta el
        /// dropdown y la ayuda dinámica de Administracion/PoliticaForm.aspx,
        /// y sirve para validar que no se guarde un TipoRegla con un typo.
        /// </summary>
        public static readonly TipoReglaInfo[] Catalogo =
        {
            new TipoReglaInfo(EdadMinima, "Edad Mínima", "{\"min\":18}"),
            new TipoReglaInfo(EdadMaxima, "Edad Máxima", "{\"max\":70,\"amarilloDesde\":65}"),
            new TipoReglaInfo(ListaNegraSib, "Verificación en Listas de Riesgo SIB", "{}"),
            new TipoReglaInfo(ScoreBuroMinimo, "Score de Buró Mínimo", "{\"verde\":650,\"amarillo\":580}"),
            new TipoReglaInfo(EndeudamientoMaximo, "Endeudamiento Máximo", "{\"verde\":40,\"amarillo\":60}"),
            new TipoReglaInfo(IngresoMinimo, "Ingreso Mínimo", "{\"minimo\":3000,\"toleranciaPorcentaje\":10}"),
            new TipoReglaInfo(HistorialMoras, "Historial de Moras", "{\"verde\":0,\"amarillo\":2}"),
            new TipoReglaInfo(AntiguedadLaboralMinima, "Antigüedad Laboral Mínima", "{\"verdeMeses\":24,\"amarilloMeses\":12}"),
            new TipoReglaInfo(RciBancario, "RCI Bancario (Relación Cuota/Ingreso)", "{\"verde\":35,\"amarillo\":45}")
        };

        public static bool EsValido(string tipoRegla)
        {
            foreach (var info in Catalogo)
            {
                if (string.Equals(info.Codigo, tipoRegla, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }

    public class TipoReglaInfo
    {
        public string Codigo { get; }
        public string NombreAmigable { get; }
        public string EjemploParametros { get; }

        public TipoReglaInfo(string codigo, string nombreAmigable, string ejemploParametros)
        {
            Codigo = codigo;
            NombreAmigable = nombreAmigable;
            EjemploParametros = ejemploParametros;
        }
    }
}
