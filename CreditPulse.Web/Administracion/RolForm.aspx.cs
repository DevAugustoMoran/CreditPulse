using System;
using CreditPulse.Web.BLL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Administracion
{
    public partial class RolForm : BasePage
    {
        private readonly AdminUsuariosService _adminService = new AdminUsuariosService();
        private int _rolId;

        protected override Permiso? PermisoRequerido
        {
            get { return Permiso.AdministrarUsuarios; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            int.TryParse(Request.QueryString["id"], out _rolId);

            if (!IsPostBack)
            {
                if (_rolId > 0)
                {
                    CargarRol();
                }
                else
                {
                    litTitulo.Text = "Nuevo Rol";
                    chkActivo.Checked = true;
                }
            }
        }

        private void CargarRol()
        {
            Rol rol = _adminService.ObtenerRol(_rolId);
            if (rol == null)
            {
                Response.Redirect("~/Administracion/Roles.aspx", false);
                return;
            }

            litTitulo.Text = "Editar Rol";
            txtNombre.Text = rol.Nombre;
            txtDescripcion.Text = rol.Descripcion;
            chkPuedeEvaluar.Checked = rol.PuedeEvaluar;
            chkPuedeAdministrarCatalogo.Checked = rol.PuedeAdministrarCatalogo;
            chkPuedeAdministrarUsuarios.Checked = rol.PuedeAdministrarUsuarios;
            chkActivo.Checked = rol.Activo;
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var rol = new Rol
            {
                RolId = _rolId,
                Nombre = txtNombre.Text.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(txtDescripcion.Text) ? null : txtDescripcion.Text.Trim(),
                PuedeEvaluar = chkPuedeEvaluar.Checked,
                PuedeAdministrarCatalogo = chkPuedeAdministrarCatalogo.Checked,
                PuedeAdministrarUsuarios = chkPuedeAdministrarUsuarios.Checked,
                Activo = chkActivo.Checked
            };

            ResultadoGuardado resultado = _adminService.GuardarRol(rol);
            if (!resultado.Exitoso)
            {
                MostrarError(resultado.MensajeError);
                return;
            }

            Response.Redirect("~/Administracion/Roles.aspx", false);
        }

        private void MostrarError(string mensaje)
        {
            litMensajeError.Text = Server.HtmlEncode(mensaje);
            pnlMensajeError.Visible = true;
        }
    }
}
