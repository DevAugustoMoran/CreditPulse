using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.DAL
{
    public class LoteRepository
    {
        /// <summary>
        /// Crea el encabezado del lote de inmediato, en estado Procesando,
        /// antes de evaluar la primera fila (ver BatchEvaluationService).
        /// Así el reporte existe y es consultable desde el primer momento,
        /// en vez de aparecer recién cuando termina todo el archivo.
        /// </summary>
        public int CrearPendiente(string nombreArchivo, int usuarioId, int totalRegistros)
        {
            using (var connection = DbHelper.GetConnection())
            using (var command = new SqlCommand("dbo.sp_Lotes_CrearPendiente", connection) { CommandType = CommandType.StoredProcedure })
            {
                command.Parameters.AddWithValue("@NombreArchivo", nombreArchivo);
                command.Parameters.AddWithValue("@UsuarioId", usuarioId);
                command.Parameters.AddWithValue("@TotalRegistros", totalRegistros);

                var output = new SqlParameter("@LoteId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                command.Parameters.Add(output);

                connection.Open();
                command.ExecuteNonQuery();
                return (int)output.Value;
            }
        }

        /// <summary>Se llama al terminar cada bloque (chunk) de filas, para que el reporte muestre avance real.</summary>
        public void ActualizarProgreso(int loteId, int totalProcesados, int totalVerde, int totalAmarillo, int totalRojo, int totalErrores)
        {
            DbHelper.ExecuteNonQuery("dbo.sp_Lotes_ActualizarProgreso", new[]
            {
                new SqlParameter("@LoteId", loteId),
                new SqlParameter("@TotalProcesados", totalProcesados),
                new SqlParameter("@TotalVerde", totalVerde),
                new SqlParameter("@TotalAmarillo", totalAmarillo),
                new SqlParameter("@TotalRojo", totalRojo),
                new SqlParameter("@TotalErrores", totalErrores)
            });
        }

        /// <summary>Cierra el lote en un estado terminal (Completado o Error).</summary>
        public void CambiarEstado(int loteId, EstadoLote estado, string mensajeError = null)
        {
            DbHelper.ExecuteNonQuery("dbo.sp_Lotes_CambiarEstado", new[]
            {
                new SqlParameter("@LoteId", loteId),
                new SqlParameter("@Estado", estado.ToDbValue()),
                new SqlParameter("@MensajeError", DbHelper.OrDbNull(mensajeError))
            });
        }

        /// <summary>
        /// Inserta todas las filas de un bloque (chunk) en un solo viaje a
        /// SQL Server con un Table-Valued Parameter, en vez de un INSERT por
        /// fila (ver Database/09_BatchAsincrono.sql).
        /// </summary>
        public void InsertarFilasDetalle(int loteId, List<LoteEvaluacion> filas)
        {
            if (filas == null || filas.Count == 0) return;

            DataTable tabla = ConstruirTablaFilas(filas);

            using (var connection = DbHelper.GetConnection())
            using (var command = new SqlCommand("dbo.sp_LoteEvaluaciones_InsertarLote", connection) { CommandType = CommandType.StoredProcedure })
            {
                command.Parameters.AddWithValue("@LoteId", loteId);

                var tvp = new SqlParameter("@Filas", SqlDbType.Structured) { TypeName = "dbo.LoteEvaluacionTVP", Value = tabla };
                command.Parameters.Add(tvp);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static DataTable ConstruirTablaFilas(List<LoteEvaluacion> filas)
        {
            var tabla = new DataTable();
            tabla.Columns.Add("EvaluacionId", typeof(int));
            tabla.Columns.Add("FilaExcel", typeof(int));
            tabla.Columns.Add("NombreCompletoOriginal", typeof(string));
            tabla.Columns.Add("NitOriginal", typeof(string));
            tabla.Columns.Add("DpiOriginal", typeof(string));
            tabla.Columns.Add("Estado", typeof(string));
            tabla.Columns.Add("MensajeError", typeof(string));

            foreach (var fila in filas)
            {
                tabla.Rows.Add(
                    (object)fila.EvaluacionId ?? DBNull.Value,
                    fila.FilaExcel,
                    (object)fila.NombreCompletoOriginal ?? DBNull.Value,
                    (object)fila.NitOriginal ?? DBNull.Value,
                    (object)fila.DpiOriginal ?? DBNull.Value,
                    fila.Estado.ToDbValue(),
                    (object)fila.MensajeError ?? DBNull.Value);
            }

            return tabla;
        }

        public Lote ObtenerPorId(int loteId)
        {
            var lote = DbHelper.ExecuteReaderSingle(
                "dbo.sp_Lotes_ObtenerPorId",
                new[] { new SqlParameter("@LoteId", loteId) },
                MapEncabezado);

            if (lote == null) return null;

            lote.Detalle = DbHelper.ExecuteReader(
                "dbo.sp_LoteEvaluaciones_ObtenerPorLote",
                new[] { new SqlParameter("@LoteId", loteId) },
                MapFila);

            return lote;
        }

        /// <summary>Solo el encabezado del lote (sin cargar sus filas): para pantallas que paginan el detalle por su cuenta, como el reporte de batch.</summary>
        public Lote ObtenerEncabezado(int loteId)
        {
            return DbHelper.ExecuteReaderSingle(
                "dbo.sp_Lotes_ObtenerPorId",
                new[] { new SqlParameter("@LoteId", loteId) },
                MapEncabezado);
        }

        /// <summary>Lotes para el selector del reporte de batch (más recientes primero). Requiere Database/13_Reportes.sql.</summary>
        public List<Lote> Listar(int top = 200)
        {
            return DbHelper.ExecuteReader(
                "dbo.sp_Lotes_Listar",
                new[] { new SqlParameter("@Top", top) },
                MapEncabezado);
        }

        public List<Lote> ObtenerRecientes(int top = 10)
        {
            return DbHelper.ExecuteReader(
                "dbo.sp_Lotes_ObtenerRecientes",
                new[] { new SqlParameter("@Top", top) },
                MapEncabezado);
        }

        private static Lote MapEncabezado(IDataRecord r)
        {
            return new Lote
            {
                LoteId = r.GetInt32(r.GetOrdinal("LoteId")),
                NombreArchivo = r.GetString(r.GetOrdinal("NombreArchivo")),
                UsuarioId = HasColumn(r, "UsuarioId") ? r.GetInt32(r.GetOrdinal("UsuarioId")) : 0,
                AnalistaNombre = r.GetString(r.GetOrdinal("AnalistaNombre")),
                TotalRegistros = r.GetInt32(r.GetOrdinal("TotalRegistros")),
                TotalProcesados = HasColumn(r, "TotalProcesados") ? r.GetInt32(r.GetOrdinal("TotalProcesados")) : 0,
                TotalVerde = r.GetInt32(r.GetOrdinal("TotalVerde")),
                TotalAmarillo = r.GetInt32(r.GetOrdinal("TotalAmarillo")),
                TotalRojo = r.GetInt32(r.GetOrdinal("TotalRojo")),
                TotalErrores = r.GetInt32(r.GetOrdinal("TotalErrores")),
                Estado = HasColumn(r, "Estado") ? EstadoLoteExtensions.FromDbValue(r.GetString(r.GetOrdinal("Estado"))) : EstadoLote.Completado,
                MensajeError = HasColumn(r, "MensajeError") ? DbHelper.GetNullableString(r, "MensajeError") : null,
                FechaCreacion = r.GetDateTime(r.GetOrdinal("FechaCreacion"))
            };
        }

        private static LoteEvaluacion MapFila(IDataRecord r)
        {
            return new LoteEvaluacion
            {
                LoteEvaluacionId = r.GetInt32(r.GetOrdinal("LoteEvaluacionId")),
                LoteId = r.GetInt32(r.GetOrdinal("LoteId")),
                EvaluacionId = DbHelper.GetNullable<int>(r, "EvaluacionId"),
                FilaExcel = r.GetInt32(r.GetOrdinal("FilaExcel")),
                NombreCompletoOriginal = DbHelper.GetNullableString(r, "NombreCompletoOriginal"),
                NitOriginal = DbHelper.GetNullableString(r, "NitOriginal"),
                DpiOriginal = DbHelper.GetNullableString(r, "DpiOriginal"),
                Estado = EstadoFilaLoteExtensions.FromDbValue(r.GetString(r.GetOrdinal("Estado"))),
                MensajeError = DbHelper.GetNullableString(r, "MensajeError"),
                ResultadoGeneral = MapResultadoNullable(r)
            };
        }

        private static ResultadoSemaforo? MapResultadoNullable(IDataRecord r)
        {
            string valor = DbHelper.GetNullableString(r, "ResultadoGeneral");
            return valor == null ? (ResultadoSemaforo?)null : ResultadoSemaforoExtensions.FromDbValue(valor);
        }

        private static bool HasColumn(IDataRecord r, string columnName)
        {
            for (int i = 0; i < r.FieldCount; i++)
            {
                if (string.Equals(r.GetName(i), columnName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
