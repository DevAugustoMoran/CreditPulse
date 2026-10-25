using System;
using System.Configuration;
using System.Net.Http;
using System.Web;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.BLL
{
    /// <summary>
    /// Implementación de <see cref="ISibService"/> que consulta el perfil
    /// crediticio llamando por HTTP a CreditPulse.SibSimulador -un proyecto
    /// Web API 2 aparte, que corre como su propio servicio- en vez de
    /// generarlo en memoria dentro del mismo proceso (eso es lo que hace
    /// <see cref="SibMockService"/>).
    ///
    /// Se activa/desactiva con el appSetting "UsarSibMock" en Web.config
    /// (ver <see cref="EvaluationService.CrearSibService"/>):
    ///   - UsarSibMock = true  -> se usa SibMockService (en el mismo proceso).
    ///   - UsarSibMock = false -> se usa esta clase, que llama a la URL de
    ///     "SibApiBaseUrl" (por ejemplo, https://localhost:44399/).
    ///
    /// El XML que responde CreditPulse.SibSimulador usa exactamente el mismo
    /// esquema que ya generaba SibMockService (ver
    /// CreditPulse.SibSimulador\Models\SibPerfilGenerator.cs), así que se
    /// puede reutilizar SibXmlParser.Parsear tal cual para convertir la
    /// respuesta en un SibPerfil, sin duplicar lógica de interpretación.
    /// </summary>
    public class SibApiClientService : ISibService
    {
        private readonly string _baseUrl;
        private readonly int _timeoutMs;

        public SibApiClientService()
        {
            _baseUrl = ConfigurationManager.AppSettings["SibApiBaseUrl"];
            int timeout;
            _timeoutMs = int.TryParse(ConfigurationManager.AppSettings["SibApiTimeoutMs"], out timeout) ? timeout : 5000;
        }

        public SibPerfil ConsultarPerfil(string dpi, string nit, string nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
            {
                throw new InvalidOperationException(
                    "El appSetting \"SibApiBaseUrl\" no está configurado. Apunta a la URL donde corre " +
                    "CreditPulse.SibSimulador (por ejemplo, https://localhost:44399/), o pon \"UsarSibMock\" en true " +
                    "en Web.config para usar el simulador interno mientras tanto.");
            }

            string url = string.Format(
                "{0}api/perfil?dpi={1}&nit={2}&nombre={3}",
                _baseUrl.TrimEnd('/') + "/",
                HttpUtility.UrlEncode(dpi ?? string.Empty),
                HttpUtility.UrlEncode(nit ?? string.Empty),
                HttpUtility.UrlEncode(nombreCompleto ?? string.Empty));

            string xml;
            using (var cliente = new HttpClient())
            {
                cliente.Timeout = TimeSpan.FromMilliseconds(_timeoutMs);
                string llave = ConfigurationManager.AppSettings["SibApiKey"];
                if (!string.IsNullOrWhiteSpace(llave))
                    cliente.DefaultRequestHeaders.Add("X-Api-Key", llave.Trim());
                try
                {
                    HttpResponseMessage respuesta = cliente.GetAsync(url).GetAwaiter().GetResult();
                    respuesta.EnsureSuccessStatusCode();
                    xml = respuesta.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex) when (!(ex is InvalidOperationException))
                {
                    // Envuelve errores de red/timeout/HTTP en un mensaje que la
                    // pantalla de evaluación pueda mostrar tal cual, sin que el
                    // usuario final tenga que interpretar una excepción de
                    // HttpClient o de sockets.
                    throw new InvalidOperationException(
                        "No se pudo consultar el servicio de la SIB (" + _baseUrl + "). " +
                        "Verifique que CreditPulse.SibSimulador esté corriendo, o cambie \"UsarSibMock\" a true " +
                        "en Web.config para usar el simulador interno mientras tanto. Detalle: " + ex.Message, ex);
                }
            }

            SibPerfil perfil = SibXmlParser.Parsear(xml);
            if (perfil == null)
            {
                throw new InvalidOperationException(
                    "El servicio de la SIB respondió, pero el XML recibido no tiene el formato esperado.");
            }

            return perfil;
        }
    }
}
