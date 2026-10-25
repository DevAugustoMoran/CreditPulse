using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using CreditPulse.Web.DAL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.BLL
{
    /// <summary>
    /// Orquesta el flujo completo de una evaluación individual:
    /// 1) consulta el perfil crediticio (SIB real o simulador),
    /// 2) ejecuta cada política activa del producto sobre ese perfil,
    /// 3) calcula el semáforo general aplicando la jerarquía de negocio, y
    /// 4) persiste el resultado.
    ///
    /// Jerarquía de negocio (ver requerimiento original del usuario):
    ///   - Si CUALQUIER política da Rojo         -> resultado general Rojo.
    ///   - Si no hay Rojo pero alguna da Amarillo -> resultado general Amarillo.
    ///   - Si TODAS dan Verde                     -> resultado general Verde.
    /// Esto equivale a tomar la severidad máxima entre los resultados,
    /// porque ResultadoSemaforo está ordenado Verde(0) &lt; Amarillo(1) &lt; Rojo(2).
    /// </summary>
    public class EvaluationService
    {
        private readonly ProductoRepository _productoRepository;
        private readonly PoliticaRepository _politicaRepository;
        private readonly EvaluacionRepository _evaluacionRepository;
        private readonly ISibService _sibService;
        private readonly PolicyEngine _policyEngine;

        public EvaluationService()
            : this(new ProductoRepository(), new PoliticaRepository(), new EvaluacionRepository(), CrearSibService(), new PolicyEngine())
        {
        }

        public EvaluationService(ProductoRepository productoRepository, PoliticaRepository politicaRepository,
            EvaluacionRepository evaluacionRepository, ISibService sibService, PolicyEngine policyEngine)
        {
            _productoRepository = productoRepository;
            _politicaRepository = politicaRepository;
            _evaluacionRepository = evaluacionRepository;
            _sibService = sibService;
            _policyEngine = policyEngine;
        }

        /// <summary>
        /// Decide, según el appSetting "UsarSibMock" de Web.config, si la
        /// consulta a la SIB se resuelve con el simulador en memoria
        /// (SibMockService, todo dentro del mismo proceso de
        /// CreditPulse.Web -útil para desarrollo, sin depender de que otro
        /// proyecto esté corriendo) o llamando por HTTP al proyecto
        /// independiente CreditPulse.SibSimulador (SibApiClientService),
        /// que simula cómo se vería integrarse con el servicio real de la
        /// SIB. Por defecto (appSetting ausente o "true") usa el simulador
        /// en memoria, para que el proyecto siga funcionando sin tener que
        /// levantar dos sitios a la vez.
        /// </summary>
        public static ISibService CrearSibService()
        {
            string valor = ConfigurationManager.AppSettings["UsarSibMock"];
            bool usarMock;
            if (!bool.TryParse(valor, out usarMock))
            {
                usarMock = true;
            }

            return usarMock ? (ISibService)new SibMockService() : new SibApiClientService();
        }

        /// <summary>Flujo de Nueva Evaluación (individual): lee producto/políticas, evalúa y guarda. Sin cambios de comportamiento.</summary>
        public Evaluacion EvaluarYGuardar(NuevaEvaluacionRequest solicitud)
        {
            Producto producto = _productoRepository.ObtenerPorId(solicitud.ProductoId);
            if (producto == null)
                throw new InvalidOperationException("El producto seleccionado no existe o no está activo.");

            var politicas = _politicaRepository.ObtenerPorProducto(solicitud.ProductoId);
            if (politicas.Count == 0)
                throw new InvalidOperationException("El producto seleccionado no tiene políticas configuradas.");

            Evaluacion evaluacion = ConstruirEvaluacion(solicitud, producto, politicas);

            int nuevoId = _evaluacionRepository.Guardar(evaluacion);
            evaluacion.EvaluacionId = nuevoId;

            return evaluacion;
        }

        /// <summary>
        /// Evalúa una solicitud contra un producto y sus políticas que el
        /// llamador ya tiene en memoria, SIN volver a leerlos de la base de
        /// datos ni guardar el resultado. La usa Evaluaciones Batch (ver
        /// BatchEvaluationService): Producto y Políticas son los mismos para
        /// las miles de filas de un mismo archivo, así que consultarlos una
        /// sola vez por lote -en vez de una vez por fila- es la primera de
        /// las optimizaciones descritas en Database/09_BatchAsincrono.sql.
        /// El guardado queda a cargo del llamador (ver
        /// EvaluacionRepository.GuardarConDetalleEnLote), para poder
        /// insertarlo también de forma optimizada (TVP) y en bloques.
        /// </summary>
        public Evaluacion EvaluarSinGuardar(NuevaEvaluacionRequest solicitud, Producto producto, List<Politica> politicas)
        {
            return ConstruirEvaluacion(solicitud, producto, politicas);
        }

        private Evaluacion ConstruirEvaluacion(NuevaEvaluacionRequest solicitud, Producto producto, List<Politica> politicas)
        {
            Validar(solicitud);

            string nombreCompleto = string.Join(" ",
                new[] { solicitud.PrimerNombre, solicitud.SegundoNombre, solicitud.TercerNombre, solicitud.PrimerApellido, solicitud.SegundoApellido }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

            SibPerfil perfilSib = _sibService.ConsultarPerfil(solicitud.Dpi, solicitud.Nit, nombreCompleto);
            var contexto = new EvaluationContext(solicitud, perfilSib);

            var evaluacion = new Evaluacion
            {
                ProductoId = producto.ProductoId,
                ProductoNombre = producto.Nombre,
                UsuarioId = solicitud.UsuarioId,
                PrimerNombre = solicitud.PrimerNombre,
                SegundoNombre = solicitud.SegundoNombre,
                TercerNombre = solicitud.TercerNombre,
                PrimerApellido = solicitud.PrimerApellido,
                SegundoApellido = solicitud.SegundoApellido,
                Nit = solicitud.Nit,
                Dpi = solicitud.Dpi,
                FechaNacimiento = solicitud.FechaNacimiento,
                IngresosMensuales = solicitud.IngresosMensuales,
                TipoEmpleo = solicitud.TipoEmpleo,
                AntiguedadLaboralMeses = solicitud.AntiguedadLaboralMeses,
                Estado = "Evaluada",
                SibRawXml = perfilSib.XmlCrudo,
                SibScoreBuro = perfilSib.ScoreBuro,
                SibNivelEndeudamiento = perfilSib.NivelEndeudamiento,
                SibEnListaNegra = perfilSib.EnListaNegra,
                FechaCreacion = DateTime.UtcNow
            };

            ResultadoSemaforo peorResultado = ResultadoSemaforo.Verde;
            int orden = 1;

            foreach (var politica in politicas.OrderBy(p => p.Orden))
            {
                PolicyEvaluationResult resultado = _policyEngine.Evaluar(contexto, politica);

                evaluacion.Politicas.Add(new EvaluacionPolitica
                {
                    PoliticaId = politica.PoliticaId,
                    NombrePolitica = politica.Nombre,
                    Resultado = resultado.Resultado,
                    Detalle = resultado.Detalle,
                    Orden = orden++
                });

                if (resultado.Resultado > peorResultado)
                {
                    peorResultado = resultado.Resultado;
                }
            }

            evaluacion.ResultadoGeneral = peorResultado;

            return evaluacion;
        }

        public Evaluacion ObtenerPorId(int evaluacionId)
        {
            return _evaluacionRepository.ObtenerPorId(evaluacionId);
        }

        private static void Validar(NuevaEvaluacionRequest s)
        {
            if (s.ProductoId <= 0) throw new ArgumentException("Debe seleccionar un producto.");
            if (string.IsNullOrWhiteSpace(s.PrimerNombre)) throw new ArgumentException("El primer nombre es obligatorio.");
            if (string.IsNullOrWhiteSpace(s.PrimerApellido)) throw new ArgumentException("El primer apellido es obligatorio.");
            if (string.IsNullOrWhiteSpace(s.Nit)) throw new ArgumentException("El NIT es obligatorio.");
            if (string.IsNullOrWhiteSpace(s.Dpi)) throw new ArgumentException("El DPI es obligatorio.");
            if (s.FechaNacimiento == default(DateTime)) throw new ArgumentException("La fecha de nacimiento es obligatoria.");
            if (s.FechaNacimiento > DateTime.Today) throw new ArgumentException("La fecha de nacimiento no puede ser futura.");
        }
    }
}
