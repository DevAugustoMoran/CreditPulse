using System;

namespace CreditPulse.Web
{
    /// <summary>
    /// Destino de redirección de BasePage cuando el usuario no tiene el
    /// permiso requerido (BasePage.PermisoRequerido) de la página
    /// solicitada. No define PermisoRequerido propio (queda null =
    /// cualquier usuario autenticado), para no generar un bucle de
    /// redirección.
    /// </summary>
    public partial class AccesoDenegado : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            litRolActual.Text = Server.HtmlEncode(UsuarioActual.RolNombre);
        }
    }
}
