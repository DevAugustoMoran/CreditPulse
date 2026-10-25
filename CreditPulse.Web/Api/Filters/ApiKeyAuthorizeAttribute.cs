using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace CreditPulse.Web.Api.Filters
{
    /// <summary>
    /// Autenticación simple por "API key" compartida para las APIs de
    /// integración (Api/Controllers/*.cs): el llamador debe enviar el
    /// header "X-Api-Key" con el valor configurado en el appSetting
    /// "ApiKeyIntegraciones" de Web.config. Sin ese header (o con un valor
    /// incorrecto), se responde 401 Unauthorized sin ejecutar la acción.
    ///
    /// Es deliberadamente simple -una sola llave compartida entre todos los
    /// sistemas consumidores, en vez de una llave por sistema o un esquema
    /// tipo OAuth2 client-credentials con expiración/rotación- para no
    /// sobrecargar el alcance del curso. Se documenta en el README como una
    /// simplificación consciente: un ecosistema bancario real necesitaría
    /// llaves por sistema consumidor (para poder revocar una sin afectar a
    /// los demás) y, probablemente, mTLS u OAuth2 en vez de una llave fija
    /// en un header.
    /// </summary>
    public class ApiKeyAuthorizeAttribute : AuthorizationFilterAttribute
    {
        private const string HeaderName = "X-Api-Key";

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            string llaveConfigurada = ConfigurationManager.AppSettings["ApiKeyIntegraciones"];

            if (string.IsNullOrWhiteSpace(llaveConfigurada))
            {
                // Falta configurar la llave: es un error de despliegue, no
                // del llamador. Se responde 500 en vez de dejar la API
                // abierta por accidente si alguien borra el appSetting.
                actionContext.Response = actionContext.Request.CreateErrorResponse(
                    HttpStatusCode.InternalServerError,
                    "La API no tiene configurada \"ApiKeyIntegraciones\" en Web.config.");
                return;
            }

            IEnumerable<string> valores;
            string llaveRecibida = actionContext.Request.Headers.TryGetValues(HeaderName, out valores)
                ? System.Linq.Enumerable.FirstOrDefault(valores)
                : null;

            if (string.IsNullOrWhiteSpace(llaveRecibida) ||
                !FixedTimeEquals(llaveRecibida, llaveConfigurada))
            {
                actionContext.Response = actionContext.Request.CreateErrorResponse(
                    HttpStatusCode.Unauthorized,
                    "Falta o es inválido el header \"X-Api-Key\".");
            }
        }

        /// <summary>Comparación en tiempo constante, igual que Security.PasswordHasher, para no filtrar la llave por temporización.</summary>
        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;

            int diferencia = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diferencia |= a[i] ^ b[i];
            }
            return diferencia == 0;
        }
    }
}
