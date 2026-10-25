using System;
using System.Globalization;
using System.Xml.Linq;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.BLL
{
    /// <summary>
    /// Reconstruye un <see cref="SibPerfil"/> legible a partir del XML crudo
    /// que ya se guarda en Evaluaciones.SibRawXml (ver BLL.SibMockService).
    /// Se eligió esta ruta -reinterpretar el mismo XML que ya se persiste-
    /// en vez de agregar columnas nuevas a la tabla Evaluaciones para cada
    /// campo del perfil: así el reporte visual de Evaluacion/Resultado.aspx
    /// (pestaña "Información SIB") no depende de un cambio de esquema, y
    /// sigue funcionando igual el día que SibMockService se reemplace por un
    /// cliente real de la SIB (con tal de que la forma del XML no cambie; si
    /// cambia, este es el único lugar que hay que tocar).
    /// </summary>
    public static class SibXmlParser
    {
        public static SibPerfil Parsear(string xmlCrudo)
        {
            if (string.IsNullOrWhiteSpace(xmlCrudo)) return null;

            try
            {
                XDocument doc = XDocument.Parse(xmlCrudo);
                XElement root = doc.Root;
                if (root == null) return null;

                XElement solicitante = root.Element("Solicitante");
                XElement historial = root.Element("HistorialCrediticio");
                XElement listas = root.Element("ListasDeRiesgo");
                XElement referencias = root.Element("ReferenciasInternas");
                XElement metadatos = root.Element("MetadatosConsulta");

                return new SibPerfil
                {
                    Nit = Texto(solicitante, "Nit"),
                    Dpi = Texto(solicitante, "Dpi"),
                    NombreCompleto = Texto(solicitante, "NombreCompleto"),

                    ScoreBuro = EnteroDe(root, "ScoreBuro"),
                    NivelEndeudamiento = DecimalDe(root, "NivelEndeudamiento"),
                    IngresosReportados = DecimalDe(root, "IngresosReportados"),

                    CreditosActivos = EnteroDe(historial, "CreditosActivos"),
                    CreditosCancelados = EnteroDe(historial, "CreditosCancelados"),
                    MorasUltimos12Meses = EnteroDe(historial, "MorasUltimos12Meses"),
                    DiasMoraMaximo = EnteroDe(historial, "DiasMoraMaximo"),

                    EnListaNegraLocal = BoolDe(listas, "EnListaNegraLocal"),
                    EnListaNegraInternacional = BoolDe(listas, "EnListaNegraInternacional"),
                    EnListaOfac = BoolDe(listas, "EnListaOFAC"),

                    ClienteExistente = BoolDe(referencias, "ClienteExistente"),
                    CreditosPreviosEnBanco = EnteroDe(referencias, "CreditosPreviosEnBanco"),

                    FechaConsulta = FechaDe(metadatos, "FechaConsulta"),
                    FuenteConsultada = Texto(metadatos, "FuenteConsultada"),
                    TiempoRespuestaMs = EnteroDe(metadatos, "TiempoRespuestaMs"),

                    XmlCrudo = xmlCrudo
                };
            }
            catch (System.Xml.XmlException)
            {
                // XML corrupto o de un formato que este parser no reconoce: se
                // deja que el llamador muestre el resumen guardado en las
                // columnas planas (Score/Endeudamiento/ListaNegra) en vez de
                // fallar toda la pantalla de resultado.
                return null;
            }
        }

        private static string Texto(XElement padre, string hijo)
        {
            return padre?.Element(hijo)?.Value;
        }

        private static int EnteroDe(XElement padre, string hijo)
        {
            int valor;
            return int.TryParse(Texto(padre, hijo), NumberStyles.Integer, CultureInfo.InvariantCulture, out valor) ? valor : 0;
        }

        private static decimal DecimalDe(XElement padre, string hijo)
        {
            decimal valor;
            return decimal.TryParse(Texto(padre, hijo), NumberStyles.Number, CultureInfo.InvariantCulture, out valor) ? valor : 0m;
        }

        private static bool BoolDe(XElement padre, string hijo)
        {
            bool valor;
            return bool.TryParse(Texto(padre, hijo), out valor) && valor;
        }

        private static DateTime FechaDe(XElement padre, string hijo)
        {
            DateTime valor;
            return DateTime.TryParse(Texto(padre, hijo), CultureInfo.InvariantCulture, DateTimeStyles.None, out valor) ? valor : DateTime.MinValue;
        }
    }
}
