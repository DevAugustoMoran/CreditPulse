using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using CreditPulse.Web.BLL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.DAL
{
    /// <summary>Lecturas del módulo de reportes (ver Database/13_Reportes.sql). Solo consulta; no modifica datos.</summary>
    public class ReporteRepository
    {
        /// <summary>Evaluaciones con FechaCreacion en [desdeUtc, hastaUtc). resultado = null para todos los colores.</summary>
        public List<ReporteEvaluacionFila> ObtenerEvaluaciones(DateTime desdeUtc, DateTime hastaUtc, ResultadoSemaforo? resultado, int offset, int limit)
        {
            return DbHelper.ExecuteReader(
                "dbo.sp_Reporte_Evaluaciones",
                new[]
                {
                    FechaParam("@Desde", desdeUtc),
                    FechaParam("@Hasta", hastaUtc),
                    ResultadoParam(resultado),
                    new SqlParameter("@Offset", offset),
                    new SqlParameter("@Limit", limit)
                },
                r => new ReporteEvaluacionFila
                {
                    EvaluacionId = r.GetInt32(r.GetOrdinal("EvaluacionId")),
                    ClienteNombre = r.GetString(r.GetOrdinal("ClienteNombre")),
                    Dpi = r.GetString(r.GetOrdinal("Dpi")),
                    ProductoNombre = r.GetString(r.GetOrdinal("ProductoNombre")),
                    ResultadoGeneral = ResultadoSemaforoExtensions.FromDbValue(r.GetString(r.GetOrdinal("ResultadoGeneral"))),
                    ScoreBuro = DbHelper.GetNullable<int>(r, "SibScoreBuro"),
                    FechaCreacion = r.GetDateTime(r.GetOrdinal("FechaCreacion"))
                });
        }

        public ReporteEvaluacionesResumen ObtenerResumenEvaluaciones(DateTime desdeUtc, DateTime hastaUtc, ResultadoSemaforo? resultado)
        {
            var filas = DbHelper.ExecuteReader(
                "dbo.sp_Reporte_Evaluaciones_Resumen",
                new[] { FechaParam("@Desde", desdeUtc), FechaParam("@Hasta", hastaUtc), ResultadoParam(resultado) },
                r =>
                {
                    int scoreOrdinal = r.GetOrdinal("ScorePromedio");
                    return new ReporteEvaluacionesResumen
                    {
                        Total = r.GetInt32(r.GetOrdinal("Total")),
                        Verde = r.GetInt32(r.GetOrdinal("TotalVerde")),
                        Amarillo = r.GetInt32(r.GetOrdinal("TotalAmarillo")),
                        Rojo = r.GetInt32(r.GetOrdinal("TotalRojo")),
                        ScorePromedio = r.IsDBNull(scoreOrdinal) ? (double?)null : r.GetDouble(scoreOrdinal)
                    };
                });

            return filas.Count > 0 ? filas[0] : new ReporteEvaluacionesResumen();
        }

        /// <summary>Filas de un batch con la información de la SIB ya interpretada (SibXmlParser) para cada una.</summary>
        public List<ReporteBatchFila> ObtenerBatch(int loteId, int offset, int limit)
        {
            List<ReporteBatchFila> filas = DbHelper.ExecuteReader(
                "dbo.sp_Reporte_Batch",
                new[]
                {
                    new SqlParameter("@LoteId", loteId),
                    new SqlParameter("@Offset", offset),
                    new SqlParameter("@Limit", limit)
                },
                MapBatch);

            foreach (ReporteBatchFila fila in filas)
            {
                fila.Sib = SibXmlParser.Parsear(fila.SibRawXml);
            }

            return filas;
        }

        private static ReporteBatchFila MapBatch(IDataRecord r)
        {
            string resultado = DbHelper.GetNullableString(r, "ResultadoGeneral");

            return new ReporteBatchFila
            {
                LoteId = r.GetInt32(r.GetOrdinal("LoteId")),
                FilaExcel = r.GetInt32(r.GetOrdinal("FilaExcel")),
                EstadoFila = EstadoFilaLoteExtensions.FromDbValue(r.GetString(r.GetOrdinal("EstadoFila"))),
                MensajeError = DbHelper.GetNullableString(r, "MensajeError"),
                EvaluacionId = DbHelper.GetNullable<int>(r, "EvaluacionId"),
                NombreCompletoOriginal = DbHelper.GetNullableString(r, "NombreCompletoOriginal"),
                NitOriginal = DbHelper.GetNullableString(r, "NitOriginal"),
                DpiOriginal = DbHelper.GetNullableString(r, "DpiOriginal"),
                ClienteNombre = DbHelper.GetNullableString(r, "ClienteNombre"),
                Nit = DbHelper.GetNullableString(r, "Nit"),
                Dpi = DbHelper.GetNullableString(r, "Dpi"),
                FechaNacimiento = DbHelper.GetNullable<DateTime>(r, "FechaNacimiento"),
                ProductoNombre = DbHelper.GetNullableString(r, "ProductoNombre"),
                ResultadoGeneral = resultado == null ? (ResultadoSemaforo?)null : ResultadoSemaforoExtensions.FromDbValue(resultado),
                ScoreBuro = DbHelper.GetNullable<int>(r, "SibScoreBuro"),
                NivelEndeudamiento = DbHelper.GetNullable<decimal>(r, "SibNivelEndeudamiento"),
                EnListaNegra = DbHelper.GetNullable<bool>(r, "SibEnListaNegra"),
                SibRawXml = DbHelper.GetNullableString(r, "SibRawXml"),
                FechaEvaluacion = DbHelper.GetNullable<DateTime>(r, "FechaEvaluacion")
            };
        }

        private static SqlParameter FechaParam(string nombre, DateTime valor)
        {
            return new SqlParameter(nombre, SqlDbType.DateTime2) { Value = valor };
        }

        private static SqlParameter ResultadoParam(ResultadoSemaforo? resultado)
        {
            return new SqlParameter("@Resultado", SqlDbType.NVarChar, 10)
            {
                Value = resultado.HasValue ? (object)resultado.Value.ToDbValue() : DBNull.Value
            };
        }
    }
}
