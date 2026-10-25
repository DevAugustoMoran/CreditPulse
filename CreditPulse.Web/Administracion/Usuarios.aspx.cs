using System;
using System.Web.UI.WebControls;
using CreditPulse.Web.BLL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Administracion
{
    public partial class Usuarios : BasePage
    {
        private const int MaxIntentosFallidos = 5; // igual que AuthService.MaxIntentosFallidos

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
            gvUsuarios.DataSource = _adminService.ListarUsuarios();
            gvUsuarios.DataBind();
        }

        protected void gvUsuarios_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var usuario = (Usuario)e.Row.DataItem;
            var badge = (System.Web.UI.HtmlControls.HtmlGenericControl)e.Row.FindControl("badgeEstado");
            if (badge == null) return;

            var lnkDesbloquear = (LinkButton)e.Row.FindControl("lnkDesbloquear");
            if (lnkDesbloquear != null)
            {
                lnkDesbloquear.Visible = usuario.Activo && usuario.IntentosFallidos >= MaxIntentosFallidos;
            }

            if (!usuario.Activo)
            {
                badge.InnerText = "Inactivo";
                badge.Attributes["class"] = "badge badge-neutro";
            }
            else if (usuario.IntentosFallidos >= MaxIntentosFallidos)
            {
                badge.InnerText = "Bloqueado";
                badge.Attributes["class"] = "badge badge-rojo";
            }
            else
            {
                badge.InnerText = "Activo";
                badge.Attributes["class"] = "badge badge-verde";
            }
        }

        protected void gvUsuarios_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "Desbloquear")
            {
                ResultadoGuardado desbloqueo = _adminService.DesbloquearUsuario(Convert.ToInt32(e.CommandArgument));
                if (desbloqueo.Exitoso)
                {
                    litMensajeOk.Text = Server.HtmlEncode("Cuenta desbloqueada. El usuario ya puede iniciar sesión con su contraseña actual.");
                    pnlMensajeOk.Visible = true;
                }
                else
                {
                    MostrarError(desbloqueo.MensajeError);
                }

                CargarGrid();
                return;
            }

            if (e.CommandName != "ToggleEstado") return;

            int usuarioId = Convert.ToInt32(e.CommandArgument);
            Usuario usuario = _adminService.ObtenerUsuario(usuarioId);
            if (usuario == null)
            {
                MostrarError("El usuario ya no existe.");
                CargarGrid();
                return;
            }

            ResultadoGuardado resultado = _adminService.CambiarEstadoUsuario(usuarioId, !usuario.Activo, UsuarioActual.UsuarioId);
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
