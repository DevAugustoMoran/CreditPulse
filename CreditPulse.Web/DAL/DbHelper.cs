using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace CreditPulse.Web.DAL
{
    /// <summary>
    /// Helper delgado sobre ADO.NET. Todas las operaciones de la aplicación
    /// pasan por procedimientos almacenados (ver Database/03_StoredProcedures.sql)
    /// en lugar de SQL dinámico embebido, tal como lo exige el RNF03
    /// (Arquitectura de Datos Optimizada) del levantamiento de requerimientos.
    /// </summary>
    internal static class DbHelper
    {
        private static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["CreditPulseDB"].ConnectionString; }
        }

        internal static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        /// <summary>Ejecuta un procedimiento almacenado y mapea cada fila con <paramref name="map"/>.</summary>
        internal static List<T> ExecuteReader<T>(string storedProcedure, IEnumerable<SqlParameter> parameters, Func<IDataRecord, T> map)
        {
            var results = new List<T>();

            using (var connection = GetConnection())
            using (var command = new SqlCommand(storedProcedure, connection) { CommandType = CommandType.StoredProcedure })
            {
                if (parameters != null)
                {
                    foreach (var p in parameters) command.Parameters.Add(p);
                }

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(map(reader));
                    }
                }
            }

            return results;
        }

        /// <summary>Ejecuta un procedimiento almacenado que retorna una sola fila (o ninguna).</summary>
        internal static T ExecuteReaderSingle<T>(string storedProcedure, IEnumerable<SqlParameter> parameters, Func<IDataRecord, T> map) where T : class
        {
            var list = ExecuteReader(storedProcedure, parameters, map);
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>Ejecuta un procedimiento almacenado sin resultset (INSERT/UPDATE/DELETE).</summary>
        internal static int ExecuteNonQuery(string storedProcedure, IEnumerable<SqlParameter> parameters)
        {
            using (var connection = GetConnection())
            using (var command = new SqlCommand(storedProcedure, connection) { CommandType = CommandType.StoredProcedure })
            {
                if (parameters != null)
                {
                    foreach (var p in parameters) command.Parameters.Add(p);
                }

                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Ejecuta un procedimiento almacenado y devuelve el valor de un parámetro
        /// de salida (usado por sp_Evaluaciones_Crear para recuperar el nuevo Id).
        /// </summary>
        internal static int ExecuteNonQueryWithOutput(string storedProcedure, IEnumerable<SqlParameter> parameters, string outputParameterName)
        {
            using (var connection = GetConnection())
            using (var command = new SqlCommand(storedProcedure, connection) { CommandType = CommandType.StoredProcedure })
            {
                SqlParameter outputParam = null;

                if (parameters != null)
                {
                    foreach (var p in parameters)
                    {
                        command.Parameters.Add(p);
                        if (string.Equals(p.ParameterName, outputParameterName, StringComparison.OrdinalIgnoreCase))
                        {
                            outputParam = p;
                        }
                    }
                }

                connection.Open();
                command.ExecuteNonQuery();

                return outputParam != null && outputParam.Value != DBNull.Value
                    ? Convert.ToInt32(outputParam.Value)
                    : 0;
            }
        }

        internal static object OrDbNull(object value)
        {
            return value ?? DBNull.Value;
        }

        internal static T? GetNullable<T>(IDataRecord record, string columnName) where T : struct
        {
            int ordinal = record.GetOrdinal(columnName);
            return record.IsDBNull(ordinal) ? (T?)null : (T)Convert.ChangeType(record.GetValue(ordinal), typeof(T));
        }

        internal static bool HasColumn(IDataRecord record, string columnName)
        {
            for (int i = 0; i < record.FieldCount; i++)
            {
                if (string.Equals(record.GetName(i), columnName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        internal static string GetNullableString(IDataRecord record, string columnName)
        {
            int ordinal = record.GetOrdinal(columnName);
            return record.IsDBNull(ordinal) ? null : record.GetString(ordinal);
        }
    }
}
