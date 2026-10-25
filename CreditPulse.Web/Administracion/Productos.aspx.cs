using System;
using System.Web.UI.WebControls;
using CreditPulse.Web.BLL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Administracion
{
    public partial class Productos : BasePage
    {
        private readonly AdminCatalogoService _adminService = new AdminCatalogoService();

        protected override Permiso? PermisoRequerido
        {
            get { return Permiso.AdministrarCatalogo; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarGrid();
            }
        }

        private void CargarGrid()
        {
            gvProductos.DataSource = _adminService.ListarProductos();
            gvProductos.DataBind();
        }

        protected void gvProductos_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "ToggleEstado") return;

            int productoId = Convert.ToInt32(e.CommandArgument);
            Producto producto = _adminService.ObtenerProducto(productoId);
            if (producto == null)
            {
                MostrarError("El producto ya no existe.");
                CargarGrid();
                return;
            }

            ResultadoGuardado resultado = _adminService.CambiarEstadoProducto(productoId, !producto.Activo);
            if (!resultado.Exitoso)
            {
                MostrarError(resultado.MensajeError);
            }

            CargarGrid();
        }

        private void MostrarError(string mensaje)
        {
            litMensajeError.Text = Server.HtmlEncode(mensaje);
            pnlMensajeError.Visible = true;
        }
    }
}
