using System;
using System.Globalization;
using CreditPulse.Web.BLL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Evaluaciones
{
    public partial class Resultado : BasePage
    {
        private readonly EvaluationService _evaluationService = new EvaluationService();

        protected void Page_Load(object sender, EventArgs e)
        {
            int evaluacionId;
            if (!int.TryParse(Request.QueryString["id"], out evaluacionId))
            {
                MostrarNoEncontrado();
                return;
            }

            Models.Evaluacion evaluacion = _evaluationService.ObtenerPorId(evaluacionId);
            if (evaluacion == null)
            {
                MostrarNoEncontrado();
                return;
            }

            CargarEvaluacion(evaluacion);
        }

        private void MostrarNoEncontrado()
        {
            pnlNoEncontrado.Visible = true;
            pnlContenido.Visible = false;
        }

        private void CargarEvaluacion(Models.Evaluacion evaluacion)
        {
            litEncabezadoNumero.Text = "#" + evaluacion.EvaluacionId;
            litCliente.Text = Server.HtmlEncode(evaluacion.NombreCompleto);
            litProducto.Text = Server.HtmlEncode(evaluacion.ProductoNombre);
            litNumero.Text = evaluacion.EvaluacionId.ToString();
            litFecha.Text = ZonaHoraria.Formato(evaluacion.FechaCreacion);
            litAnalista.Text = Server.HtmlEncode(evaluacion.AnalistaNombre);

            badgeResultado.InnerText = evaluacion.ResultadoGeneral.Etiqueta();
            badgeResultado.Attributes["class"] = "badge badge-lg " + evaluacion.ResultadoGeneral.CssClass();

            gvPoliticas.DataSource = evaluacion.Politicas;
            gvPoliticas.DataBind();

            CargarInformacionSib(evaluacion);
        }

        /// <summary>
        /// Arma el reporte legible de la pestaña "Información SIB". Se
        /// reconstruye a partir del XML crudo guardado (ver SibXmlParser)
        /// para no depender de columnas nuevas en Evaluaciones; si ese XML
        /// no se puede interpretar (por ejemplo, una evaluación muy antigua
        /// o un formato inesperado), se usa el resumen de 3 campos que sí
        /// está guardado en columnas planas, y si tampoco hay eso, se
        /// muestra el aviso de "sin datos".
        /// </summary>
        private void CargarInformacionSib(Models.Evaluacion evaluacion)
        {
            litSibXml.Text = Server.HtmlEncode(evaluacion.SibRawXml ?? "(sin datos)");

            SibPerfil perfil = SibXmlParser.Parsear(evaluacion.SibRawXml);

            if (perfil == null && !evaluacion.SibScoreBuro.HasValue)
            {
                pnlSibReporte.Visible = false;
                pnlSibSinDatos.Visible = true;
                return;
            }

            litSibNombreCliente.Text = Server.HtmlEncode(evaluacion.NombreCompleto);

            int score = perfil != null ? perfil.ScoreBuro : evaluacion.SibScoreBuro.Value;
            litSibScore.Text = score.ToString();
            litSibScoreTexto.Text = DescripcionScore(score);
            meterScore.Attributes["style"] = "width:" + PorcentajeScore(score).ToString(CultureInfo.InvariantCulture) + "%; background:" + ColorScore(score) + ";";

            decimal? endeudamiento = perfil != null ? perfil.NivelEndeudamiento : evaluacion.SibNivelEndeudamiento;
            litSibEndeudamiento.Text = endeudamiento.HasValue ? endeudamiento.Value.ToString("0.##", CultureInfo.InvariantCulture) + "%" : "N/D";

            if (perfil == null)
            {
                // Solo se guardó el resumen (Score/Endeudamiento/ListaNegra);
                // el resto del reporte no se puede reconstruir sin el XML.
                litSibIngresos.Text = "N/D";
                litCreditosActivos.Text = litCreditosCancelados.Text = litMoras.Text = litDiasMora.Text = "N/D";
                litClienteExistente.Text = litCreditosPrevios.Text = "N/D";
                litFechaConsulta.Text = litFuenteConsultada.Text = litTiempoRespuesta.Text = "N/D";

                bool? enListaNegra = evaluacion.SibEnListaNegra;
                string textoLista = !enListaNegra.HasValue ? "N/D" : BadgeSiNo(enListaNegra.Value, esRiesgo: true);
                litListaLocal.Text = textoLista;
                litListaInternacional.Text = "N/D";
                litListaOfac.Text = "N/D";
                return;
            }

            litSibIngresos.Text = "Q " + perfil.IngresosReportados.ToString("N2", CultureInfo.InvariantCulture);

            litCreditosActivos.Text = perfil.CreditosActivos.ToString();
            litCreditosCancelados.Text = perfil.CreditosCancelados.ToString();
            litMoras.Text = perfil.MorasUltimos12Meses.ToString();
            litDiasMora.Text = perfil.MorasUltimos12Meses > 0 ? perfil.DiasMoraMaximo + " días" : "Sin moras";

            litListaLocal.Text = BadgeSiNo(perfil.EnListaNegraLocal, esRiesgo: true);
            litListaInternacional.Text = BadgeSiNo(perfil.EnListaNegraInternacional, esRiesgo: true);
            litListaOfac.Text = BadgeSiNo(perfil.EnListaOfac, esRiesgo: true);

            litClienteExistente.Text = BadgeSiNo(perfil.ClienteExistente, esRiesgo: false);
            litCreditosPrevios.Text = perfil.CreditosPreviosEnBanco.ToString();

            litFechaConsulta.Text = perfil.FechaConsulta == DateTime.MinValue
                ? "N/D"
                : perfil.FechaConsulta.ToString("dd/MM/yyyy HH:mm:ss");
            litFuenteConsultada.Text = Server.HtmlEncode(perfil.FuenteConsultada ?? "N/D");
            litTiempoRespuesta.Text = perfil.TiempoRespuestaMs + " ms";
        }

        /// <summary>Badge Sí/No. Para banderas de riesgo (listas negras), "Sí" se marca en rojo; para datos neutrales (cliente existente), "Sí" es informativo.</summary>
        private static string BadgeSiNo(bool valor, bool esRiesgo)
        {
            if (!valor) return "<span class=\"badge badge-verde\">No</span>";
            return esRiesgo ? "<span class=\"badge badge-rojo\">Sí</span>" : "<span class=\"badge badge-neutro\">Sí</span>";
        }

        /// <summary>Rango orientativo 300-850 (escala típica de score crediticio) usado solo para la barra visual.</summary>
        private static double PorcentajeScore(int score)
        {
            double porcentaje = (score - 300) / (850.0 - 300.0) * 100.0;
            if (porcentaje < 3) porcentaje = 3;
            if (porcentaje > 100) porcentaje = 100;
            return Math.Round(porcentaje, 1);
        }

        private static string ColorScore(int score)
        {
            if (score >= 720) return "var(--cp-verde)";
            if (score >= 620) return "var(--cp-amarillo)";
            return "var(--cp-rojo)";
        }

        private static string DescripcionScore(int score)
        {
            if (score >= 720) return "Excelente historial crediticio.";
            if (score >= 620) return "Historial aceptable; algunas políticas pueden pedir revisión manual.";
            return "Historial débil; alto riesgo de incumplimiento.";
        }
    }
}
