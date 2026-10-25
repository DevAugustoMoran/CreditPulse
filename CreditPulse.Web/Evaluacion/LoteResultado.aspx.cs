using System;
using CreditPulse.Web.BLL;
using CreditPulse.Web.DAL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Evaluaciones
{
    public partial class LoteResultado : BasePage
    {
        private readonly LoteRepository _loteRepository = new LoteRepository();

        protected void Page_Load(object sender, EventArgs e)
        {
            int loteId;
            if (!int.TryParse(Request.QueryString["id"], out loteId))
            {
                MostrarNoEncontrado();
                return;
            }

            Lote lote = _loteRepository.ObtenerPorId(loteId);
            if (lote == null)
            {
                MostrarNoEncontrado();
                return;
            }

            CargarLote(lote);
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            hlNuevaCargaBatch.Visible = UsuarioActual.PuedeEvaluar;
        }

        private void MostrarNoEncontrado()
        {
            pnlNoEncontrado.Visible = true;
            pnlContenido.Visible = false;
        }

        private void CargarLote(Lote lote)
        {
            litEncabezadoNumero.Text = "#" + lote.LoteId;
            litNombreArchivo.Text = Server.HtmlEncode(lote.NombreArchivo);
            litAnalista.Text = Server.HtmlEncode(lote.AnalistaNombre);
            litFecha.Text = ZonaHoraria.Formato(lote.FechaCreacion);

            bool procesando = lote.Estado == EstadoLote.Procesando;

            litTotal.Text = procesando
                ? lote.TotalProcesados + " / " + lote.TotalRegistros
                : lote.TotalProcesados.ToString();
            litVerde.Text = lote.TotalVerde.ToString();
            litAmarillo.Text = lote.TotalAmarillo.ToString();
            litRojo.Text = lote.TotalRojo.ToString();
            litErrores.Text = lote.TotalErrores.ToString();

            gvDetalle.DataSource = lote.Detalle;
            gvDetalle.DataBind();

            // Mientras el lote sigue en "Procesando" en segundo plano
            // (ver BatchEvaluationService.ProcesarEnSegundoPlano), esta
            // página se refresca sola para mostrar avance sin necesidad de
            // JavaScript/AJAX: suficiente para el alcance de este proyecto,
            // documentado como una simplificación deliberada en el README.
            metaRefresh.Visible = procesando;

            pnlProcesando.Visible = procesando;
            if (procesando)
            {
                litProgresoTexto.Text = string.Format(
                    "{0} de {1} filas procesadas ({2}%).", lote.TotalProcesados, lote.TotalRegistros, lote.PorcentajeProgreso);
                progresoFill.Attributes["style"] = "width:" + lote.PorcentajeProgreso + "%;";
            }

            pnlErrorLote.Visible = lote.Estado == EstadoLote.Error;
            if (pnlErrorLote.Visible)
            {
                litMensajeErrorLote.Text = Server.HtmlEncode(lote.MensajeError ?? "Error desconocido.");
            }
        }

        /// <summary>Badge de resultado para cada fila: color del semáforo, o "Error" con el motivo.</summary>
        protected string RenderResultado(LoteEvaluacion fila)
        {
            if (fila.Estado == EstadoFilaLote.Error)
            {
                string motivo = Server.HtmlEncode(fila.MensajeError ?? "Error desconocido");
                return "<span class=\"badge badge-rojo\" title=\"" + motivo + "\">Error</span> "
                     + "<span class=\"cp-row-error-msg\">" + motivo + "</span>";
            }

            if (!fila.ResultadoGeneral.HasValue)
            {
                return "<span class=\"badge badge-neutro\">Sin resultado</span>";
            }

            ResultadoSemaforo resultado = fila.ResultadoGeneral.Value;
            return "<span class=\"badge " + resultado.CssClass() + "\">" + resultado.Etiqueta() + "</span>";
        }

        protected string RenderAccion(LoteEvaluacion fila)
        {
            if (!fila.EvaluacionId.HasValue) return string.Empty;

            string url = ResolveUrl("~/Evaluacion/Resultado.aspx?id=" + fila.EvaluacionId.Value);
            return "<a class=\"cp-link\" href=\"" + url + "\">Ver detalle</a>";
        }
    }
}
