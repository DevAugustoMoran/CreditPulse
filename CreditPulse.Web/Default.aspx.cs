using System;
using CreditPulse.Web.BLL;
using CreditPulse.Web.DAL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web
{
    public partial class Default : BasePage
    {
        private readonly EvaluacionRepository _evaluacionRepository = new EvaluacionRepository();
        private readonly LoteRepository _loteRepository = new LoteRepository();

        protected void Page_Load(object sender, EventArgs e)
        {
            bool puedeCrear = UsuarioActual.PuedeEvaluar;
            hlNuevaEvaluacion.Visible = puedeCrear;
            hlNuevaCargaBatch.Visible = puedeCrear;

            if (!IsPostBack)
            {
                CargarResumen();
                CargarLotesRecientes();
                CargarRecientes();
            }
        }

        private void CargarLotesRecientes()
        {
            gvLotes.DataSource = _loteRepository.ObtenerRecientes(5);
            gvLotes.DataBind();
        }

        private void CargarResumen()
        {
            var resumen = _evaluacionRepository.ObtenerResumenDashboard();
            litTotal.Text = resumen.Total.ToString();
            litVerde.Text = resumen.Verde.ToString();
            litAmarillo.Text = resumen.Amarillo.ToString();
            litRojo.Text = resumen.Rojo.ToString();
        }

        private void CargarRecientes()
        {
            gvRecientes.DataSource = _evaluacionRepository.ObtenerRecientes(15);
            gvRecientes.DataBind();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            int numero;
            if (!int.TryParse(txtNumeroBusqueda.Text.Trim(), out numero))
            {
                litBusquedaError.Text = "<p class=\"cp-field-error\">Ingresa un número de evaluación válido.</p>";
                return;
            }

            var evaluacion = _evaluacionRepository.ObtenerPorId(numero);
            if (evaluacion == null)
            {
                litBusquedaError.Text = "<p class=\"cp-field-error\">No se encontró ninguna evaluación con ese número.</p>";
                return;
            }

            Response.Redirect("~/Evaluacion/Resultado.aspx?id=" + numero, false);
        }

        /// <summary>Badge de estado para la grilla de "Cargas Batch recientes" (ver BLL.BatchEvaluationService, procesamiento en segundo plano).</summary>
        protected string RenderEstadoLote(Lote lote)
        {
            switch (lote.Estado)
            {
                case EstadoLote.Procesando:
                    return "<span class=\"badge badge-amarillo\">Procesando (" + lote.PorcentajeProgreso + "%)</span>";
                case EstadoLote.Error:
                    return "<span class=\"badge badge-rojo\" title=\"" + Server.HtmlEncode(lote.MensajeError ?? string.Empty) + "\">Error</span>";
                default:
                    return "<span class=\"badge badge-verde\">Completado</span>";
            }
        }
    }
}
