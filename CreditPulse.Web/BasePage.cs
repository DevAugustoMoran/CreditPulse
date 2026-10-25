using System;
using System.Web.Security;
using CreditPulse.Web.Security;

namespace CreditPulse.Web
{
    /// <summary>
    /// Página base para toda pantalla que requiere sesión iniciada. Si el
    /// ticket de Forms Authentication sigue vigente pero la Session en
    /// memoria ya expiró (por ejemplo, reciclado del AppPool), se fuerza un
    /// nuevo login en vez de continuar con un Usuario nulo.
    /// </summary>
    public abstract class BasePage : System.Web.UI.Page
    {
        protected Models.Usuario UsuarioActual { get; private set; }

        /// <summary>
        /// Permiso (ver Models.Permiso) que un usuario necesita para ver
        /// esta pantalla. Una página restringida la sobreescribe; por
        /// defecto es null, es decir, cualquier usuario autenticado puede
        /// verla. La restricción es "pantalla completa": si el rol del
        /// usuario no tiene el permiso, se redirige a AccesoDenegado.aspx
        /// antes de ejecutar el Page_Load de la página concreta.
        ///
        /// Antes de Database/08_UsuariosRoles.sql esta verificación
        /// comparaba Usuario.RolNombre contra una lista fija de nombres de
        /// rol (RolesPermitidos). Se cambió a permisos para que un rol
        /// nuevo, creado desde Administracion/RolForm.aspx, pueda recibir
        /// acceso real a una pantalla sin tener que modificar código.
        /// </summary>
        protected virtual Models.Permiso? PermisoRequerido
        {
            get { return null; }
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            UsuarioActual = SessionHelper.ObtenerUsuarioActual();

            if (UsuarioActual == null)
            {
                FormsAuthentication.SignOut();
                Response.Redirect("~/Login.aspx?expirado=1", true);
                return;
            }

            if (PermisoRequerido.HasValue && !UsuarioActual.TienePermiso(PermisoRequerido.Value))
            {
                Response.Redirect("~/AccesoDenegado.aspx", true);
            }
        }
    }
}
