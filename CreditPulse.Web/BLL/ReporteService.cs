using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CreditPulse.Web.DAL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.BLL
{
    /// <summary>
    /// Reglas del módulo de reportes: arma los límites de un mes en hora de
    /// Guatemala (convertidos a UTC, que es como se guardan las fechas), y
    /// genera los archivos CSV de exportación.
    /// </summary>
    public class ReporteService
    {
        private const int TamanoBloqueExportacion = 2000;

        private static readonly string[] NombresMeses =
        {
            "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
            "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
        };

        private readonly ReporteRepository _repositorio;

        public ReporteService() : this(new ReporteRepository()) { }

        public ReporteService(ReporteRepository repositorio)
        {
            _repositorio = repositorio;
        }

        public static string NombreMes(int mes)
        {
            return NombresMeses[mes - 1];
        }

        /// <summary>"Color" de una evaluación tal como se muestra en los reportes.</summary>
        public static string NombreColor(ResultadoSemaforo resultado)
        {
            switch (resultado)
            {
                case ResultadoSemaforo.Verde: return "Verde";
                case ResultadoSemaforo.Amarillo: return "Amarillo";
                default: return "Rojo";
            }
        }

        // ---------- Reporte de evaluaciones por mes ----------

        public ReporteEvaluacionesResumen ResumenDelMes(int anio, int mes, ResultadoSemaforo? resultado)
        {
            return _repositorio.ObtenerResumenEvaluaciones(
                ZonaHoraria.InicioDeMesEnUtc(anio, mes), ZonaHoraria.FinDeMesEnUtc(anio, mes), resultado);
        }

        public List<ReporteEvaluacionFila> EvaluacionesDelMes(int anio, int mes, ResultadoSemaforo? resultado, int pagina, int tamanoPagina)
        {
            return _repositorio.ObtenerEvaluaciones(
                ZonaHoraria.InicioDeMesEnUtc(anio, mes), ZonaHoraria.FinDeMesEnUtc(anio, mes),
                resultado, Math.Max(0, pagina) * tamanoPagina, tamanoPagina);
        }

        /// <summary>
        /// El reporte de evaluaciones de un mes completo (todas las páginas) como tabla genérica,
        /// que luego se escribe como CSV, Excel o PDF. Siempre es la misma información que ve el usuario en pantalla.
        /// </summary>
        public TablaReporte ConstruirTablaEvaluaciones(int anio, int mes, ResultadoSemaforo? resultado)
        {
            var tabla = new TablaReporte
            {
                Titulo = string.Format("Reporte de evaluaciones - {0} {1}", NombreMes(mes), anio),
                Subtitulo = "Color: " + (resultado.HasValue ? NombreColor(resultado.Value) : "Todos") + " · el mes se cuenta en hora de Guatemala",
                GeneradoEn = TextoGenerado()
            };

            ReporteEvaluacionesResumen resumen = ResumenDelMes(anio, mes, resultado);
            tabla.Resumen.Add(string.Format(CultureInfo.InvariantCulture,
                "Total: {0:N0} · Verde: {1:N0} · Amarillo: {2:N0} · Rojo: {3:N0} · Score promedio (SIB): {4}",
                resumen.Total, resumen.Verde, resumen.Amarillo, resumen.Rojo,
                resumen.ScorePromedio.HasValue ? Math.Round(resumen.ScorePromedio.Value).ToString("0", CultureInfo.InvariantCulture) : "N/D"));

            tabla.Columnas.Add(new ColumnaReporte("No. Evaluación", 1.0, numerica: true, tituloPdf: "No."));
            tabla.Columnas.Add(new ColumnaReporte("Cliente", 3.4));
            tabla.Columnas.Add(new ColumnaReporte("DPI", 1.7));
            tabla.Columnas.Add(new ColumnaReporte("Producto", 2.4));
            tabla.Columnas.Add(new ColumnaReporte("Color", 1.0, esColor: true));
            tabla.Columnas.Add(new ColumnaReporte("Score crediticio (SIB)", 1.2, numerica: true, tituloPdf: "Score crediticio"));
            tabla.Columnas.Add(new ColumnaReporte("Fecha de evaluación (hora de Guatemala)", 1.7, tituloPdf: "Fecha de evaluación"));

            DateTime desde = ZonaHoraria.InicioDeMesEnUtc(anio, mes);
            DateTime hasta = ZonaHoraria.FinDeMesEnUtc(anio, mes);

            int offset = 0;
            while (true)
            {
                List<ReporteEvaluacionFila> bloque = _repositorio.ObtenerEvaluaciones(desde, hasta, resultado, offset, TamanoBloqueExportacion);
                foreach (ReporteEvaluacionFila f in bloque)
                {
                    tabla.Filas.Add(new[]
                    {
                        f.EvaluacionId.ToString(CultureInfo.InvariantCulture),
                        f.ClienteNombre,
                        f.Dpi,
                        f.ProductoNombre,
                        NombreColor(f.ResultadoGeneral),
                        f.ScoreBuro.HasValue ? f.ScoreBuro.Value.ToString(CultureInfo.InvariantCulture) : "",
                        ZonaHoraria.Formato(f.FechaCreacion)
                    });
                }

                if (bloque.Count < TamanoBloqueExportacion) break;
                offset += TamanoBloqueExportacion;
            }

            return tabla;
        }

        /// <summary>CSV completo (todas las páginas) del reporte de evaluaciones de un mes.</summary>
        public void EscribirCsvEvaluaciones(TextWriter salida, int anio, int mes, ResultadoSemaforo? resultado)
        {
            EscribirCsv(salida, ConstruirTablaEvaluaciones(anio, mes, resultado));
        }

        // ---------- Reporte de clientes por batch ----------

        public List<ReporteBatchFila> FilasDelBatch(int loteId, int pagina, int tamanoPagina)
        {
            return _repositorio.ObtenerBatch(loteId, Math.Max(0, pagina) * tamanoPagina, tamanoPagina);
        }

        /// <summary>
        /// Todas las filas de un batch como tabla genérica: cliente, información de la SIB, número de batch y fecha de evaluación.
        /// El CSV y el Excel llevan las 26 columnas; el PDF, solo las principales (no caben todas en una hoja).
        /// </summary>
        public TablaReporte ConstruirTablaBatch(Lote lote)
        {
            int loteId = lote.LoteId;
            var tabla = new TablaReporte
            {
                Titulo = string.Format("Reporte de clientes evaluados - Batch #{0}", loteId),
                Subtitulo = string.Format("Archivo: {0} · Cargado: {1} · Analista: {2}", lote.NombreArchivo, ZonaHoraria.Formato(lote.FechaCreacion), lote.AnalistaNombre),
                GeneradoEn = TextoGenerado()
            };
            tabla.Resumen.Add(string.Format(CultureInfo.InvariantCulture,
                "Registros: {0:N0} · Verde: {1:N0} · Amarillo: {2:N0} · Rojo: {3:N0} · Errores: {4:N0}",
                lote.TotalRegistros, lote.TotalVerde, lote.TotalAmarillo, lote.TotalRojo, lote.TotalErrores));

            var c = tabla.Columnas;
            c.Add(new ColumnaReporte("Batch", 0.6, numerica: true, enPdf: false));
            c.Add(new ColumnaReporte("Fila del Excel", 0.5, numerica: true, tituloPdf: "Fila"));
            c.Add(new ColumnaReporte("Estado", 1.05));
            c.Add(new ColumnaReporte("Motivo de error", 1.7, tituloPdf: "Motivo de error"));
            c.Add(new ColumnaReporte("No. Evaluación", 0.75, numerica: true, tituloPdf: "No. Eval."));
            c.Add(new ColumnaReporte("Fecha de evaluación (hora de Guatemala)", 1.3, tituloPdf: "Fecha de evaluación"));
            c.Add(new ColumnaReporte("Cliente", 2.15));
            c.Add(new ColumnaReporte("NIT", 1.0, enPdf: false));
            c.Add(new ColumnaReporte("DPI", 1.5));
            c.Add(new ColumnaReporte("Fecha de nacimiento", 1.0, enPdf: false));
            c.Add(new ColumnaReporte("Producto", 1.6));
            c.Add(new ColumnaReporte("Color", 1.0, esColor: true));
            c.Add(new ColumnaReporte("Score crediticio (SIB)", 0.75, numerica: true, tituloPdf: "Score SIB"));
            c.Add(new ColumnaReporte("Nivel de endeudamiento (%)", 1.0, numerica: true, tituloPdf: "Endeud. (%)"));
            c.Add(new ColumnaReporte("Ingresos reportados a la SIB (Q)", 1.05, numerica: true, tituloPdf: "Ingresos SIB (Q)"));
            c.Add(new ColumnaReporte("Créditos activos", 0.95, numerica: true, tituloPdf: "Activos"));
            c.Add(new ColumnaReporte("Créditos cancelados", 0.8, numerica: true, enPdf: false));
            c.Add(new ColumnaReporte("Moras últimos 12 meses", 0.75, numerica: true, tituloPdf: "Moras 12 m"));
            c.Add(new ColumnaReporte("Días de mora máximo", 0.8, numerica: true, enPdf: false));
            c.Add(new ColumnaReporte("Lista negra local", 0.85, tituloPdf: "Lista negra"));
            c.Add(new ColumnaReporte("Lista negra internacional", 0.9, enPdf: false));
            c.Add(new ColumnaReporte("Lista OFAC", 0.7, enPdf: false));
            c.Add(new ColumnaReporte("Cliente existente en el banco", 0.9, enPdf: false));
            c.Add(new ColumnaReporte("Créditos previos en el banco", 0.9, numerica: true, enPdf: false));
            c.Add(new ColumnaReporte("Fuente consultada", 1.5, enPdf: false));
            c.Add(new ColumnaReporte("Fecha de consulta SIB", 1.3, enPdf: false));

            int offset = 0;
            while (true)
            {
                List<ReporteBatchFila> bloque = _repositorio.ObtenerBatch(loteId, offset, TamanoBloqueExportacion);
                foreach (ReporteBatchFila f in bloque)
                {
                    SibPerfil sib = f.Sib;
                    decimal? endeudamiento = sib != null ? sib.NivelEndeudamiento : f.NivelEndeudamiento;
                    int? score = sib != null ? sib.ScoreBuro : f.ScoreBuro;

                    tabla.Filas.Add(new[]
                    {
                        f.LoteId.ToString(CultureInfo.InvariantCulture),
                        f.FilaExcel.ToString(CultureInfo.InvariantCulture),
                        f.EstadoFila == EstadoFilaLote.Evaluado ? "Evaluado" : "Error",
                        f.MensajeError,
                        f.EvaluacionId.HasValue ? f.EvaluacionId.Value.ToString(CultureInfo.InvariantCulture) : "",
                        f.FechaEvaluacion.HasValue ? ZonaHoraria.Formato(f.FechaEvaluacion.Value) : "",
                        f.NombreParaMostrar,
                        f.NitParaMostrar,
                        f.DpiParaMostrar,
                        f.FechaNacimiento.HasValue ? f.FechaNacimiento.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "",
                        f.ProductoNombre,
                        f.ResultadoGeneral.HasValue ? NombreColor(f.ResultadoGeneral.Value) : "",
                        score.HasValue ? score.Value.ToString(CultureInfo.InvariantCulture) : "",
                        endeudamiento.HasValue ? endeudamiento.Value.ToString("0.##", CultureInfo.InvariantCulture) : "",
                        sib != null ? sib.IngresosReportados.ToString("0.##", CultureInfo.InvariantCulture) : "",
                        sib != null ? sib.CreditosActivos.ToString(CultureInfo.InvariantCulture) : "",
                        sib != null ? sib.CreditosCancelados.ToString(CultureInfo.InvariantCulture) : "",
                        sib != null ? sib.MorasUltimos12Meses.ToString(CultureInfo.InvariantCulture) : "",
                        sib != null ? sib.DiasMoraMaximo.ToString(CultureInfo.InvariantCulture) : "",
                        sib != null ? SiNo(sib.EnListaNegraLocal) : (f.EnListaNegra.HasValue ? SiNo(f.EnListaNegra.Value) : ""),
                        sib != null ? SiNo(sib.EnListaNegraInternacional) : "",
                        sib != null ? SiNo(sib.EnListaOfac) : "",
                        sib != null ? SiNo(sib.ClienteExistente) : "",
                        sib != null ? sib.CreditosPreviosEnBanco.ToString(CultureInfo.InvariantCulture) : "",
                        sib != null ? sib.FuenteConsultada : "",
                        sib != null && sib.FechaConsulta != DateTime.MinValue ? sib.FechaConsulta.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) : ""
                    });
                }

                if (bloque.Count < TamanoBloqueExportacion) break;
                offset += TamanoBloqueExportacion;
            }

            return tabla;
        }

        /// <summary>CSV completo (todas las filas) de un batch.</summary>
        public void EscribirCsvBatch(TextWriter salida, int loteId)
        {
            Lote lote = new LoteRepository().ObtenerEncabezado(loteId);
            if (lote == null) throw new InvalidOperationException("No existe el batch #" + loteId + ".");
            EscribirCsv(salida, ConstruirTablaBatch(lote));
        }

        // ---------- CSV ----------

        /// <summary>Bytes UTF-8 con BOM (Excel necesita el BOM para mostrar bien las tildes) del contenido que escribe <paramref name="escribir"/>.</summary>
        public static byte[] ComoCsvBytes(Action<TextWriter> escribir)
        {
            using (var memoria = new MemoryStream())
            {
                using (var escritor = new StreamWriter(memoria, new UTF8Encoding(true)))
                {
                    escribir(escritor);
                }
                return memoria.ToArray();
            }
        }

        /// <summary>Escribe una tabla genérica como CSV: una fila de encabezados y una por registro.</summary>
        public static void EscribirCsv(TextWriter salida, TablaReporte tabla)
        {
            Fila(salida, tabla.Columnas.Select(c => c.Titulo).ToArray());
            foreach (string[] fila in tabla.Filas) Fila(salida, fila);
        }

        private static string TextoGenerado()
        {
            return "Generado el " + ZonaHoraria.Formato(DateTime.UtcNow) + " (hora de Guatemala)";
        }

        private static string SiNo(bool valor)
        {
            return valor ? "Sí" : "No";
        }

        private static void Fila(TextWriter salida, params string[] campos)
        {
            for (int i = 0; i < campos.Length; i++)
            {
                if (i > 0) salida.Write(',');
                salida.Write(EscaparCsv(campos[i]));
            }
            salida.Write("\r\n");
        }

        /// <summary>Escapa un campo CSV (comillas, comas, saltos de línea) y neutraliza fórmulas de Excel (=, +, -, @) al inicio.</summary>
        public static string EscaparCsv(string valor)
        {
            if (string.IsNullOrEmpty(valor)) return string.Empty;

            // Un texto que empieza con = + - @ se interpretaría como fórmula al abrir el CSV en Excel.
            char primero = valor[0];
            if (primero == '=' || primero == '+' || primero == '@' || (primero == '-' && !EsNumero(valor)))
            {
                valor = "'" + valor;
            }

            if (valor.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
            {
                return "\"" + valor.Replace("\"", "\"\"") + "\"";
            }
            return valor;
        }

        private static bool EsNumero(string valor)
        {
            double numero;
            return double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out numero);
        }
    }
}
