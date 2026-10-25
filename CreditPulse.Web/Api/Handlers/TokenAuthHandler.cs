using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CreditPulse.Web.Api.Models;
using CreditPulse.Web.BLL;
using CreditPulse.Web.Models;
using Newtonsoft.Json;

namespace CreditPulse.Web.Api.Handlers
{
    /// <summary>
    /// Autenticación por token (JWT + refresh token) para las APIs, SIN
    /// modificar las APIs existentes: es un "message handler" que se ejecuta
    /// antes que sus controladores y filtros.
    ///
    /// 1) Atiende los endpoints de autenticación (no pasan por la API key,
    ///    se protegen con las credenciales del usuario):
    ///      POST /api/v1/auth/token    { "email", "password" }  → access + refresh token
    ///      POST /api/v1/auth/refresh  { "refreshToken" }       → nuevo par (rotación)
    ///      POST /api/v1/auth/revoke   { "refreshToken" }       → cierra la sesión de API
    ///
    /// 2) Acepta "Authorization: Bearer {accessToken}" en las APIs existentes
    ///    COMO ALTERNATIVA a "X-Api-Key". Si la petición trae X-Api-Key, este
    ///    handler no hace nada y todo funciona exactamente igual que antes
    ///    (el servicio externo que ya consume las APIs no se entera). Si trae
    ///    un token válido, se comprueba el permiso del rol (crear una
    ///    evaluación exige "Evaluar") y se continúa con la credencial
    ///    interna de integración, que es lo que el filtro de API key valida.
    /// </summary>
    public class TokenAuthHandler : DelegatingHandler
    {
        private const string HeaderApiKey = "X-Api-Key";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string path = request.RequestUri.AbsolutePath.TrimEnd('/');

            string operacion = OperacionAuth(path);
            if (operacion != null)
            {
                return await ManejarAuth(request, operacion);
            }

            AuthenticationHeaderValue auth = request.Headers.Authorization;
            bool usaBearer = !request.Headers.Contains(HeaderApiKey)
                             && auth != null
                             && string.Equals(auth.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase);

            if (usaBearer)
            {
                var servicio = new TokenService();
                Usuario usuario = servicio.ValidarAccessToken(auth.Parameter);

                if (usuario == null)
                {
                    HttpResponseMessage rechazo = Respuesta(request, HttpStatusCode.Unauthorized, new { Message = "Token de acceso inválido o vencido. Renuévalo con /api/v1/auth/refresh." });
                    rechazo.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Bearer", "error=\"invalid_token\""));
                    return rechazo;
                }

                if (request.Method == HttpMethod.Post && path.IndexOf("/api/v1/evaluaciones", StringComparison.OrdinalIgnoreCase) >= 0 && !usuario.PuedeEvaluar)
                {
                    return Respuesta(request, HttpStatusCode.Forbidden, new { Message = "Tu rol no tiene el permiso \"Evaluar\"." });
                }

                string llaveInterna = ConfigurationManager.AppSettings["ApiKeyIntegraciones"];
                if (!string.IsNullOrWhiteSpace(llaveInterna))
                {
                    request.Headers.Add(HeaderApiKey, llaveInterna);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private static string OperacionAuth(string path)
        {
            foreach (string operacion in new[] { "token", "refresh", "revoke" })
            {
                if (path.EndsWith("/api/v1/auth/" + operacion, StringComparison.OrdinalIgnoreCase)) return operacion;
            }
            return null;
        }

        private async Task<HttpResponseMessage> ManejarAuth(HttpRequestMessage request, string operacion)
        {
            if (request.Method != HttpMethod.Post)
            {
                HttpResponseMessage noPermitido = Respuesta(request, HttpStatusCode.MethodNotAllowed, new { Message = "Usa POST." });
                noPermitido.Content.Headers.Allow.Add("POST");
                return noPermitido;
            }

            var servicio = new TokenService();
            if (!servicio.EstaConfigurado)
            {
                return Respuesta(request, HttpStatusCode.InternalServerError, new { Message = "La API no tiene configurado \"JwtSecret\" (mínimo 32 caracteres) en Web.config." });
            }

            string cuerpo = request.Content != null ? await request.Content.ReadAsStringAsync() : string.Empty;
            string ip = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;

            try
            {
                if (operacion == "token")
                {
                    var datos = JsonConvert.DeserializeObject<TokenApiRequest>(cuerpo) ?? new TokenApiRequest();
                    return ComoRespuesta(request, servicio.Emitir(datos.Email, datos.Password, ip));
                }

                var refresco = JsonConvert.DeserializeObject<RefreshApiRequest>(cuerpo) ?? new RefreshApiRequest();

                if (operacion == "refresh")
                {
                    return ComoRespuesta(request, servicio.Renovar(refresco.RefreshToken, ip));
                }

                servicio.Revocar(refresco.RefreshToken);
                return Respuesta(request, HttpStatusCode.OK, new { Message = "Sesión de API cerrada." });
            }
            catch (JsonException)
            {
                return Respuesta(request, HttpStatusCode.BadRequest, new { Message = "El cuerpo de la solicitud no es un JSON válido." });
            }
        }

        private static HttpResponseMessage ComoRespuesta(HttpRequestMessage request, TokenResult resultado)
        {
            if (!resultado.Exito)
            {
                return Respuesta(request, (HttpStatusCode)resultado.CodigoHttp, new { Message = resultado.Mensaje });
            }

            var cuerpo = new TokenApiResponse
            {
                TokenType = "Bearer",
                AccessToken = resultado.AccessToken,
                ExpiresIn = (int)Math.Max(0, (resultado.AccessExpiraUtc - DateTime.UtcNow).TotalSeconds),
                ExpiresAtUtc = resultado.AccessExpiraUtc,
                RefreshToken = resultado.RefreshToken,
                RefreshExpiresAtUtc = resultado.RefreshExpiraUtc,
                Usuario = new TokenUsuarioApiDto
                {
                    Email = resultado.Usuario.Email,
                    Nombre = resultado.Usuario.NombreCompleto,
                    Rol = resultado.Usuario.RolNombre,
                    PuedeEvaluar = resultado.Usuario.PuedeEvaluar
                }
            };

            return Respuesta(request, HttpStatusCode.OK, cuerpo);
        }

        /// <summary>Respuesta JSON (application/json, nunca text/html: ver la nota sobre el hosting con publicidad en WebApiConfig).</summary>
        private static HttpResponseMessage Respuesta(HttpRequestMessage request, HttpStatusCode codigo, object cuerpo)
        {
            var respuesta = new HttpResponseMessage(codigo)
            {
                RequestMessage = request,
                Content = new StringContent(JsonConvert.SerializeObject(cuerpo), Encoding.UTF8, "application/json")
            };
            respuesta.Headers.CacheControl = new CacheControlHeaderValue { NoStore = true };
            return respuesta;
        }
    }
}
