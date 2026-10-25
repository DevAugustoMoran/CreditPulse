using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.DAL
{
    public class EvaluacionRepository
    {
        /// <summary>
        /// Persiste el encabezado de la evaluación y el detalle de cada
        /// política evaluada dentro de una sola transacción: si falla el
        /// guardado de un detalle, se revierte también el encabezado.
        /// </summary>
        public int Guardar(Evaluacion evaluacion)
        {
            using (var connection = DbHelper.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int evaluacionId = InsertarEncabezado(connection, transaction, evaluacion);

                        foreach (var politica in evaluacion.Politicas)
                        {
                            InsertarDetallePolitica(connection, transaction, evaluacionId, politica);
                        }

                        transaction.Commit();
                        return evaluacionId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Igual que <see cref="Guardar"/> (encabezado + detalle en una sola
        /// transacción), pero inserta el detalle de políticas con UN solo
        /// Table-Valued Parameter en vez de un INSERT por política. La usa
        /// Evaluaciones Batch (ver BatchEvaluationService y
        /// Database/09_BatchAsincrono.sql): para el producto "General" (5
        /// políticas) esto baja de ~6 idas y vueltas a SQL Server por fila a
        /// solo 2 (encabezado + un TVP con todas las políticas).
        /// </summary>
        public int GuardarConDetalleEnLote(Evaluacion evaluacion)
        {
            using (var connection = DbHelper.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int evaluacionId = InsertarEncabezado(connection, transaction, evaluacion);

                        if (evaluacion.Politicas.Count > 0)
                        {
                            InsertarDetallePoliticasEnLote(connection, transaction, evaluacionId, evaluacion.Politicas);
                        }

                        transaction.Commit();
                        return evaluacionId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private static void InsertarDetallePoliticasEnLote(SqlConnection connection, SqlTransaction transaction, int evaluacionId, List<EvaluacionPolitica> politicas)
        {
            DataTable tabla = ConstruirTablaPoliticas(politicas);

            using (var command = new SqlCommand("dbo.sp_EvaluacionPoliticas_InsertarLote", connection, transaction))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@EvaluacionId", evaluacionId);

                var tvp = new SqlParameter("@Filas", SqlDbType.Structured) { TypeName = "dbo.EvaluacionPoliticaTVP", Value = tabla };
                command.Parameters.Add(tvp);

                command.ExecuteNonQuery();
            }
        }

        private static DataTable ConstruirTablaPoliticas(List<EvaluacionPolitica> politicas)
        {
            var tabla = new DataTable();
            tabla.Columns.Add("PoliticaId", typeof(int));
            tabla.Columns.Add("NombrePolitica", typeof(string));
            tabla.Columns.Add("Resultado", typeof(string));
            tabla.Columns.Add("Detalle", typeof(string));
            tabla.Columns.Add("Orden", typeof(int));

            foreach (var politica in politicas)
            {
                tabla.Rows.Add(
                    (object)politica.PoliticaId ?? DBNull.Value,
                    politica.NombrePolitica,
                    politica.Resultado.ToDbValue(),
                    (object)politica.Detalle ?? DBNull.Value,
                    politica.Orden);
            }

            return tabla;
        }

        private static int InsertarEncabezado(SqlConnection connection, SqlTransaction transaction, Evaluacion e)
        {
            using (var command = new SqlCommand("dbo.sp_Evaluaciones_Crear", connection, transaction))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@ProductoId", e.ProductoId);
                command.Parameters.AddWithValue("@UsuarioId", e.UsuarioId);
                command.Parameters.AddWithValue("@PrimerNombre", e.PrimerNombre);
                command.Parameters.AddWithValue("@SegundoNombre", DbHelper.OrDbNull(e.SegundoNombre));
                // Solo se envía cuando hay tercer nombre: así, si Database/12_TercerNombre.sql
                // todavía no se ejecutó en una base de datos, los flujos que no lo usan (las
                // APIs /api/v1, que no tienen este campo) siguen funcionando igual que antes.
                if (!string.IsNullOrWhiteSpace(e.TercerNombre))
                {
                    command.Parameters.AddWithValue("@TercerNombre", e.TercerNombre);
                }
                command.Parameters.AddWithValue("@PrimerApellido", e.PrimerApellido);
                command.Parameters.AddWithValue("@SegundoApellido", DbHelper.OrDbNull(e.SegundoApellido));
                command.Parameters.AddWithValue("@Nit", e.Nit);
                command.Parameters.AddWithValue("@Dpi", e.Dpi);
                command.Parameters.AddWithValue("@FechaNacimiento", e.FechaNacimiento);
                command.Parameters.AddWithValue("@IngresosMensuales", DbHelper.OrDbNull(e.IngresosMensuales));
                command.Parameters.AddWithValue("@TipoEmpleo", DbHelper.OrDbNull(e.TipoEmpleo));
                command.Parameters.AddWithValue("@AntiguedadLaboralMeses", DbHelper.OrDbNull(e.AntiguedadLaboralMeses));
                command.Parameters.AddWithValue("@ResultadoGeneral", e.ResultadoGeneral.ToDbValue());
                command.Parameters.AddWithValue("@SibRawXml", DbHelper.OrDbNull(e.SibRawXml));
                command.Parameters.AddWithValue("@SibScoreBuro", DbHelper.OrDbNull(e.SibScoreBuro));
                command.Parameters.AddWithValue("@SibNivelEndeudamiento", DbHelper.OrDbNull(e.SibNivelEndeudamiento));
                command.Parameters.AddWithValue("@SibEnListaNegra", DbHelper.OrDbNull(e.SibEnListaNegra));

                var outputParam = new SqlParameter("@EvaluacionId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                command.Parameters.Add(outputParam);

                command.ExecuteNonQuery();
                return (int)outputParam.Value;
            }
        }

        private static void InsertarDetallePolitica(SqlConnection connection, SqlTransaction transaction, int evaluacionId, EvaluacionPolitica politica)
        {
            using (var command = new SqlCommand("dbo.sp_EvaluacionPoliticas_Insertar", connection, transaction))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@EvaluacionId", evaluacionId);
                command.Parameters.AddWithValue("@PoliticaId", DbHelper.OrDbNull(politica.PoliticaId));
                command.Parameters.AddWithValue("@NombrePolitica", politica.NombrePolitica);
                command.Parameters.AddWithValue("@Resultado", politica.Resultado.ToDbValue());
                command.Parameters.AddWithValue("@Detalle", DbHelper.OrDbNull(politica.Detalle));
                command.Parameters.AddWithValue("@Orden", politica.Orden);
                command.ExecuteNonQuery();
            }
        }

        public Evaluacion ObtenerPorId(int evaluacionId)
        {
            var evaluacion = DbHelper.ExecuteReaderSingle(
                "dbo.sp_Evaluaciones_ObtenerPorId",
                new[] { new SqlParameter("@EvaluacionId", evaluacionId) },
                MapEncabezado);

            if (evaluacion == null) return null;

            evaluacion.Politicas = DbHelper.ExecuteReader(
                "dbo.sp_EvaluacionPoliticas_ObtenerPorEvaluacion",
                new[] { new SqlParameter("@EvaluacionId", evaluacionId) },
                MapDetalle);

            return evaluacion;
        }

        public List<EvaluacionResumen> ObtenerRecientes(int top = 15)
        {
            return DbHelper.ExecuteReader(
                "dbo.sp_Evaluaciones_ObtenerRecientes",
                new[] { new SqlParameter("@Top", top) },
                r => new EvaluacionResumen
                {
                    EvaluacionId = r.GetInt32(r.GetOrdinal("EvaluacionId")),
                    ClienteNombre = r.GetString(r.GetOrdinal("ClienteNombre")),
                    ProductoNombre = r.GetString(r.GetOrdinal("ProductoNombre")),
                    ResultadoGeneral = ResultadoSemaforoExtensions.FromDbValue(r.GetString(r.GetOrdinal("ResultadoGeneral"))),
                    FechaCreacion = r.GetDateTime(r.GetOrdinal("FechaCreacion"))
                });
        }

        public ResumenDashboard ObtenerResumenDashboard()
        {
            var filas = DbHelper.ExecuteReader(
                "dbo.sp_Evaluaciones_ObtenerResumenDashboard",
                null,
                r => new ResumenDashboard
                {
                    Total = r.IsDBNull(r.GetOrdinal("TotalEvaluaciones")) ? 0 : r.GetInt32(r.GetOrdinal("TotalEvaluaciones")),
                    Verde = r.IsDBNull(r.GetOrdinal("TotalVerde")) ? 0 : r.GetInt32(r.GetOrdinal("TotalVerde")),
                    Amarillo = r.IsDBNull(r.GetOrdinal("TotalAmarillo")) ? 0 : r.GetInt32(r.GetOrdinal("TotalAmarillo")),
                    Rojo = r.IsDBNull(r.GetOrdinal("TotalRojo")) ? 0 : r.GetInt32(r.GetOrdinal("TotalRojo"))
                });

            return filas.Count > 0 ? filas[0] : new ResumenDashboard();
        }

        private static Evaluacion MapEncabezado(IDataRecord r)
        {
            return new Evaluacion
            {
                EvaluacionId = r.GetInt32(r.GetOrdinal("EvaluacionId")),
                ProductoId = r.GetInt32(r.GetOrdinal("ProductoId")),
                ProductoNombre = r.GetString(r.GetOrdinal("ProductoNombre")),
                UsuarioId = r.GetInt32(r.GetOrdinal("UsuarioId")),
                AnalistaNombre = r.GetString(r.GetOrdinal("AnalistaNombre")),
                PrimerNombre = r.GetString(r.GetOrdinal("PrimerNombre")),
                SegundoNombre = DbHelper.GetNullableString(r, "SegundoNombre"),
                TercerNombre = DbHelper.HasColumn(r, "TercerNombre") ? DbHelper.GetNullableString(r, "TercerNombre") : null,
                PrimerApellido = r.GetString(r.GetOrdinal("PrimerApellido")),
                SegundoApellido = DbHelper.GetNullableString(r, "SegundoApellido"),
                Nit = r.GetString(r.GetOrdinal("Nit")),
                Dpi = r.GetString(r.GetOrdinal("Dpi")),
                FechaNacimiento = r.GetDateTime(r.GetOrdinal("FechaNacimiento")),
                IngresosMensuales = DbHelper.GetNullable<decimal>(r, "IngresosMensuales"),
                TipoEmpleo = DbHelper.GetNullableString(r, "TipoEmpleo"),
                AntiguedadLaboralMeses = DbHelper.GetNullable<int>(r, "AntiguedadLaboralMeses"),
                ResultadoGeneral = ResultadoSemaforoExtensions.FromDbValue(r.GetString(r.GetOrdinal("ResultadoGeneral"))),
                Estado = r.GetString(r.GetOrdinal("Estado")),
                SibRawXml = DbHelper.GetNullableString(r, "SibRawXml"),
                SibScoreBuro = DbHelper.GetNullable<int>(r, "SibScoreBuro"),
                SibNivelEndeudamiento = DbHelper.GetNullable<decimal>(r, "SibNivelEndeudamiento"),
                SibEnListaNegra = DbHelper.GetNullable<bool>(r, "SibEnListaNegra"),
                FechaCreacion = r.GetDateTime(r.GetOrdinal("FechaCreacion"))
            };
        }

        private static EvaluacionPolitica MapDetalle(IDataRecord r)
        {
            return new EvaluacionPolitica
            {
                EvaluacionPoliticaId = r.GetInt32(r.GetOrdinal("EvaluacionPoliticaId")),
                EvaluacionId = r.GetInt32(r.GetOrdinal("EvaluacionId")),
                PoliticaId = DbHelper.GetNullable<int>(r, "PoliticaId"),
                NombrePolitica = r.GetString(r.GetOrdinal("NombrePolitica")),
                Resultado = ResultadoSemaforoExtensions.FromDbValue(r.GetString(r.GetOrdinal("Resultado"))),
                Detalle = DbHelper.GetNullableString(r, "Detalle"),
                Orden = r.GetInt32(r.GetOrdinal("Orden"))
            };
        }
    }
}
