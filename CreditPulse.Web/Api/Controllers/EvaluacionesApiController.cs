using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CreditPulse.Web.Api.Models;
using CreditPulse.Web.BLL;
using CreditPulse.Web.DAL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Api.Controllers
{
    /// <summary>
    /// API de evaluación crediticia: otros sistemas del ecosistema bancario
    /// le piden a CreditPulse que evalúe a un cliente (POST) o que les
    /// devuelva el detalle de una evaluación ya hecha (GET), con el mismo
    /// resultado -color, políticas evaluadas, número de evaluación- que
    /// vería un analista en Evaluacion/Resultado.aspx. Todas las rutas
    /// requieren el header "X-Api-Key" (ver ApiKeyAuthorizeAttribute,
    /// registrado como filtro global en WebApiConfig).
    /// </summary>
    [RoutePrefix("api/v1/evaluaciones")]
    public class EvaluacionesApiController : ApiController
    {
        private readonly ProductoRepository _productoRepository = new ProductoRepository();
        private readonly EvaluacionRepository _evaluacionRepository = new EvaluacionRepository();
        private readonly EvaluationService _evaluationService = new EvaluationService();

        // Se resuelve una sola vez por ciclo de vida de la aplicación: el
        // usuario de servicio no cambia, y así se evita una consulta extra
        // en cada llamada (ver Database/11_ApiIntegraciones.sql).
        private static int? _usuarioIntegracionIdCache;

        /// <summary>
        /// POST /api/v1/evaluaciones: ejecuta una evaluación completa (SIB +
        /// motor de políticas) tal como lo haría Evaluacion/Nueva.aspx, la
        /// guarda, y devuelve el detalle. 201 Created con el resultado; 400
        /// si el producto no existe/está inactivo o faltan datos
        /// obligatorios del cliente; 502 si la SIB (como servicio HTTP
        /// separado, ver CreditPulse.SibSimulador) no respondió.
        /// </summary>
        [HttpPost]
        [Route("")]
        public HttpResponseMessage Post([FromBody] EvaluacionApiRequest request)
        {
            if (request == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "El cuerpo de la solicitud está vacío o no es JSON/XML válido.");
            }

            if (string.IsNullOrWhiteSpace(request.ProductoCodigo))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "\"ProductoCodigo\" es obligatorio.");
            }

            Producto producto = _productoRepository.ObtenerPorCodigo(request.ProductoCodigo.Trim());
            if (producto == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    string.Format("El producto '{0}' no existe o no está activo.", request.ProductoCodigo));
            }

            int usuarioIntegracionId;
            try
            {
                usuarioIntegracionId = ObtenerUsuarioIntegracionId();
            }
            catch (InvalidOperationException ex)
            {
                // Falta ejecutar Database/11_ApiIntegraciones.sql: es un problema
                // de despliegue, no algo que el llamador pueda corregir reintentando.
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

            try
            {
                var solicitud = new NuevaEvaluacionRequest
                {
                    ProductoId = producto.ProductoId,
                    UsuarioId = usuarioIntegracionId,
                    PrimerNombre = request.PrimerNombre,
                    SegundoNombre = request.SegundoNombre,
                    PrimerApellido = request.PrimerApellido,
                    SegundoApellido = request.SegundoApellido,
                    Nit = request.Nit,
                    Dpi = request.Dpi,
                    FechaNacimiento = request.FechaNacimiento,
                    IngresosMensuales = request.IngresosMensuales,
                    TipoEmpleo = request.TipoEmpleo,
                    AntiguedadLaboralMeses = request.AntiguedadLaboralMeses
                };

                Evaluacion evaluacion = _evaluationService.EvaluarYGuardar(solicitud);

                EvaluacionApiResponse respuesta = MapearRespuesta(evaluacion, producto.Codigo);
                return Request.CreateResponse(HttpStatusCode.Created, respuesta);
            }
            catch (ArgumentException ex)
            {
                // Datos del cliente inválidos (ver EvaluationService.Validar): responsabilidad del llamador.
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                // SibApiClientService envuelve los errores de red/timeout con una InnerException
                // (ver BLL/SibApiClientService.cs); eso distingue "la SIB no respondió" (502,
                // problema nuestro/upstream) de "producto sin políticas configuradas" (400,
                // problema de configuración que no depende del llamador tampoco, pero no es
                // un fallo de red) sin tener que cambiar las excepciones que ya lanzaba
                // EvaluationService.
                HttpStatusCode codigo = ex.InnerException != null ? HttpStatusCode.BadGateway : HttpStatusCode.BadRequest;
                return Request.CreateErrorResponse(codigo, ex.Message);
            }
        }

        /// <summary>GET /api/v1/evaluaciones/{id}: detalle de una evaluación ya existente (creada por API, por Nueva.aspx o por un lote Batch).</summary>
        [HttpGet]
        [Route("{id:int}")]
        public HttpResponseMessage Get(int id)
        {
            Evaluacion evaluacion = _evaluacionRepository.ObtenerPorId(id);
            if (evaluacion == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format("No existe la evaluación #{0}.", id));
            }

            Producto producto = _productoRepository.ObtenerPorId(evaluacion.ProductoId);
            EvaluacionApiResponse respuesta = MapearRespuesta(evaluacion, producto?.Codigo);
            return Request.CreateResponse(HttpStatusCode.OK, respuesta);
        }

        /// <summary>
        /// GET /api/v1/evaluaciones/{id}/variables-score: las variables
        /// ponderadas del modelo de score de referencia (ver
        /// Api/Models/ScoreApiModels.cs -> VariablesScoreCalculator) para
        /// esta evaluación.
        /// </summary>
        [HttpGet]
        [Route("{id:int}/variables-score")]
        public HttpResponseMessage GetVariablesScore(int id)
        {
            Evaluacion evaluacion = _evaluacionRepository.ObtenerPorId(id);
            if (evaluacion == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format("No existe la evaluación #{0}.", id));
            }

            SibPerfil sib = SibXmlParser.Parsear(evaluacion.SibRawXml);
            bool datosCompletos = sib != null;
            if (sib == null)
            {
                // Mismo criterio de degradación que Evaluacion/Resultado.aspx.cs:
                // si el XML no se pudo interpretar, se arma un perfil parcial con
                // lo que sí quedó en las columnas planas, en vez de fallar la
                // respuesta completa.
                sib = new SibPerfil
                {
                    ScoreBuro = evaluacion.SibScoreBuro ?? 0,
                    NivelEndeudamiento = evaluacion.SibNivelEndeudamiento ?? 0m,
                    IngresosReportados = 0m
                };
            }

            VariablesScoreApiResponse respuesta = VariablesScoreCalculator.Calcular(evaluacion, sib, datosCompletos);
            return Request.CreateResponse(HttpStatusCode.OK, respuesta);
        }

        private static EvaluacionApiResponse MapearRespuesta(Evaluacion evaluacion, string productoCodigo)
        {
            return new EvaluacionApiResponse
            {
                EvaluacionId = evaluacion.EvaluacionId,
                ProductoCodigo = productoCodigo,
                ProductoNombre = evaluacion.ProductoNombre,
                ClienteNombreCompleto = evaluacion.NombreCompleto,
                Nit = evaluacion.Nit,
                Dpi = evaluacion.Dpi,
                ResultadoGeneral = evaluacion.ResultadoGeneral.ToDbValue(),
                ResultadoGeneralEtiqueta = evaluacion.ResultadoGeneral.Etiqueta(),
                Politicas = evaluacion.Politicas
                    .OrderBy(p => p.Orden)
                    .Select(p => new PoliticaEvaluadaApiDto
                    {
                        PoliticaId = p.PoliticaId,
                        Nombre = p.NombrePolitica,
                        Resultado = p.Resultado.ToDbValue(),
                        Detalle = p.Detalle,
                        Orden = p.Orden
                    })
                    .ToList(),
                Sib = new SibResumenApiDto
                {
                    ScoreBuro = evaluacion.SibScoreBuro,
                    NivelEndeudamiento = evaluacion.SibNivelEndeudamiento,
                    EnListaNegra = evaluacion.SibEnListaNegra
                },
                FechaEvaluacion = evaluacion.FechaCreacion
            };
        }

        private static int ObtenerUsuarioIntegracionId()
        {
            if (_usuarioIntegracionIdCache.HasValue) return _usuarioIntegracionIdCache.Value;

            string email = ConfigurationManager.AppSettings["ApiUsuarioIntegracionEmail"];
            if (string.IsNullOrWhiteSpace(email)) email = "integracion@creditpulse.com";

            Usuario usuario = new UsuarioRepository().ObtenerPorEmail(email);
            if (usuario == null)
            {
                throw new InvalidOperationException(string.Format(
                    "No se encontró el usuario de servicio '{0}' para atribuir evaluaciones creadas por API. " +
                    "Ejecuta Database/11_ApiIntegraciones.sql.", email));
            }

            _usuarioIntegracionIdCache = usuario.UsuarioId;
            return _usuarioIntegracionIdCache.Value;
        }
    }
}
