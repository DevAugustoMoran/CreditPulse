using System;
using System.Collections.Generic;

namespace CreditPulse.Web.Api.Models
{
    /// <summary>
    /// Cuerpo de POST /api/v1/evaluaciones: los mismos datos que pide
    /// Evaluacion/Nueva.aspx, para que otro sistema le pida a CreditPulse
    /// que evalúe a un cliente exactamente como lo haría un analista desde
    /// la plataforma.
    /// </summary>
    public class EvaluacionApiRequest
    {
        /// <summary>Código del producto (ver Administracion/Productos.aspx), ej. "TARJETA_CREDITO", "GENERAL".</summary>
        public string ProductoCodigo { get; set; }

        public string PrimerNombre { get; set; }
        public string SegundoNombre { get; set; }
        public string PrimerApellido { get; set; }
        public string SegundoApellido { get; set; }

        public string Nit { get; set; }
        public string Dpi { get; set; }
        public DateTime FechaNacimiento { get; set; }

        public decimal? IngresosMensuales { get; set; }
        public string TipoEmpleo { get; set; }
        public int? AntiguedadLaboralMeses { get; set; }
    }

    /// <summary>
    /// Respuesta de la API de evaluación: el mismo detalle que vería un
    /// analista en Evaluacion/Resultado.aspx (color general, cada política
    /// evaluada con su resultado y explicación, número de evaluación),
    /// serializado a JSON o XML según el header "Accept" del llamador.
    /// </summary>
    public class EvaluacionApiResponse
    {
        public int EvaluacionId { get; set; }

        public string ProductoCodigo { get; set; }
        public string ProductoNombre { get; set; }

        public string ClienteNombreCompleto { get; set; }
        public string Nit { get; set; }
        public string Dpi { get; set; }

        /// <summary>"VERDE" | "AMARILLO" | "ROJO".</summary>
        public string ResultadoGeneral { get; set; }

        /// <summary>"Aprobado" | "Requiere Revisión" | "Rechazado" (ver ResultadoSemaforoExtensions.Etiqueta).</summary>
        public string ResultadoGeneralEtiqueta { get; set; }

        public List<PoliticaEvaluadaApiDto> Politicas { get; set; }

        public SibResumenApiDto Sib { get; set; }

        public DateTime FechaEvaluacion { get; set; }
    }

    public class PoliticaEvaluadaApiDto
    {
        /// <summary>Id de la política en Administracion/Politicas.aspx (puede ser null si la política ya no existe).</summary>
        public int? PoliticaId { get; set; }

        public string Nombre { get; set; }

        /// <summary>"VERDE" | "AMARILLO" | "ROJO".</summary>
        public string Resultado { get; set; }

        public string Detalle { get; set; }
        public int Orden { get; set; }
    }

    /// <summary>
    /// Resumen de lo que reportó la SIB para esta evaluación específica
    /// (las mismas 3 columnas que ya se guardan en Evaluaciones, ver
    /// Database/02_CreateTables.sql). Para el detalle completo del perfil
    /// SIB de un cliente, ver la API de datos SIB (SibApiController).
    /// </summary>
    public class SibResumenApiDto
    {
        public int? ScoreBuro { get; set; }
        public decimal? NivelEndeudamiento { get; set; }
        public bool? EnListaNegra { get; set; }
    }
}
