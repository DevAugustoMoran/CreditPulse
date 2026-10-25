using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.DAL
{
    public class ProductoRepository
    {
        public List<Producto> ObtenerActivos()
        {
            return DbHelper.ExecuteReader("dbo.sp_Productos_ObtenerActivos", null, Map);
        }

        public Producto ObtenerPorId(int productoId)
        {
            return DbHelper.ExecuteReaderSingle(
                "dbo.sp_Productos_ObtenerPorId",
                new[] { new SqlParameter("@ProductoId", productoId) },
                Map);
        }

        /// <summary>Usado por Evaluaciones Batch para ubicar el producto "General" (código GENERAL).</summary>
        public Producto ObtenerPorCodigo(string codigo)
        {
            return DbHelper.ExecuteReaderSingle(
                "dbo.sp_Productos_ObtenerPorCodigo",
                new[] { new SqlParameter("@Codigo", codigo) },
                Map);
        }

        // ---------- Administración (incluye productos inactivos) ----------

        public List<Producto> ObtenerTodosAdmin()
        {
            return DbHelper.ExecuteReader("dbo.sp_Productos_ObtenerTodosAdmin", null, MapAdmin);
        }

        public Producto ObtenerPorIdAdmin(int productoId)
        {
            return DbHelper.ExecuteReaderSingle(
                "dbo.sp_Productos_ObtenerPorIdAdmin",
                new[] { new SqlParameter("@ProductoId", productoId) },
                MapAdmin);
        }

        /// <summary>Inserta o actualiza según <see cref="Producto.ProductoId"/> (0 = nuevo). Devuelve el Id resultante.</summary>
        public int Guardar(Producto producto)
        {
            return producto.ProductoId > 0 ? Actualizar(producto) : Crear(producto);
        }

        private static int Crear(Producto p)
        {
            using (var connection = DbHelper.GetConnection())
            using (var command = new SqlCommand("dbo.sp_Productos_Crear", connection) { CommandType = CommandType.StoredProcedure })
            {
                command.Parameters.AddWithValue("@Codigo", p.Codigo);
                command.Parameters.AddWithValue("@Nombre", p.Nombre);
                command.Parameters.AddWithValue("@Descripcion", DbHelper.OrDbNull(p.Descripcion));
                command.Parameters.AddWithValue("@RequiereIngresos", p.RequiereIngresos);
                command.Parameters.AddWithValue("@RequiereTipoEmpleo", p.RequiereTipoEmpleo);
                command.Parameters.AddWithValue("@RequiereAntiguedadLaboral", p.RequiereAntiguedadLaboral);
                command.Parameters.AddWithValue("@Activo", p.Activo);

                var output = new SqlParameter("@ProductoId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                command.Parameters.Add(output);

                connection.Open();
                command.ExecuteNonQuery();
                return (int)output.Value;
            }
        }

        private static int Actualizar(Producto p)
        {
            DbHelper.ExecuteNonQuery("dbo.sp_Productos_Actualizar", new[]
            {
                new SqlParameter("@ProductoId", p.ProductoId),
                new SqlParameter("@Codigo", p.Codigo),
                new SqlParameter("@Nombre", p.Nombre),
                new SqlParameter("@Descripcion", DbHelper.OrDbNull(p.Descripcion)),
                new SqlParameter("@RequiereIngresos", p.RequiereIngresos),
                new SqlParameter("@RequiereTipoEmpleo", p.RequiereTipoEmpleo),
                new SqlParameter("@RequiereAntiguedadLaboral", p.RequiereAntiguedadLaboral),
                new SqlParameter("@Activo", p.Activo)
            });
            return p.ProductoId;
        }

        private static Producto Map(IDataRecord r)
        {
            return new Producto
            {
                ProductoId = r.GetInt32(r.GetOrdinal("ProductoId")),
                Codigo = r.GetString(r.GetOrdinal("Codigo")),
                Nombre = r.GetString(r.GetOrdinal("Nombre")),
                Descripcion = DbHelper.GetNullableString(r, "Descripcion"),
                RequiereIngresos = r.GetBoolean(r.GetOrdinal("RequiereIngresos")),
                RequiereTipoEmpleo = r.GetBoolean(r.GetOrdinal("RequiereTipoEmpleo")),
                RequiereAntiguedadLaboral = r.GetBoolean(r.GetOrdinal("RequiereAntiguedadLaboral"))
            };
        }

        private static Producto MapAdmin(IDataRecord r)
        {
            Producto p = Map(r);
            p.Activo = r.GetBoolean(r.GetOrdinal("Activo"));

            int totalOrdinal;
            try { totalOrdinal = r.GetOrdinal("TotalPoliticas"); }
            catch (IndexOutOfRangeException) { totalOrdinal = -1; }
            if (totalOrdinal >= 0) p.TotalPoliticas = r.GetInt32(totalOrdinal);

            return p;
        }
    }
}
