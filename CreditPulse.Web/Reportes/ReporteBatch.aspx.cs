using System;
using System.Globalization;
using System.Web.UI.WebControls;
using CreditPulse.Web.BLL;
using CreditPulse.Web.DAL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Reportes
{
    /// <summary>
    /// Reporte de los clientes evaluados en un batch (carga masiva): datos
    /// del cliente, información recolectada de la SIB, número de batch y
    /// fecha de evaluación. El batch se elige en pantalla (lista o número) o
    /// llega por ?lote=N desde el detalle del lote. Solo lectura.
    /// </summary>
    public partial class ReporteBatch : BasePage
    {
        private const int TamanoPagina = 25;
        private readonly ReporteService _reporteService = new ReporteService();
        private readonly LoteRepository _loteRepository = new LoteRepository();

        private int PaginaActual
        {
            get { return ViewState["Pagina"] is int ? (int)ViewState["Pagina"] : 0; }
            set { ViewState["Pagina"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            CargarLotes();

            int loteId;
            if (int.TryParse(Request.QueryString["lote"], out loteId) && loteId > 0)
            {
                txtLote.Text = loteId.ToString(CultureInfo.InvariantCulture);
                SeleccionarEnLista(loteId);
                Consultar(loteId);
            }
        }

        private void CargarLotes()
        {
            ddlLotes.Items.Add(new ListItem("-- Selecciona un batch --", ""));

            try
            {
                foreach (Lote lote in _loteRepository.Listar(200))
                {
                    string texto = string.Format("Batch #{0} · {1} · {2} · {3} registros",
                        lote.LoteId, lote.NombreArchivo, ZonaHoraria.Formato(lote.FechaCreacion), lote.TotalRegistros);
                    ddlLotes.Items.Add(new ListItem(texto, lote.LoteId.ToString(CultureInfo.InvariantCulture)));
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MostrarError("No se pudo leer la lista de batches. ¿Ya ejecutaste Database/13_Reportes.sql? Detalle: " + ex.Message);
            }
        }

        private void SeleccionarEnLista(int loteId)
        {
            ListItem item = ddlLotes.Items.FindByValue(loteId.ToString(CultureInfo.InvariantCulture));
            if (item != null) ddlLotes.SelectedValue = item.Value;
        }

        protected void ddlLotes_SelectedIndexChanged(object sender, EventArgs e)
        {
            int loteId;
            if (int.TryParse(ddlLotes.SelectedValue, out loteId))
            {
                txtLote.Text = loteId.ToString(CultureInfo.InvariantCulture);
                PaginaActual = 0;
                Consultar(loteId);
            }
        }

        protected void btnGenerar_Click(object sender, EventArgs e)
        {
            int loteId;
            if (!LeerLote(out loteId)) return;

            SeleccionarEnLista(loteId);
            PaginaActual = 0;
            Consultar(loteId);
        }

        protected void btnAnterior_Click(object sender, EventArgs e)
        {
            int loteId;
            if (!LeerLote(out loteId)) return;
            if (PaginaActual > 0) PaginaActual--;
            Consultar(loteId);
        }

        protected void btnSiguiente_Click(object sender, EventArgs e)
        {
            int loteId;
            if (!LeerLote(out loteId)) return;
            PaginaActual++;
            Consultar(loteId);
        }

        private bool LeerLote(out int loteId)
        {
            if (int.TryParse((txtLote.Text ?? string.Empty).Trim(), out loteId) && loteId > 0) return true;

            pnlResultado.Visible = false;
            MostrarError("Escribe un número de batch válido o elígelo de la lista.");
            return false;
        }

        private void Consultar(int loteId)
        {
            try
            {
                Lote lote = _loteRepository.ObtenerEncabezado(loteId);
                if (lote == null)
                {
                    pnlResultado.Visible = false;
                    MostrarError(string.Format("No existe el batch #{0}.", loteId));
                    return;
                }

                int totalFilas = Math.Max(lote.TotalProcesados, 0);
                int totalPaginas = Math.Max(1, (int)Math.Ceiling(totalFilas / (double)TamanoPagina));
                if (PaginaActual >= totalPaginas) PaginaActual = totalPaginas - 1;

                litLoteNumero.Text = lote.LoteId.ToString(CultureInfo.InvariantCulture);
                litArchivo.Text = Server.HtmlEncode(lote.NombreArchivo);
                litFechaLote.Text = ZonaHoraria.Formato(lote.FechaCreacion);
                litAnalista.Text = Server.HtmlEncode(lote.AnalistaNombre);
                litEstadoLote.Text = RenderEstadoLote(lote);

                litTotal.Text = lote.TotalRegistros.ToString("N0", CultureInfo.InvariantCulture);
                litVerde.Text = lote.TotalVerde.ToString("N0", CultureInfo.InvariantCulture);
                litAmarillo.Text = lote.TotalAmarillo.ToString("N0", CultureInfo.InvariantCulture);
                litRojo.Text = lote.TotalRojo.ToString("N0", CultureInfo.InvariantCulture);
                litErrores.Text = lote.TotalErrores.ToString("N0", CultureInfo.InvariantCulture);

                gvBatch.DataSource = _reporteService.FilasDelBatch(loteId, PaginaActual, TamanoPagina);
                gvBatch.DataBind();

                btnAnterior.Enabled = PaginaActual > 0;
                btnSiguiente.Enabled = PaginaActual < totalPaginas - 1;
                litPagina.Text = string.Format("Página {0} de {1}", PaginaActual + 1, totalPaginas);

                pnlResultado.Visible = true;
                pnlMensajeError.Visible = false;
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                pnlResultado.Visible = false;
                MostrarError("No se pudo generar el reporte. ¿Ya ejecutaste Database/13_Reportes.sql? Detalle: " + ex.Message);
            }
        }

        protected void btnExportar_Click(object sender, EventArgs e) { Exportar(FormatoExportacion.Csv); }

        protected void btnExportarExcel_Click(object sender, EventArgs e) { Exportar(FormatoExportacion.Excel); }

        protected void btnExportarPdf_Click(object sender, EventArgs e) { Exportar(FormatoExportacion.Pdf); }

        private void Exportar(FormatoExportacion formato)
        {
            int loteId;
            if (!LeerLote(out loteId)) return;

            TablaReporte tabla;
            try
            {
                Lote lote = _loteRepository.ObtenerEncabezado(loteId);
                if (lote == null)
                {
                    pnlResultado.Visible = false;
                    MostrarError(string.Format("No existe el batch #{0}.", loteId));
                    return;
                }

                tabla = _reporteService.ConstruirTablaBatch(lote);
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MostrarError("No se pudo exportar el reporte: " + ex.Message);
                return;
            }

            ExportacionReporte.Enviar(Response, formato, tabla, "reporte-batch-" + loteId.ToString(CultureInfo.InvariantCulture), "Batch " + loteId.ToString(CultureInfo.InvariantCulture));
        }

        private void MostrarError(string mensaje)
        {
            litMensajeError.Text = Server.HtmlEncode(mensaje);
            pnlMensajeError.Visible = true;
        }

        // ---------- Render de celdas (todo texto de usuario va codificado) ----------

        private static string RenderEstadoLote(Lote lote)
        {
            switch (lote.Estado)
            {
                case EstadoLote.Procesando:
                    return "<span class=\"badge badge-amarillo\">Procesando (" + lote.PorcentajeProgreso + "%)</span>";
                case EstadoLote.Error:
                    return "<span class=\"badge badge-rojo\">Error</span>";
                default:
                    return "<span class=\"badge badge-verde\">Completado</span>";
            }
        }

        protected string RenderCliente(ReporteBatchFila f)
        {
            string fechaNac = f.FechaNacimiento.HasValue
                ? " · Nac. " + f.FechaNacimiento.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                : string.Empty;

            return "<strong>" + Server.HtmlEncode(f.NombreParaMostrar ?? string.Empty) + "</strong>"
                 + "<div class=\"cp-row-sub\">NIT " + Server.HtmlEncode(f.NitParaMostrar ?? "N/D")
                 + " · DPI " + Server.HtmlEncode(f.DpiParaMostrar ?? "N/D") + Server.HtmlEncode(fechaNac) + "</div>";
        }

        protected string RenderColor(ReporteBatchFila f)
        {
            if (f.EstadoFila == EstadoFilaLote.Error || !f.ResultadoGeneral.HasValue)
            {
                string motivo = Server.HtmlEncode(f.MensajeError ?? "Sin resultado");
                return "<span class=\"badge badge-rojo\" title=\"" + motivo + "\">Error</span>"
                     + "<div class=\"cp-row-sub\">" + motivo + "</div>";
            }

            return "<span class=\"badge " + f.ResultadoGeneral.Value.CssClass() + "\">" + ReporteService.NombreColor(f.ResultadoGeneral.Value) + "</span>";
        }

        protected string RenderScore(ReporteBatchFila f)
        {
            int? score = f.Sib != null ? f.Sib.ScoreBuro : f.ScoreBuro;
            return score.HasValue ? score.Value.ToString(CultureInfo.InvariantCulture) : "N/D";
        }

        protected string RenderEndeudamiento(ReporteBatchFila f)
        {
            decimal? valor = f.Sib != null ? f.Sib.NivelEndeudamiento : f.NivelEndeudamiento;
            return valor.HasValue ? valor.Value.ToString("0.##", CultureInfo.InvariantCulture) + "%" : "N/D";
        }

        protected string RenderIngresos(ReporteBatchFila f)
        {
            return f.Sib != null ? "Q " + f.Sib.IngresosReportados.ToString("N2", CultureInfo.InvariantCulture) : "N/D";
        }

        protected string RenderCreditos(ReporteBatchFila f)
        {
            return f.Sib != null
                ? f.Sib.CreditosActivos + " activos / " + f.Sib.CreditosCancelados + " cancelados"
                : "N/D";
        }

        protected string RenderMoras(ReporteBatchFila f)
        {
            if (f.Sib == null) return "N/D";
            return f.Sib.MorasUltimos12Meses > 0
                ? f.Sib.MorasUltimos12Meses + " (máx. " + f.Sib.DiasMoraMaximo + " días)"
                : "Sin moras";
        }

        protected string RenderListas(ReporteBatchFila f)
        {
            if (f.Sib == null)
            {
                if (!f.EnListaNegra.HasValue) return "N/D";
                return f.EnListaNegra.Value ? "<span class=\"badge badge-rojo\">En lista</span>" : "<span class=\"badge badge-verde\">Sin alertas</span>";
            }

            var alertas = new System.Collections.Generic.List<string>();
            if (f.Sib.EnListaNegraLocal) alertas.Add("Local");
            if (f.Sib.EnListaNegraInternacional) alertas.Add("Internacional");
            if (f.Sib.EnListaOfac) alertas.Add("OFAC");

            return alertas.Count == 0
                ? "<span class=\"badge badge-verde\">Sin alertas</span>"
                : "<span class=\"badge badge-rojo\">" + string.Join(" · ", alertas) + "</span>";
        }

        protected string RenderFecha(ReporteBatchFila f)
        {
            return f.FechaEvaluacion.HasValue ? ZonaHoraria.Formato(f.FechaEvaluacion.Value) : "—";
        }

        protected string RenderAccion(ReporteBatchFila f)
        {
            if (!f.EvaluacionId.HasValue) return string.Empty;
            return "<a class=\"cp-link\" href=\"" + ResolveUrl("~/Evaluacion/Resultado.aspx?id=" + f.EvaluacionId.Value) + "\">Ver detalle</a>";
        }
    }
}
