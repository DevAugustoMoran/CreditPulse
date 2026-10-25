using System;
using System.Web.Security;
using CreditPulse.Web.Models;
using CreditPulse.Web.Security;

namespace CreditPulse.Web
{
    public partial class SiteMaster : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var usuario = SessionHelper.ObtenerUsuarioActual();
            if (usuario != null)
            {
                litNombreUsuario.Text = Server.HtmlEncode(usuario.NombreCompleto);
                litRolUsuario.Text = Server.HtmlEncode(usuario.RolNombre);
                AplicarVisibilidadPorPermisos(usuario);
            }

            ResaltarEnlaceActivo();
        }

        /// <summary>
        /// Oculta del menú lateral los enlaces a pantallas que el usuario
        /// actual no puede abrir (ver BasePage.PermisoRequerido en cada
        /// página). Es solo una ayuda de navegación: la restricción real
        /// ocurre en BasePage, así que aunque alguien escriba la URL a mano
        /// sigue protegido.
        /// </summary>
        private void AplicarVisibilidadPorPermisos(Usuario usuario)
        {
            navAdminProductos.Visible = usuario.PuedeAdministrarCatalogo;
            navAdminUsuarios.Visible = usuario.PuedeAdministrarUsuarios;
            navGrupoAdmin.Visible = usuario.PuedeAdministrarCatalogo || usuario.PuedeAdministrarUsuarios;
            navNueva.Visible = usuario.PuedeEvaluar;
            navBatch.Visible = usuario.PuedeEvaluar;
            navGrupoEvaluacion.Visible = usuario.PuedeEvaluar;
        }

        private void ResaltarEnlaceActivo()
        {
            string pagina = Request.AppRelativeCurrentExecutionFilePath.ToLowerInvariant();

            if (pagina.Contains("reportes/reporteevaluaciones"))
            {
                navReporteEvaluaciones.Attributes["class"] += " cp-nav-link-active";
            }
            else if (pagina.Contains("reportes/reportebatch"))
            {
                navReporteBatch.Attributes["class"] += " cp-nav-link-active";
            }
            else if (pagina.Contains("evaluacion/nueva"))
            {
                navNueva.Attributes["class"] += " cp-nav-link-active";
            }
            else if (pagina.Contains("evaluacion/batch") || pagina.Contains("evaluacion/loteresultado"))
            {
                navBatch.Attributes["class"] += " cp-nav-link-active";
            }
            else if (pagina.Contains("administracion/usuarios") || pagina.Contains("administracion/usuarioform") ||
                     pagina.Contains("administracion/roles") || pagina.Contains("administracion/rolform"))
            {
                navAdminUsuarios.Attributes["class"] += " cp-nav-link-active";
            }
            else if (pagina.Contains("administracion/"))
            {
                navAdminProductos.Attributes["class"] += " cp-nav-link-active";
            }
            else if (pagina.Contains("default.aspx") || pagina == "~/")
            {
                navDashboard.Attributes["class"] += " cp-nav-link-active";
            }
        }

        protected void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            SessionHelper.CerrarSesion();
            FormsAuthentication.SignOut();
            Response.Redirect("~/Login.aspx");
        }
    }
}
