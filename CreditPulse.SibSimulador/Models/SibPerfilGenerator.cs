using System;
using System.Globalization;
using System.Text;

namespace CreditPulse.SibSimulador.Models
{
    /// <summary>
    /// Generador determinístico del perfil crediticio simulado, y su
    /// serializador a XML. Es una copia intencional del algoritmo de
    /// CreditPulse.Web.BLL.SibMockService: los dos proyectos representan
    /// sistemas distintos (CreditPulse y la SIB) que no comparten código en
    /// la realidad, solo un contrato de datos, así que aquí se duplica el
    /// generador en lugar de referenciar el ensamblado de CreditPulse.Web.
    ///
    /// El mismo DPI siempre produce el mismo perfil (semilla derivada del
    /// DPI), para que las evaluaciones sean reproducibles en pruebas y para
    /// que, si se corre este simulador Y el simulador interno de
    /// CreditPulse.Web con el mismo DPI, el resultado sea idéntico.
    /// </summary>
    public static class SibPerfilGenerator
    {
        public static SibPerfilSimulado Generar(string dpi, string nit, string nombreCompleto)
        {
            int seed = ComputeSeed(dpi);
            var random = new Random(seed);

            int score = random.Next(420, 850);
            decimal endeudamiento = Math.Round((decimal)(random.NextDouble() * 90), 2);
            int moras = WeightedMoras(random);
            int diasMoraMaximo = moras == 0 ? 0 : random.Next(1, 120);
            bool enListaNegraLocal = random.NextDouble() < 0.04;
            bool enListaNegraInternacional = random.NextDouble() < 0.01;
            bool enListaOfac = random.NextDouble() < 0.005;
            bool clienteExistente = random.NextDouble() < 0.35;
            int creditosActivos = random.Next(0, 5);
            int creditosCancelados = random.Next(0, 12);
            int creditosPreviosEnBanco = clienteExistente ? random.Next(1, 4) : 0;
            decimal ingresosReportados = random.Next(1800, 25000);
            int tiempoRespuestaMs = random.Next(180, 1400);

            var perfil = new SibPerfilSimulado
            {
                Nit = nit,
                Dpi = dpi,
                NombreCompleto = nombreCompleto,
                ScoreBuro = score,
                NivelEndeudamiento = endeudamiento,
                IngresosReportados = ingresosReportados,
                CreditosActivos = creditosActivos,
                CreditosCancelados = creditosCancelados,
                MorasUltimos12Meses = moras,
                DiasMoraMaximo = diasMoraMaximo,
                EnListaNegraLocal = enListaNegraLocal,
                EnListaNegraInternacional = enListaNegraInternacional,
                EnListaOfac = enListaOfac,
                ClienteExistente = clienteExistente,
                CreditosPreviosEnBanco = creditosPreviosEnBanco,
                FechaConsulta = DateTime.UtcNow.AddHours(-6), // hora de Guatemala (UTC-6, sin horario de verano), independiente de la zona del servidor
                FuenteConsultada = "SIB-API (CreditPulse.SibSimulador, servicio HTTP independiente)",
                TiempoRespuestaMs = tiempoRespuestaMs
            };

            return perfil;
        }

        /// <summary>Semilla estable a partir del DPI: el mismo DPI siempre produce el mismo perfil.</summary>
        private static int ComputeSeed(string dpi)
        {
            unchecked
            {
                int hash = 17;
                foreach (char c in dpi ?? string.Empty)
                {
                    hash = hash * 31 + c;
                }
                return hash & 0x7FFFFFFF;
            }
        }

        private static int WeightedMoras(Random random)
        {
            double roll = random.NextDouble();
            if (roll < 0.65) return 0;
            if (roll < 0.85) return random.Next(1, 3);
            return random.Next(3, 7);
        }

        /// <summary>
        /// Serializa el perfil al MISMO esquema XML que
        /// CreditPulse.Web.BLL.SibMockService.SerializarXml, para que
        /// CreditPulse.Web.BLL.SibXmlParser pueda leer la respuesta de este
        /// servicio HTTP sin ningún cambio.
        /// </summary>
        public static string SerializarXml(SibPerfilSimulado p)
        {
            var culture = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<PerfilCrediticioSIB>");
            sb.AppendLine("  <Solicitante>");
            sb.AppendLine($"    <Nit>{System.Security.SecurityElement.Escape(p.Nit)}</Nit>");
            sb.AppendLine($"    <Dpi>{System.Security.SecurityElement.Escape(p.Dpi)}</Dpi>");
            sb.AppendLine($"    <NombreCompleto>{System.Security.SecurityElement.Escape(p.NombreCompleto)}</NombreCompleto>");
            sb.AppendLine("  </Solicitante>");
            sb.AppendLine($"  <ScoreBuro>{p.ScoreBuro}</ScoreBuro>");
            sb.AppendLine($"  <NivelEndeudamiento>{p.NivelEndeudamiento.ToString(culture)}</NivelEndeudamiento>");
            sb.AppendLine($"  <IngresosReportados>{p.IngresosReportados.ToString(culture)}</IngresosReportados>");
            sb.AppendLine("  <HistorialCrediticio>");
            sb.AppendLine($"    <CreditosActivos>{p.CreditosActivos}</CreditosActivos>");
            sb.AppendLine($"    <CreditosCancelados>{p.CreditosCancelados}</CreditosCancelados>");
            sb.AppendLine($"    <MorasUltimos12Meses>{p.MorasUltimos12Meses}</MorasUltimos12Meses>");
            sb.AppendLine($"    <DiasMoraMaximo>{p.DiasMoraMaximo}</DiasMoraMaximo>");
            sb.AppendLine("  </HistorialCrediticio>");
            sb.AppendLine("  <ListasDeRiesgo>");
            sb.AppendLine($"    <EnListaNegraLocal>{p.EnListaNegraLocal.ToString().ToLowerInvariant()}</EnListaNegraLocal>");
            sb.AppendLine($"    <EnListaNegraInternacional>{p.EnListaNegraInternacional.ToString().ToLowerInvariant()}</EnListaNegraInternacional>");
            sb.AppendLine($"    <EnListaOFAC>{p.EnListaOfac.ToString().ToLowerInvariant()}</EnListaOFAC>");
            sb.AppendLine("  </ListasDeRiesgo>");
            sb.AppendLine("  <ReferenciasInternas>");
            sb.AppendLine($"    <ClienteExistente>{p.ClienteExistente.ToString().ToLowerInvariant()}</ClienteExistente>");
            sb.AppendLine($"    <CreditosPreviosEnBanco>{p.CreditosPreviosEnBanco}</CreditosPreviosEnBanco>");
            sb.AppendLine("  </ReferenciasInternas>");
            sb.AppendLine("  <MetadatosConsulta>");
            sb.AppendLine($"    <FechaConsulta>{p.FechaConsulta:yyyy-MM-ddTHH:mm:ss}</FechaConsulta>");
            sb.AppendLine($"    <FuenteConsultada>{System.Security.SecurityElement.Escape(p.FuenteConsultada)}</FuenteConsultada>");
            sb.AppendLine($"    <TiempoRespuestaMs>{p.TiempoRespuestaMs}</TiempoRespuestaMs>");
            sb.AppendLine("  </MetadatosConsulta>");
            sb.AppendLine("</PerfilCrediticioSIB>");

            return sb.ToString();
        }
    }
}
