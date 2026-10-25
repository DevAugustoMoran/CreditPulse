using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using CreditPulse.SibSimulador.Models;

namespace CreditPulse.SibSimulador.Controllers
{
    /// <summary>
    /// Simula el endpoint de consulta de historial crediticio de la SIB.
    /// GET api/perfil?dpi=...&amp;nit=...&amp;nombre=...
    ///
    /// Devuelve XML (no JSON) a propósito: es el mismo esquema que ya
    /// generaba CreditPulse.Web.BLL.SibMockService y que
    /// CreditPulse.Web.BLL.SibXmlParser ya sabe leer, y además así se
    /// guarda igual que antes en la columna XML Evaluaciones.SibRawXml.
    /// </summary>
    [RoutePrefix("api/perfil")]
    public class PerfilController : ApiController
    {
        [HttpGet]
        [Route("")]
        public HttpResponseMessage Consultar(string dpi, string nit, string nombre = null)
        {
            if (string.IsNullOrWhiteSpace(dpi))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "El parámetro 'dpi' es obligatorio.");
            }

            var perfil = SibPerfilGenerator.Generar(dpi, nit, nombre);
            string xml = SibPerfilGenerator.SerializarXml(perfil);

            var respuesta = Request.CreateResponse(HttpStatusCode.OK);
            respuesta.Content = new StringContent(xml, Encoding.UTF8, "application/xml");
            return respuesta;
        }
    }
}
