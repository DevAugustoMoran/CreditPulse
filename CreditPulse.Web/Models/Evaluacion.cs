using System;
using System.Collections.Generic;
using System.Linq;

namespace CreditPulse.Web.Models
{
    public class Evaluacion
    {
        public int EvaluacionId { get; set; }

        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; }

        public int UsuarioId { get; set; }
        public string AnalistaNombre { get; set; }

        public string PrimerNombre { get; set; }
        public string SegundoNombre { get; set; }

        /// <summary>Tercer nombre (opcional): algunos clientes tienen tres nombres de pila.</summary>
        public string TercerNombre { get; set; }
        public string PrimerApellido { get; set; }
        public string SegundoApellido { get; set; }

        public string NombreCompleto
        {
            get
            {
                return string.Join(" ", new[] { PrimerNombre, SegundoNombre, TercerNombre, PrimerApellido, SegundoApellido }
                    .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
            }
        }

        public string Nit { get; set; }
        public string Dpi { get; set; }
        public DateTime FechaNacimiento { get; set; }

        public decimal? IngresosMensuales { get; set; }
        public string TipoEmpleo { get; set; }
        public int? AntiguedadLaboralMeses { get; set; }

        public ResultadoSemaforo ResultadoGeneral { get; set; }
        public string Estado { get; set; }

        public string SibRawXml { get; set; }
        public int? SibScoreBuro { get; set; }
        public decimal? SibNivelEndeudamiento { get; set; }
        public bool? SibEnListaNegra { get; set; }

        public DateTime FechaCreacion { get; set; }

        public List<EvaluacionPolitica> Politicas { get; set; } = new List<EvaluacionPolitica>();
    }

    /// <summary>Resultado (snapshot) de una política individual dentro de una evaluación.</summary>
    public class EvaluacionPolitica
    {
        public int EvaluacionPoliticaId { get; set; }
        public int EvaluacionId { get; set; }
        public int? PoliticaId { get; set; }
        public string NombrePolitica { get; set; }
        public ResultadoSemaforo Resultado { get; set; }
        public string Detalle { get; set; }
        public int Orden { get; set; }
    }

    /// <summary>Fila resumida para el listado de evaluaciones recientes del dashboard.</summary>
    public class EvaluacionResumen
    {
        public int EvaluacionId { get; set; }
        public string ClienteNombre { get; set; }
        public string ProductoNombre { get; set; }
        public ResultadoSemaforo ResultadoGeneral { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    /// <summary>Contadores agregados que alimentan las tarjetas del dashboard.</summary>
    public class ResumenDashboard
    {
        public int Total { get; set; }
        public int Verde { get; set; }
        public int Amarillo { get; set; }
        public int Rojo { get; set; }
    }
}
