using System;

namespace CreditPulse.Web.Models
{
    /// <summary>
    /// Datos capturados en Evaluacion/Nueva.aspx. Se valida y se convierte
    /// en una <see cref="Evaluacion"/> persistida por
    /// <see cref="BLL.EvaluationService"/>.
    /// </summary>
    public class NuevaEvaluacionRequest
    {
        public int ProductoId { get; set; }
        public int UsuarioId { get; set; }

        public string PrimerNombre { get; set; }
        public string SegundoNombre { get; set; }

        /// <summary>Tercer nombre (opcional). Las APIs /api/v1 no lo envían: queda null.</summary>
        public string TercerNombre { get; set; }
        public string PrimerApellido { get; set; }
        public string SegundoApellido { get; set; }

        public string Nit { get; set; }
        public string Dpi { get; set; }
        public DateTime FechaNacimiento { get; set; }

        public decimal? IngresosMensuales { get; set; }
        public string TipoEmpleo { get; set; }
        public int? AntiguedadLaboralMeses { get; set; }
    }
}
