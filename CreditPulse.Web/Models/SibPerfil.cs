using System;
using System.Collections.Generic;

namespace CreditPulse.Web.Models
{
    /// <summary>
    /// Representa el perfil crediticio devuelto por la Superintendencia de
    /// Bancos (o por el simulador <see cref="BLL.SibMockService"/> mientras
    /// no se dispone de acceso a la API oficial). El PolicyEngine consume
    /// esta clase; el XML crudo se conserva aparte para el tab de
    /// "Información SIB" en la pantalla de resultado.
    /// </summary>
    public class SibPerfil
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
        public bool EnListaNegra { get { return EnListaNegraLocal || EnListaNegraInternacional || EnListaOfac; } }

        public bool ClienteExistente { get; set; }
        public int CreditosPreviosEnBanco { get; set; }

        public DateTime FechaConsulta { get; set; }
        public string FuenteConsultada { get; set; }
        public int TiempoRespuestaMs { get; set; }

        /// <summary>XML crudo tal como "lo devolvería" la SIB. Se persiste sin modificar.</summary>
        public string XmlCrudo { get; set; }
    }
}
