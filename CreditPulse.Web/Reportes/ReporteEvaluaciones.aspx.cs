using System;
using System.Globalization;
using System.Web.UI.WebControls;
using CreditPulse.Web.BLL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Reportes
{
    /// <summary>
    /// Reporte de evaluaciones por mes (color, cliente, score crediticio,
    /// fecha). Es de solo lectura: cualquier usuario autenticado puede
    /// consultarlo (igual que el Dashboard).
    /// </summary>
    public partial class ReporteEvaluaciones : BasePage
    {
        private const int TamanoPagina = 25;
        private readonly ReporteService _reporteService = new ReporteService();

        private int PaginaActual
        {
            get { return ViewState["Pagina"] is int ? (int)ViewState["Pagina"] : 0; }
            set { ViewState["Pagina"] = value; }
        }

        private int TotalFiltrado
        {
            get { return ViewState["Total"] is int ? (int)ViewState["Total"] : 0; }
            set { ViewState["Total"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            CargarFiltros();
            Consultar();
        }

        private void CargarFiltros()
        {
            DateTime hoy = ZonaHoraria.Ahora();

            for (int mes = 1; mes <= 12; mes++)
            {
                ddlMes.Items.Add(new ListItem(ReporteService.NombreMes(mes), mes.ToString(CultureInfo.InvariantCulture)));
            }

            for (int anio = hoy.Year; anio >= hoy.Year - 5; anio--)
            {
                ddlAnio.Items.Add(new ListItem(anio.ToString(CultureInfo.InvariantCulture), anio.ToString(CultureInfo.InvariantCulture)));
            }

            ddlMes.SelectedValue = hoy.Month.ToString(CultureInfo.InvariantCulture);
            ddlAnio.SelectedValue = hoy.Year.ToString(CultureInfo.InvariantCulture);
        }

        protected void btnGenerar_Click(object sender, EventArgs e)
        {
            PaginaActual = 0;
            Consultar();
        }

        protected void btnAnterior_Click(object sender, EventArgs e)
        {
            if (PaginaActual > 0) PaginaActual--;
            Consultar();
        }

        protected void btnSiguiente_Click(object sender, EventArgs e)
        {
            PaginaActual++;
            Consultar();
        }

        private bool LeerFiltros(out int anio, out int mes, out ResultadoSemaforo? color)
        {
            color = string.IsNullOrEmpty(ddlColor.SelectedValue)
                ? (ResultadoSemaforo?)null
                : ResultadoSemaforoExtensions.FromDbValue(ddlColor.SelectedValue);

            anio = 0; mes = 0;
            return int.TryParse(ddlAnio.SelectedValue, out anio) && int.TryParse(ddlMes.SelectedValue, out mes) && mes >= 1 && mes <= 12;
        }

        private void Consultar()
        {
            int anio, mes;
            ResultadoSemaforo? color;
            if (!LeerFiltros(out anio, out mes, out color))
            {
                MostrarError("Selecciona un mes y un año válidos.");
                return;
            }

            try
            {
                ReporteEvaluacionesResumen resumen = _reporteService.ResumenDelMes(anio, mes, color);
                TotalFiltrado = resumen.Total;

                int totalPaginas = Math.Max(1, (int)Math.Ceiling(resumen.Total / (double)TamanoPagina));
                if (PaginaActual >= totalPaginas) PaginaActual = totalPaginas - 1;

                litTitulo.Text = Server.HtmlEncode(string.Format("Evaluaciones de {0} {1}", ReporteService.NombreMes(mes), anio));
                litTotal.Text = resumen.Total.ToString("N0", CultureInfo.InvariantCulture);
                litVerde.Text = resumen.Verde.ToString("N0", CultureInfo.InvariantCulture);
                litAmarillo.Text = resumen.Amarillo.ToString("N0", CultureInfo.InvariantCulture);
                litRojo.Text = resumen.Rojo.ToString("N0", CultureInfo.InvariantCulture);
                litScorePromedio.Text = resumen.ScorePromedio.HasValue
                    ? Math.Round(resumen.ScorePromedio.Value).ToString("0", CultureInfo.InvariantCulture)
                    : "N/D";

                gvEvaluaciones.DataSource = _reporteService.EvaluacionesDelMes(anio, mes, color, PaginaActual, TamanoPagina);
                gvEvaluaciones.DataBind();

                btnAnterior.Enabled = PaginaActual > 0;
                btnSiguiente.Enabled = PaginaActual < totalPaginas - 1;
                litPagina.Text = string.Format("Página {0} de {1}", PaginaActual + 1, totalPaginas);

                pnlResultado.Visible = true;
                pnlMensajeError.Visible = false;
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                pnlResultado.Visible = false;
                MostrarError("No se pudo generar el reporte. ¿Ya ejecutaste Database/13_Reportes.sql en la base de datos? Detalle: " + ex.Message);
            }
        }

        protected void btnExportar_Click(object sender, EventArgs e) { Exportar(FormatoExportacion.Csv); }

        protected void btnExportarExcel_Click(object sender, EventArgs e) { Exportar(FormatoExportacion.Excel); }

        protected void btnExportarPdf_Click(object sender, EventArgs e) { Exportar(FormatoExportacion.Pdf); }

        private void Exportar(FormatoExportacion formato)
        {
            int anio, mes;
            ResultadoSemaforo? color;
            if (!LeerFiltros(out anio, out mes, out color))
            {
                MostrarError("Selecciona un mes y un año válidos.");
                return;
            }

            TablaReporte tabla;
            try
            {
                tabla = _reporteService.ConstruirTablaEvaluaciones(anio, mes, color);
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MostrarError("No se pudo exportar el reporte: " + ex.Message);
                return;
            }

            string nombre = string.Format("reporte-evaluaciones-{0}-{1:00}", anio, mes);
            ExportacionReporte.Enviar(Response, formato, tabla, nombre, "Evaluaciones " + ReporteService.NombreMes(mes) + " " + anio);
        }

        private void MostrarError(string mensaje)
        {
            litMensajeError.Text = Server.HtmlEncode(mensaje);
            pnlMensajeError.Visible = true;
        }

        protected string RenderCliente(ReporteEvaluacionFila fila)
        {
            return "<strong>" + Server.HtmlEncode(fila.ClienteNombre) + "</strong>"
                 + "<div class=\"cp-row-sub\">DPI " + Server.HtmlEncode(fila.Dpi) + "</div>";
        }

        protected string RenderColor(ReporteEvaluacionFila fila)
        {
            return "<span class=\"badge " + fila.ResultadoGeneral.CssClass() + "\">" + ReporteService.NombreColor(fila.ResultadoGeneral) + "</span>";
        }

        protected string RenderScore(ReporteEvaluacionFila fila)
        {
            return fila.ScoreBuro.HasValue ? fila.ScoreBuro.Value.ToString(CultureInfo.InvariantCulture) : "N/D";
        }
    }
}
