using System.Web.Http;

namespace CreditPulse.SibSimulador
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Habilita rutas por atributo ([RoutePrefix]/[Route] en los controllers),
            // que es lo que usa Controllers\PerfilController.cs.
            config.MapHttpAttributeRoutes();

            // Ruta por defecto de respaldo (no se usa en la práctica porque el
            // controller ya define su propia ruta con atributos, pero se deja
            // como es convención en todo proyecto Web API 2).
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
