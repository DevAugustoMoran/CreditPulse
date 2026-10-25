using System;

namespace CreditPulse.SibSimulador.Models
{
    /// <summary>
    /// Espejo de CreditPulse.Web.Models.SibPerfil, pero viviendo del lado del
    /// "servicio SIB" (este proyecto). No se comparte el ensamblado entre los
    /// dos proyectos a propósito: en la vida real, CreditPulse.Web y la SIB
    /// son sistemas de organizaciones distintas y no comparten código, solo
    /// un contrato XML. Ese contrato es el que hay que mantener idéntico al
    /// de CreditPulse.Web.BLL.SibMockService.SerializarXml para que
    /// CreditPulse.Web.BLL.SibXmlParser pueda leer la respuesta sin cambios.
    /// </summary>
    public class SibPerfilSimulado
    {
        public string Nit { get; set; }
        public string Dpi { get; set; }
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

        public bool ClienteExistente { get; set; }
        public int CreditosPreviosEnBanco { get; set; }

        public DateTime FechaConsulta { get; set; }
        public string FuenteConsultada { get; set; }
        public int TiempoRespuestaMs { get; set; }
    }
}
