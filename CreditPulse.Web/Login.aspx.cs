using System;
using System.Web.Security;
using CreditPulse.Web.BLL;
using CreditPulse.Web.Security;

namespace CreditPulse.Web
{
    public partial class Login : System.Web.UI.Page
    {
        private readonly AuthService _authService = new AuthService();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && Request.QueryString["expirado"] == "1")
            {
                MostrarMensaje("Tu sesión expiró por inactividad. Ingresa nuevamente.");
            }
        }

        protected void btnIngresar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            LoginResult resultado = _authService.IniciarSesion(txtEmail.Text, txtPassword.Text);

            if (resultado.Status != LoginStatus.Exitoso)
            {
                MostrarMensaje(resultado.MensajeError);
                return;
            }

            SessionHelper.EstablecerUsuarioActual(resultado.Usuario);

            FormsAuthentication.SetAuthCookie(resultado.Usuario.Email, false);
            Response.Redirect("~/Default.aspx", false);
        }

        private void MostrarMensaje(string mensaje)
        {
            litMensaje.Text = Server.HtmlEncode(mensaje);
            pnlMensaje.Visible = true;
        }
    }
}
