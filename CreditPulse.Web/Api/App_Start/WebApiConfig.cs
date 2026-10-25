using System.Web.Http;
using CreditPulse.Web.Api.Filters;
using CreditPulse.Web.Api.Handlers;

namespace CreditPulse.Web.Api
{
    /// <summary>
    /// Registra Web API 2 DENTRO del mismo proyecto CreditPulse.Web (que ya
    /// es un sitio WebForms), bajo el prefijo /api/v1/... A diferencia de
    /// CreditPulse.SibSimulador (que es un servicio externo aparte,
    /// simulando a la SIB), estas APIs son del sistema CreditPulse mismo:
    /// otros sistemas del ecosistema bancario las llaman para pedirle a
    /// CreditPulse que evalúe a un cliente o les comparta datos que
    /// CreditPulse ya recolectó, así que viven en el mismo proceso/sitio,
    /// no en un proyecto separado.
    ///
    /// Se registra desde Global.asax.cs -> Application_Start, igual que
    /// cualquier otro punto de arranque de la aplicación.
    /// </summary>
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Rutas por atributo ([RoutePrefix]/[Route] en cada controller,
            // ver Api/Controllers/*.cs), igual que CreditPulse.SibSimulador.
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/v1/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Todas las APIs de integración requieren API key (ver
            // ApiKeyAuthorizeAttribute): se registra como filtro global en
            // vez de repetir el atributo en cada controller/acción, para
            // que sea imposible agregar un endpoint nuevo bajo /api/v1/ y
            // olvidar protegerlo.
            config.Filters.Add(new ApiKeyAuthorizeAttribute());

            // Autenticación por token (JWT + refresh token): endpoints
            // /api/v1/auth/* y "Authorization: Bearer" como alternativa a la
            // API key. Es un handler aparte (Api/Handlers/TokenAuthHandler.cs):
            // los controladores y el filtro de API key de arriba no se tocan,
            // y toda petición con X-Api-Key sigue exactamente igual que antes.
            config.MessageHandlers.Add(new TokenAuthHandler());

            // Web API 2 ya incluye JsonMediaTypeFormatter (basado en
            // Newtonsoft.Json, que este proyecto ya usa) y
            // XmlMediaTypeFormatter por defecto: content negotiation por el
            // header "Accept" (application/json o application/xml) alcanza
            // para que cada llamador reciba el formato que prefiera, sin
            // configuración adicional.
            //
            // IMPORTANTE: aquí ANTES se insertaba "text/html" como
            // SupportedMediaType del JsonFormatter, para que el JSON se
            // viera bien si alguien abría la URL directo en el navegador
            // (o cuando el llamador manda "Accept: */*", como hace Postman
            // por defecto). Se quitó: en hosts gratuitos con publicidad
            // forzada (ej. Somee), el módulo que inyecta el anuncio actúa
            // sobre CUALQUIER respuesta cuyo Content-Type resuelva a
            // "text/html" -incluida esta, aunque el cuerpo sea JSON-, y el
            // anuncio termina pegado al final del JSON, rompiéndolo para
            // cualquier parser estricto. Sin esta línea, "Accept: */*" cae
            // en application/json (el primer formatter registrado), que
            // no es tocado por ese inyector.
        }
    }
}
