using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;
using CreditPulse.Web.DAL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.BLL
{
    /// <summary>
    /// Todo lo que <see cref="BatchEvaluationService.PrepararLote"/> deja
    /// listo en memoria para que el procesamiento pesado corra en segundo
    /// plano, sin tener que volver a leer el Excel ni el catálogo
    /// (Producto/Políticas) desde la solicitud HTTP original.
    /// </summary>
    public class LotePendiente
    {
        public int LoteId { get; set; }
        public int UsuarioId { get; set; }
        public Producto ProductoGeneral { get; set; }
        public List<Politica> Politicas { get; set; }
        public DataTable Tabla { get; set; }
        public Dictionary<string, int> Columnas { get; set; }

        /// <summary>Índices (0-based) de las filas de <see cref="Tabla"/> que sí tienen datos (se ignoran filas totalmente vacías).</summary>
        public List<int> IndicesFilasConDatos { get; set; }
    }

    /// <summary>
    /// Procesa un archivo Excel de precalificación masiva (Evaluaciones
    /// Batch): cada fila es un cliente que se evalúa contra el producto
    /// "General" (ver Database/05_Batch.sql), reutilizando exactamente el
    /// mismo <see cref="EvaluationService"/> y el mismo
    /// <see cref="PolicyEngine"/> que usa la evaluación individual — así el
    /// resultado de un cliente no depende de si se evaluó uno por uno o
    /// dentro de un lote.
    ///
    /// Una fila que falla validación (DPI vacío, fecha ilegible, etc.) NO
    /// aborta el lote completo: se registra con su mensaje de error y el
    /// procesamiento continúa con la siguiente fila, para que el reporte
    /// final muestre TODAS las filas del archivo.
    ///
    /// El procesamiento se divide en dos partes (ver Evaluacion/Batch.aspx.cs
    /// y Database/09_BatchAsincrono.sql para el porqué): <see cref="PrepararLote"/>
    /// corre de forma síncrona, dentro de la solicitud HTTP que sube el
    /// archivo, y solo hace trabajo rápido en memoria; <see cref="ProcesarEnSegundoPlano"/>
    /// hace todo el trabajo pesado (una consulta a la SIB y varias
    /// escrituras a SQL Server por fila) y se encola para correr fuera de
    /// esa solicitud, en bloques, actualizando el progreso del lote a
    /// medida que avanza.
    /// </summary>
    public class BatchEvaluationService
    {
        private const string CodigoProductoGeneral = "GENERAL";

        /// <summary>
        /// Filas por bloque: cada bloque hace 2 viajes a SQL Server para
        /// cerrarse (insertar sus LoteEvaluaciones vía TVP + actualizar el
        /// progreso del Lote), sin importar su tamaño. 200 es un punto
        /// intermedio razonable entre "progreso visible con frecuencia" y
        /// "pocos viajes a la base de datos"; con un archivo de 10,000
        /// filas son 50 bloques, es decir, 100 escrituras de cierre de
        /// bloque en vez de una por fila.
        /// </summary>
        private const int TamanoChunk = 200;

        private readonly EvaluationService _evaluationService;
        private readonly ProductoRepository _productoRepository;
        private readonly PoliticaRepository _politicaRepository;
        private readonly EvaluacionRepository _evaluacionRepository;
        private readonly LoteRepository _loteRepository;

        public BatchEvaluationService()
            : this(new EvaluationService(), new ProductoRepository(), new PoliticaRepository(), new EvaluacionRepository(), new LoteRepository())
        {
        }

        public BatchEvaluationService(EvaluationService evaluationService, ProductoRepository productoRepository,
            PoliticaRepository politicaRepository, EvaluacionRepository evaluacionRepository, LoteRepository loteRepository)
        {
            _evaluationService = evaluationService;
            _productoRepository = productoRepository;
            _politicaRepository = politicaRepository;
            _evaluacionRepository = evaluacionRepository;
            _loteRepository = loteRepository;
        }

        /// <summary>
        /// Parte SÍNCRONA: valida el producto "General", lee el Excel a
        /// memoria (solo E/S, sin ninguna llamada a SQL Server) y crea el
        /// Lote de inmediato en estado Procesando. Leer un Excel a un
        /// DataTable es rápido incluso con miles de filas -el archivo ya
        /// está completo en el servidor cuando este método arranca-, así
        /// que la solicitud HTTP siempre responde rápido sin importar
        /// cuántas filas tenga el archivo: el trabajo lento (una consulta a
        /// la SIB y varias escrituras a SQL Server POR FILA) queda en
        /// <see cref="ProcesarEnSegundoPlano"/>, que el llamador debe
        /// encolar con HostingEnvironment.QueueBackgroundWorkItem (ver
        /// Evaluacion/Batch.aspx.cs) para que corra fuera de esta solicitud.
        /// </summary>
        public LotePendiente PrepararLote(Stream archivoExcel, string nombreArchivo, int usuarioId)
        {
            Producto productoGeneral = _productoRepository.ObtenerPorCodigo(CodigoProductoGeneral);
            if (productoGeneral == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el producto \"General\" (código GENERAL). Ejecuta Database/05_Batch.sql antes de usar Evaluaciones Batch.");
            }

            // Se consulta UNA sola vez por lote, no una vez por fila (ver
            // Database/09_BatchAsincrono.sql): Producto y Políticas no
            // cambian mientras se procesa el mismo archivo.
            var politicas = _politicaRepository.ObtenerPorProducto(productoGeneral.ProductoId);
            if (politicas.Count == 0)
            {
                throw new InvalidOperationException("El producto \"General\" no tiene políticas configuradas.");
            }

            DataTable tabla = LeerExcel(archivoExcel);
            Dictionary<string, int> columnas = MapearColumnas(tabla);

            var indicesFilasConDatos = new List<int>();
            for (int i = 0; i < tabla.Rows.Count; i++)
            {
                if (!EsFilaVacia(tabla.Rows[i])) indicesFilasConDatos.Add(i);
            }

            if (indicesFilasConDatos.Count == 0)
            {
                throw new InvalidOperationException("El archivo no contiene filas con datos para evaluar.");
            }

            int loteId = _loteRepository.CrearPendiente(nombreArchivo, usuarioId, indicesFilasConDatos.Count);

            return new LotePendiente
            {
                LoteId = loteId,
                UsuarioId = usuarioId,
                ProductoGeneral = productoGeneral,
                Politicas = politicas,
                Tabla = tabla,
                Columnas = columnas,
                IndicesFilasConDatos = indicesFilasConDatos
            };
        }

        /// <summary>
        /// Parte EN SEGUNDO PLANO: procesa el archivo ya leído en bloques de
        /// <see cref="TamanoChunk"/> filas, guardando el detalle de cada
        /// bloque y actualizando el progreso del lote al terminarlo. Si el
        /// proceso se interrumpe a la mitad (una excepción no prevista, la
        /// conexión a SQL Server se cae, etc.), las filas de los bloques ya
        /// guardados quedan correctamente asociadas al lote -no se pierden
        /// ni quedan huérfanas- y el lote se marca en estado Error con un
        /// mensaje en vez de quedarse "Procesando" para siempre.
        /// </summary>
        public void ProcesarEnSegundoPlano(LotePendiente pendiente)
        {
            int totalProcesados = 0, totalVerde = 0, totalAmarillo = 0, totalRojo = 0, totalErrores = 0;

            try
            {
                var bloqueActual = new List<LoteEvaluacion>(TamanoChunk);

                foreach (int indiceFila in pendiente.IndicesFilasConDatos)
                {
                    int filaExcel = indiceFila + 2; // la fila 1 del archivo es el encabezado
                    DataRow fila = pendiente.Tabla.Rows[indiceFila];

                    LoteEvaluacion resultado = ProcesarFila(fila, filaExcel, pendiente);
                    bloqueActual.Add(resultado);
                    totalProcesados++;

                    if (resultado.Estado == EstadoFilaLote.Error)
                    {
                        totalErrores++;
                    }
                    else if (resultado.ResultadoGeneral == ResultadoSemaforo.Verde) totalVerde++;
                    else if (resultado.ResultadoGeneral == ResultadoSemaforo.Amarillo) totalAmarillo++;
                    else if (resultado.ResultadoGeneral == ResultadoSemaforo.Rojo) totalRojo++;

                    if (bloqueActual.Count >= TamanoChunk)
                    {
                        GuardarBloqueYProgreso(pendiente.LoteId, bloqueActual, totalProcesados, totalVerde, totalAmarillo, totalRojo, totalErrores);
                        bloqueActual = new List<LoteEvaluacion>(TamanoChunk);
                    }
                }

                if (bloqueActual.Count > 0)
                {
                    GuardarBloqueYProgreso(pendiente.LoteId, bloqueActual, totalProcesados, totalVerde, totalAmarillo, totalRojo, totalErrores);
                }

                _loteRepository.CambiarEstado(pendiente.LoteId, EstadoLote.Completado);
            }
            catch (Exception ex)
            {
                // Punto único de recuperación: el lote nunca se queda
                // "Procesando" para siempre. Las filas que ya se guardaron
                // en bloques anteriores siguen visibles en el reporte.
                try
                {
                    _loteRepository.CambiarEstado(pendiente.LoteId, EstadoLote.Error, Truncar(ex.Message, 400));
                }
                catch
                {
                    // Si ni siquiera se puede escribir el estado de error
                    // (por ejemplo, la base de datos ya no responde), no
                    // queda nada más por hacer desde este hilo.
                }
            }
        }

        private void GuardarBloqueYProgreso(int loteId, List<LoteEvaluacion> bloque, int totalProcesados, int totalVerde, int totalAmarillo, int totalRojo, int totalErrores)
        {
            _loteRepository.InsertarFilasDetalle(loteId, bloque);
            _loteRepository.ActualizarProgreso(loteId, totalProcesados, totalVerde, totalAmarillo, totalRojo, totalErrores);
        }

        private LoteEvaluacion ProcesarFila(DataRow fila, int filaExcel, LotePendiente pendiente)
        {
            Dictionary<string, int> columnas = pendiente.Columnas;

            string primerNombre = ObtenerValor(fila, columnas, "primernombre");
            string segundoNombre = ObtenerValor(fila, columnas, "segundonombre");
            string tercerNombre = ObtenerValor(fila, columnas, "tercernombre"); // columna opcional
            string primerApellido = ObtenerValor(fila, columnas, "primerapellido");
            string segundoApellido = ObtenerValor(fila, columnas, "segundoapellido");
            string nit = ObtenerValor(fila, columnas, "nit");
            string dpi = ObtenerValor(fila, columnas, "dpi");
            string fechaTexto = ObtenerValor(fila, columnas, "fechanacimiento");

            string nombreCompletoOriginal = string.Join(" ",
                new[] { primerNombre, segundoNombre, tercerNombre, primerApellido, segundoApellido }.Where(s => !string.IsNullOrWhiteSpace(s)));

            try
            {
                if (string.IsNullOrWhiteSpace(primerNombre)) throw new ArgumentException("Falta el primer nombre.");
                if (string.IsNullOrWhiteSpace(primerApellido)) throw new ArgumentException("Falta el primer apellido.");
                if (string.IsNullOrWhiteSpace(nit)) throw new ArgumentException("Falta el NIT.");
                if (string.IsNullOrWhiteSpace(dpi)) throw new ArgumentException("Falta el DPI.");

                // Validación inicial del DPI (formato, departamento/municipio y
                // dígito verificador, ver DpiValidator). Una fila con DPI mal
                // formado se reporta con su motivo y NO se envía a la SIB.
                DpiValidationResult dpiValidado = DpiValidator.Validar(dpi);
                if (!dpiValidado.EsValido) throw new ArgumentException(dpiValidado.Mensaje);

                DateTime fechaNacimiento = ParsearFecha(fechaTexto);

                var solicitud = new NuevaEvaluacionRequest
                {
                    ProductoId = pendiente.ProductoGeneral.ProductoId,
                    UsuarioId = pendiente.UsuarioId,
                    PrimerNombre = primerNombre.Trim(),
                    SegundoNombre = string.IsNullOrWhiteSpace(segundoNombre) ? null : segundoNombre.Trim(),
                    TercerNombre = string.IsNullOrWhiteSpace(tercerNombre) ? null : tercerNombre.Trim(),
                    PrimerApellido = primerApellido.Trim(),
                    SegundoApellido = string.IsNullOrWhiteSpace(segundoApellido) ? null : segundoApellido.Trim(),
                    Nit = nit.Trim(),
                    Dpi = dpiValidado.DpiNormalizado,
                    FechaNacimiento = fechaNacimiento
                };

                // Evalúa con el Producto/Políticas ya cacheados (sin
                // releerlos) y guarda con el camino optimizado por TVP
                // (ver EvaluacionRepository.GuardarConDetalleEnLote).
                Models.Evaluacion evaluado = _evaluationService.EvaluarSinGuardar(solicitud, pendiente.ProductoGeneral, pendiente.Politicas);
                int evaluacionId = _evaluacionRepository.GuardarConDetalleEnLote(evaluado);

                return new LoteEvaluacion
                {
                    FilaExcel = filaExcel,
                    NombreCompletoOriginal = nombreCompletoOriginal,
                    NitOriginal = nit,
                    DpiOriginal = dpi,
                    Estado = EstadoFilaLote.Evaluado,
                    EvaluacionId = evaluacionId,
                    ResultadoGeneral = evaluado.ResultadoGeneral
                };
            }
            catch (Exception ex)
            {
                return new LoteEvaluacion
                {
                    FilaExcel = filaExcel,
                    NombreCompletoOriginal = nombreCompletoOriginal,
                    NitOriginal = nit,
                    DpiOriginal = dpi,
                    Estado = EstadoFilaLote.Error,
                    MensajeError = ex.Message
                };
            }
        }

        private static DataTable LeerExcel(Stream stream)
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var config = new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
                };

                DataSet dataSet = reader.AsDataSet(config);

                if (dataSet.Tables.Count == 0)
                {
                    throw new InvalidOperationException("El archivo Excel no contiene ninguna hoja con datos.");
                }

                return dataSet.Tables[0];
            }
        }

        private static Dictionary<string, int> MapearColumnas(DataTable tabla)
        {
            var mapa = new Dictionary<string, int>();
            for (int i = 0; i < tabla.Columns.Count; i++)
            {
                string clave = NormalizarEncabezado(tabla.Columns[i].ColumnName);
                if (!mapa.ContainsKey(clave))
                {
                    mapa[clave] = i;
                }
            }
            return mapa;
        }

        /// <summary>
        /// Normaliza encabezados quitando espacios, tildes y mayúsculas para
        /// que "Primer Nombre", "primer nombre" o "PRIMER  NOMBRE" mapeen
        /// todos a la misma clave interna ("primernombre").
        /// </summary>
        private static string NormalizarEncabezado(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            string sinEspacios = texto.Trim().ToLowerInvariant().Replace(" ", "");
            string normalizado = sinEspacios.Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder();
            foreach (char c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string ObtenerValor(DataRow fila, Dictionary<string, int> columnas, string clave)
        {
            int indice;
            if (!columnas.TryGetValue(clave, out indice)) return null;

            object valor = fila[indice];
            if (valor == null || valor == DBNull.Value) return null;

            // Excel guarda las fechas como DateTime real, no como texto.
            if (valor is DateTime)
            {
                return ((DateTime)valor).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            return valor.ToString().Trim();
        }

        private static bool EsFilaVacia(DataRow fila)
        {
            foreach (object valor in fila.ItemArray)
            {
                if (valor != null && valor != DBNull.Value && !string.IsNullOrWhiteSpace(valor.ToString()))
                {
                    return false;
                }
            }
            return true;
        }

        private static DateTime ParsearFecha(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new ArgumentException("Falta la fecha de nacimiento.");

            DateTime fecha;
            string[] formatos = { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "yyyy/MM/dd" };

            if (DateTime.TryParseExact(texto, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
                return fecha;

            if (DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
                return fecha;

            throw new ArgumentException(string.Format(
                "Fecha de nacimiento '{0}' no tiene un formato reconocible (usa AAAA-MM-DD).", texto));
        }

        private static string Truncar(string texto, int maxLength)
        {
            if (string.IsNullOrEmpty(texto) || texto.Length <= maxLength) return texto;
            return texto.Substring(0, maxLength);
        }
    }
}
