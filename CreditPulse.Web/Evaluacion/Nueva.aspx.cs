using System;
using System.Globalization;
using CreditPulse.Web.BLL;
using CreditPulse.Web.DAL;
using CreditPulse.Web.Models;
using CreditPulse.Web.Security;

namespace CreditPulse.Web.Evaluaciones
{
    public partial class Nueva : BasePage
    {
        private readonly ProductoRepository _productoRepository = new ProductoRepository();
        private readonly EvaluationService _evaluationService = new EvaluationService();

        protected override Permiso? PermisoRequerido
        {
            get { return Permiso.Evaluar; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarProductos();
            }
        }

        private void CargarProductos()
        {
            ddlProducto.DataSource = _productoRepository.ObtenerActivos();
            ddlProducto.DataTextField = "Nombre";
            ddlProducto.DataValueField = "ProductoId";
            ddlProducto.DataBind();
        }

        protected void ddlProducto_SelectedIndexChanged(object sender, EventArgs e)
        {
            AplicarCamposDinamicos();
        }

        private void AplicarCamposDinamicos()
        {
            int productoId;
            if (!int.TryParse(ddlProducto.SelectedValue, out productoId))
            {
                pnlDatosAdicionales.Visible = false;
                return;
            }

            Producto producto = _productoRepository.ObtenerPorId(productoId);
            if (producto == null)
            {
                pnlDatosAdicionales.Visible = false;
                return;
            }

            pnlIngresos.Visible = producto.RequiereIngresos;
            pnlTipoEmpleo.Visible = producto.RequiereTipoEmpleo;
            pnlAntiguedad.Visible = producto.RequiereAntiguedadLaboral;

            pnlDatosAdicionales.Visible = producto.RequiereIngresos || producto.RequiereTipoEmpleo || producto.RequiereAntiguedadLaboral;
        }

        protected void btnEvaluar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            try
            {
                var usuario = SessionHelper.ObtenerUsuarioActual();

                DateTime fechaNacimiento;
                if (!DateTime.TryParse(txtFechaNacimiento.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaNacimiento))
                {
                    MostrarError("La fecha de nacimiento no tiene un formato válido.");
                    return;
                }

                var solicitud = new NuevaEvaluacionRequest
                {
                    ProductoId = int.Parse(ddlProducto.SelectedValue),
                    UsuarioId = usuario.UsuarioId,
                    PrimerNombre = txtPrimerNombre.Text.Trim(),
                    SegundoNombre = NullSiVacio(txtSegundoNombre.Text),
                    TercerNombre = NullSiVacio(txtTercerNombre.Text),
                    PrimerApellido = txtPrimerApellido.Text.Trim(),
                    SegundoApellido = NullSiVacio(txtSegundoApellido.Text),
                    Nit = txtNit.Text.Trim(),
                    Dpi = DpiValidator.Normalizar(txtDpi.Text),
                    FechaNacimiento = fechaNacimiento,
                    IngresosMensuales = ParseDecimalOpcional(txtIngresos.Text),
                    TipoEmpleo = NullSiVacio(txtTipoEmpleo.Text),
                    AntiguedadLaboralMeses = ParseIntOpcional(txtAntiguedad.Text)
                };

                Models.Evaluacion resultado = _evaluationService.EvaluarYGuardar(solicitud);

                Response.Redirect("~/Evaluacion/Resultado.aspx?id=" + resultado.EvaluacionId, false);
            }
            catch (ArgumentException validationEx)
            {
                MostrarError(validationEx.Message);
            }
            catch (InvalidOperationException opEx)
            {
                MostrarError(opEx.Message);
            }
        }

        /// <summary>Validación del DPI en el servidor (la del navegador es solo comodidad): ver BLL.DpiValidator.</summary>
        protected void cvDpi_ServerValidate(object source, System.Web.UI.WebControls.ServerValidateEventArgs args)
        {
            DpiValidationResult resultado = DpiValidator.Validar(args.Value);
            args.IsValid = resultado.EsValido;
            if (!resultado.EsValido)
            {
                cvDpi.ErrorMessage = resultado.Mensaje;
            }
        }

        private void MostrarError(string mensaje)
        {
            litMensajeError.Text = Server.HtmlEncode(mensaje);
            pnlMensajeError.Visible = true;
        }

        private static string NullSiVacio(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        private static decimal? ParseDecimalOpcional(string valor)
        {
            decimal resultado;
            return decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out resultado) ? resultado : (decimal?)null;
        }

        private static int? ParseIntOpcional(string valor)
        {
            int resultado;
            return int.TryParse(valor, out resultado) ? resultado : (int?)null;
        }
    }
}
