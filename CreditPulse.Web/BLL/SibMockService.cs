using System;
using System.Globalization;
using System.Text;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.BLL
{
    /// <summary>
    /// Simulador de la consulta a la Superintendencia de Bancos (SIB).
    /// Mientras no exista acceso a la API oficial, este servicio genera un
    /// perfil crediticio plausible en XML -tal como lo devolvería la SIB- de
    /// forma determinística: el mismo DPI siempre produce el mismo perfil,
    /// lo que permite hacer pruebas repetibles del motor de políticas.
    ///
    /// Para conectar la API real en el futuro basta con implementar
    /// <see cref="ISibService"/> contra el webservice oficial y registrar esa
    /// implementación en lugar de <see cref="SibMockService"/> (ver appSetting
    /// "UsarSibMock" en Web.config).
    /// </summary>
    public interface ISibService
    {
        SibPerfil ConsultarPerfil(string dpi, string nit, string nombreCompleto);
    }

    public class SibMockService : ISibService
    {
        public SibPerfil ConsultarPerfil(string dpi, string nit, string nombreCompleto)
        {
            int seed = ComputeSeed(dpi);
            var random = new Random(seed);

            int score = random.Next(420, 850);
            decimal endeudamiento = Math.Round((decimal)(random.NextDouble() * 90), 2);
            int moras = WeightedMoras(random);
            int diasMoraMaximo = moras == 0 ? 0 : random.Next(1, 120);
            bool enListaNegraLocal = random.NextDouble() < 0.04;          // ~4% de los perfiles
            bool enListaNegraInternacional = random.NextDouble() < 0.01;  // ~1%
            bool enListaOfac = random.NextDouble() < 0.005;               // ~0.5%
            bool clienteExistente = random.NextDouble() < 0.35;
            int creditosActivos = random.Next(0, 5);
            int creditosCancelados = random.Next(0, 12);
            int creditosPreviosEnBanco = clienteExistente ? random.Next(1, 4) : 0;
            decimal ingresosReportados = random.Next(1800, 25000);
            int tiempoRespuestaMs = random.Next(180, 1400);

            var perfil = new SibPerfil
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
                FechaConsulta = ZonaHoraria.Ahora(), // hora de Guatemala, independiente de la zona del servidor
                FuenteConsultada = "SIB-MOCK (simulador local, art. 133 Ley de Bancos)",
                TiempoRespuestaMs = tiempoRespuestaMs
            };

            perfil.XmlCrudo = SerializarXml(perfil);
            return perfil;
        }

        /// <summary>Semilla estable a partir del DPI, para que el mismo cliente siempre reciba el mismo perfil simulado.</summary>
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

        /// <summary>La mayoría de perfiles no tiene moras; una minoría sí, para poder probar los caminos amarillo/rojo.</summary>
        private static int WeightedMoras(Random random)
        {
            double roll = random.NextDouble();
            if (roll < 0.65) return 0;
            if (roll < 0.85) return random.Next(1, 3);
            return random.Next(3, 7);
        }

        private static string SerializarXml(SibPerfil p)
        {
            var culture = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            // Nota: se omite el atributo "encoding" en la declaración XML a propósito.
            // SQL Server rechaza la conversión implícita NVARCHAR -> XML si el prólogo
            // declara una codificación distinta de UTF-16 (el string .NET ya está en
            // memoria como UTF-16), así que sp_Evaluaciones_Crear fallaría al guardar
            // este texto en la columna SibRawXml (tipo XML) si se incluyera encoding="UTF-8".
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
