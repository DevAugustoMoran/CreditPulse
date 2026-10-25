using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CreditPulse.Web.Api.Models;
using CreditPulse.Web.BLL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Api.Controllers
{
    /// <summary>
    /// API de datos SIB: otros sistemas del ecosistema bancario que
    /// necesitan los datos más importantes que devuelve la SIB para un
    /// cliente (score, endeudamiento, historial, listas de riesgo), sin
    /// pasar por una evaluación completa ni por la pantalla de reporte de
    /// CreditPulse. Usa el mismo ISibService que ya usa el resto de la
    /// aplicación (BLL.EvaluationService.CrearSibService), así que respeta
    /// el appSetting "UsarSibMock": simulador en memoria o llamada HTTP a
    /// CreditPulse.SibSimulador, según cómo esté configurado. Requiere el
    /// header "X-Api-Key" (ver ApiKeyAuthorizeAttribute).
    /// </summary>
    [RoutePrefix("api/v1/sib")]
    public class SibApiController : ApiController
    {
        /// <summary>GET /api/v1/sib/perfil?dpi=...&amp;nit=...&amp;nombre=...</summary>
        [HttpGet]
        [Route("perfil")]
        public HttpResponseMessage Get(string dpi, string nit = null, string nombre = null)
        {
            if (string.IsNullOrWhiteSpace(dpi))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "El parámetro \"dpi\" es obligatorio.");
            }

            try
            {
                ISibService sibService = EvaluationService.CrearSibService();
                SibPerfil perfil = sibService.ConsultarPerfil(dpi.Trim(), nit, nombre);

                var respuesta = new SibApiResponse
                {
                    Dpi = perfil.Dpi,
                    Nit = perfil.Nit,
                    NombreCompleto = perfil.NombreCompleto,
                    ScoreBuro = perfil.ScoreBuro,
                    NivelEndeudamiento = perfil.NivelEndeudamiento,
                    IngresosReportados = perfil.IngresosReportados,
                    CreditosActivos = perfil.CreditosActivos,
                    CreditosCancelados = perfil.CreditosCancelados,
                    MorasUltimos12Meses = perfil.MorasUltimos12Meses,
                    DiasMoraMaximo = perfil.DiasMoraMaximo,
                    EnListaNegraLocal = perfil.EnListaNegraLocal,
                    EnListaNegraInternacional = perfil.EnListaNegraInternacional,
                    EnListaOfac = perfil.EnListaOfac,
                    EnListaNegra = perfil.EnListaNegra,
                    ClienteExistente = perfil.ClienteExistente,
                    CreditosPreviosEnBanco = perfil.CreditosPreviosEnBanco,
                    FechaConsulta = perfil.FechaConsulta,
                    FuenteConsultada = perfil.FuenteConsultada,
                    TiempoRespuestaMs = perfil.TiempoRespuestaMs
                };

                return Request.CreateResponse(HttpStatusCode.OK, respuesta);
            }
            catch (InvalidOperationException ex)
            {
                // SibApiClientService (UsarSibMock=false) lanza esto cuando el
                // servicio HTTP de la SIB no respondió: es un problema del
                // sistema upstream, no del llamador.
                return Request.CreateErrorResponse(HttpStatusCode.BadGateway, ex.Message);
            }
        }
    }
}
