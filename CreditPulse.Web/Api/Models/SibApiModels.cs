using System;

namespace CreditPulse.Web.Api.Models
{
    /// <summary>
    /// Respuesta de GET /api/v1/sib/perfil: los datos más importantes que
    /// devuelve la SIB (o el simulador, según el appSetting "UsarSibMock")
    /// para un cliente, en JSON/XML -para sistemas del ecosistema bancario
    /// que necesitan estos datos sin pasar por la pantalla de reporte de
    /// CreditPulse-. No incluye el XML crudo (eso es un detalle interno de
    /// cómo CreditPulse lo persiste, ver Evaluaciones.SibRawXml); aquí se
    /// exponen los campos ya interpretados, igual que en la pestaña
    /// "Información SIB" de Evaluacion/Resultado.aspx.
    /// </summary>
    public class SibApiResponse
    {
        public string Dpi { get; set; }
        public string Nit { get; set; }
        public string NombreCompleto { get; set; }

        public int ScoreBuro { get; set; }
        public decimal NivelEndeudamiento { get; set; }
        public decimal IngresosReportados { get; set; }

        public int CreditosActivos { get; set; }
        public int CreditosCancelados { get; set; }
        public int MorasUltimos12Meses { get; set; }
        public int DiasMoraMaximo { get; set; }

        public bool EnListaNegraLocal { get; set; }
        public bool EnListaNegraInternacional { get; set; }
        public bool EnListaOfac { get; set; }
        public bool EnListaNegra { get; set; }

        public bool ClienteExistente { get; set; }
        public int CreditosPreviosEnBanco { get; set; }

        public DateTime FechaConsulta { get; set; }
        public string FuenteConsultada { get; set; }
        public int TiempoRespuestaMs { get; set; }
    }
}
