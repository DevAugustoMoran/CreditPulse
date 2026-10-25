using System;
using System.Web.UI.WebControls;
using CreditPulse.Web.BLL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Administracion
{
    public partial class Roles : BasePage
    {
        private readonly AdminUsuariosService _adminService = new AdminUsuariosService();

        protected override Permiso? PermisoRequerido
        {
            get { return Permiso.AdministrarUsuarios; }
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
            gvRoles.DataSource = _adminService.ListarRoles();
            gvRoles.DataBind();
        }

        protected void gvRoles_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "ToggleEstado") return;

            int rolId = Convert.ToInt32(e.CommandArgument);
            ResultadoGuardado resultado = _adminService.CambiarEstadoRol(rolId, !EstadoActualDe(rolId));
            if (!resultado.Exitoso)
            {
                MostrarError(resultado.MensajeError);
            }

            CargarGrid();
        }

        private bool EstadoActualDe(int rolId)
        {
            Rol rol = _adminService.ObtenerRol(rolId);
            return rol != null && rol.Activo;
        }

        private void MostrarError(string mensaje)
        {
            litMensajeError.Text = Server.HtmlEncode(mensaje);
            pnlMensajeError.Visible = true;
        }
    }
}
