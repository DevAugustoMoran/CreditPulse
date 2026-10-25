using System;
using System.Web;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Security
{
    /// <summary>
    /// Envoltorio simple sobre Session para guardar el usuario autenticado.
    /// Forms Authentication controla el acceso a las páginas (Web.config);
    /// esto solo evita tener que releer al usuario de la base de datos en
    /// cada postback.
    /// </summary>
    public static class SessionHelper
    {
        private const string ClaveUsuario = "CreditPulse_UsuarioActual";

        public static void EstablecerUsuarioActual(Usuario usuario)
        {
            HttpContext.Current.Session[ClaveUsuario] = usuario;
        }

        public static Usuario ObtenerUsuarioActual()
        {
            return HttpContext.Current.Session[ClaveUsuario] as Usuario;
        }

        public static void CerrarSesion()
        {
            HttpContext.Current.Session.Clear();
            HttpContext.Current.Session.Abandon();
        }
    }
}
