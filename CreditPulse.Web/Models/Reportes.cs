using System;

namespace CreditPulse.Web.Models
{
    /// <summary>Una fila del reporte de evaluaciones por mes: color, cliente, score crediticio y fecha.</summary>
    public class ReporteEvaluacionFila
    {
        public int EvaluacionId { get; set; }
        public string ClienteNombre { get; set; }
        public string Dpi { get; set; }
        public string ProductoNombre { get; set; }
        public ResultadoSemaforo ResultadoGeneral { get; set; }
        public int? ScoreBuro { get; set; }

        /// <summary>Fecha de la evaluación, tal como se guarda: UTC.</summary>
        public DateTime FechaCreacion { get; set; }
    }

    /// <summary>Totales del período consultado (tarjetas de resumen del reporte de evaluaciones).</summary>
    public class ReporteEvaluacionesResumen
    {
        public int Total { get; set; }
        public int Verde { get; set; }
        public int Amarillo { get; set; }
        public int Rojo { get; set; }
        public double? ScorePromedio { get; set; }
    }

    /// <summary>
    /// Una fila del reporte de clientes de un batch: datos del cliente,
    /// información recolectada de la SIB, número de batch y fecha de
    /// evaluación. Las filas del Excel que fallaron validación no tienen
    /// evaluación asociada (EvaluacionId nulo) y traen su motivo de error.
    /// </summary>
    public class ReporteBatchFila
    {
        public int LoteId { get; set; }
        public int FilaExcel { get; set; }
        public EstadoFilaLote EstadoFila { get; set; }
        public string MensajeError { get; set; }
        public int? EvaluacionId { get; set; }

        // Lo que venía en el Excel (existe aunque la fila haya fallado).
        public string NombreCompletoOriginal { get; set; }
        public string NitOriginal { get; set; }
        public string DpiOriginal { get; set; }

        // Lo guardado en la evaluación (solo filas evaluadas).
        public string ClienteNombre { get; set; }
        public string Nit { get; set; }
        public string Dpi { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string ProductoNombre { get; set; }
        public ResultadoSemaforo? ResultadoGeneral { get; set; }

        // Resumen de la SIB en columnas planas + XML completo para el detalle.
        public int? ScoreBuro { get; set; }
        public decimal? NivelEndeudamiento { get; set; }
        public bool? EnListaNegra { get; set; }
        public string SibRawXml { get; set; }

        /// <summary>Fecha de la evaluación: UTC.</summary>
        public DateTime? FechaEvaluacion { get; set; }

        /// <summary>Perfil SIB interpretado desde <see cref="SibRawXml"/>; null si no hay XML o no se pudo leer.</summary>
        public SibPerfil Sib { get; set; }

        public string NombreParaMostrar
        {
            get { return !string.IsNullOrWhiteSpace(ClienteNombre) ? ClienteNombre : NombreCompletoOriginal; }
        }

        public string NitParaMostrar
        {
            get { return !string.IsNullOrWhiteSpace(Nit) ? Nit : NitOriginal; }
        }

        public string DpiParaMostrar
        {
            get { return !string.IsNullOrWhiteSpace(Dpi) ? Dpi : DpiOriginal; }
        }
    }
}
