using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.DAL
{
    public class PoliticaRepository
    {
        public List<Politica> ObtenerPorProducto(int productoId)
        {
            return DbHelper.ExecuteReader(
                "dbo.sp_Politicas_ObtenerPorProducto",
                new[] { new SqlParameter("@ProductoId", productoId) },
                Map);
        }

        // ---------- Administración (incluye políticas inactivas) ----------

        public List<Politica> ObtenerTodosPorProducto(int productoId)
        {
            return DbHelper.ExecuteReader(
                "dbo.sp_Politicas_ObtenerTodosPorProducto",
                new[] { new SqlParameter("@ProductoId", productoId) },
                MapAdmin);
        }

        public Politica ObtenerPorIdAdmin(int politicaId)
        {
            return DbHelper.ExecuteReaderSingle(
                "dbo.sp_Politicas_ObtenerPorIdAdmin",
                new[] { new SqlParameter("@PoliticaId", politicaId) },
                MapAdmin);
        }

        /// <summary>Inserta o actualiza según <see cref="Politica.PoliticaId"/> (0 = nuevo). Devuelve el Id resultante.</summary>
        public int Guardar(Politica politica)
        {
            return politica.PoliticaId > 0 ? Actualizar(politica) : Crear(politica);
        }

        private static int Crear(Politica p)
        {
            using (var connection = DbHelper.GetConnection())
            using (var command = new SqlCommand("dbo.sp_Politicas_Crear", connection) { CommandType = CommandType.StoredProcedure })
            {
                command.Parameters.AddWithValue("@ProductoId", p.ProductoId);
                command.Parameters.AddWithValue("@Codigo", p.Codigo);
                command.Parameters.AddWithValue("@Nombre", p.Nombre);
                command.Parameters.AddWithValue("@Descripcion", DbHelper.OrDbNull(p.Descripcion));
                command.Parameters.AddWithValue("@TipoRegla", p.TipoRegla);
                command.Parameters.AddWithValue("@ParametrosJson", p.ParametrosJson);
                command.Parameters.AddWithValue("@Orden", p.Orden);
                command.Parameters.AddWithValue("@Activo", p.Activo);

                var output = new SqlParameter("@PoliticaId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                command.Parameters.Add(output);

                connection.Open();
                command.ExecuteNonQuery();
                return (int)output.Value;
            }
        }

        private static int Actualizar(Politica p)
        {
            DbHelper.ExecuteNonQuery("dbo.sp_Politicas_Actualizar", new[]
            {
                new SqlParameter("@PoliticaId", p.PoliticaId),
                new SqlParameter("@Codigo", p.Codigo),
                new SqlParameter("@Nombre", p.Nombre),
                new SqlParameter("@Descripcion", DbHelper.OrDbNull(p.Descripcion)),
                new SqlParameter("@TipoRegla", p.TipoRegla),
                new SqlParameter("@ParametrosJson", p.ParametrosJson),
                new SqlParameter("@Orden", p.Orden),
                new SqlParameter("@Activo", p.Activo)
            });
            return p.PoliticaId;
        }

        private static Politica Map(IDataRecord r)
        {
            return new Politica
            {
                PoliticaId = r.GetInt32(r.GetOrdinal("PoliticaId")),
                ProductoId = r.GetInt32(r.GetOrdinal("ProductoId")),
                Codigo = r.GetString(r.GetOrdinal("Codigo")),
                Nombre = r.GetString(r.GetOrdinal("Nombre")),
                Descripcion = DbHelper.GetNullableString(r, "Descripcion"),
                TipoRegla = r.GetString(r.GetOrdinal("TipoRegla")),
                ParametrosJson = r.GetString(r.GetOrdinal("ParametrosJson")),
                Orden = r.GetInt32(r.GetOrdinal("Orden"))
            };
        }

        private static Politica MapAdmin(IDataRecord r)
        {
            Politica p = Map(r);
            p.Activo = r.GetBoolean(r.GetOrdinal("Activo"));
            return p;
        }
    }
}
