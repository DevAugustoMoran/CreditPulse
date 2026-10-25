using System;
using CreditPulse.Web.BLL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Administracion
{
    public partial class UsuarioForm : BasePage
    {
        private readonly AdminUsuariosService _adminService = new AdminUsuariosService();
        private int _usuarioId;

        protected override Permiso? PermisoRequerido
        {
            get { return Permiso.AdministrarUsuarios; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            int.TryParse(Request.QueryString["id"], out _usuarioId);

            if (!IsPostBack)
            {
                CargarRoles();

                if (_usuarioId > 0)
                {
                    CargarUsuario();
                }
                else
                {
                    litTitulo.Text = "Nuevo Usuario";
                    chkActivo.Checked = true;
                }
            }
        }

        private void CargarRoles()
        {
            ddlRol.DataSource = _adminService.ListarRolesActivos();
            ddlRol.DataTextField = "Nombre";
            ddlRol.DataValueField = "RolId";
            ddlRol.DataBind();
        }

        private void CargarUsuario()
        {
            Usuario usuario = _adminService.ObtenerUsuario(_usuarioId);
            if (usuario == null)
            {
                Response.Redirect("~/Administracion/Usuarios.aspx", false);
                return;
            }

            litTitulo.Text = "Editar Usuario";
            txtNombreCompleto.Text = usuario.NombreCompleto;
            txtEmail.Text = usuario.Email;
            chkActivo.Checked = usuario.Activo;

            ListItem_SelectIfExists(ddlRol, usuario.RolId.ToString());

            litLabelPassword.Text = "Nueva contraseña";
            pContrasenaHint.InnerText = "Deja ambos campos en blanco para conservar la contraseña actual. Si la cambias, debe tener 10+ caracteres, con letras, números y símbolos.";
        }

        private static void ListItem_SelectIfExists(System.Web.UI.WebControls.DropDownList ddl, string value)
        {
            var item = ddl.Items.FindByValue(value);
            if (item != null) item.Selected = true;
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var usuario = new Usuario
            {
                UsuarioId = _usuarioId,
                Email = txtEmail.Text.Trim(),
                NombreCompleto = txtNombreCompleto.Text.Trim(),
                RolId = Convert.ToInt32(ddlRol.SelectedValue),
                Activo = chkActivo.Checked
            };

            string password = txtPassword.Text; // vacío = no cambiar (ver AdminUsuariosService.GuardarUsuario)

            ResultadoGuardado resultado = _adminService.GuardarUsuario(usuario, password, UsuarioActual.UsuarioId);
            if (!resultado.Exitoso)
            {
                MostrarError(resultado.MensajeError);
                return;
            }

            Response.Redirect("~/Administracion/Usuarios.aspx", false);
        }

        private void MostrarError(string mensaje)
        {
            litMensajeError.Text = Server.HtmlEncode(mensaje);
            pnlMensajeError.Visible = true;
        }
    }
}
