using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.DAL
{
    public class RolRepository
    {
        public List<Rol> ObtenerTodosAdmin()
        {
            return DbHelper.ExecuteReader("dbo.sp_Roles_ObtenerTodosAdmin", null, MapAdmin);
        }

        public List<Rol> ObtenerActivos()
        {
            return DbHelper.ExecuteReader("dbo.sp_Roles_ObtenerActivos", null, Map);
        }

        public Rol ObtenerPorIdAdmin(int rolId)
        {
            return DbHelper.ExecuteReaderSingle(
                "dbo.sp_Roles_ObtenerPorIdAdmin",
                new[] { new SqlParameter("@RolId", rolId) },
                Map);
        }

        /// <summary>Cuántos usuarios activos tienen asignado este rol (para bloquear desactivaciones que dejarían usuarios huérfanos de permisos).</summary>
        public int ContarUsuariosActivos(int rolId)
        {
            using (var connection = DbHelper.GetConnection())
            using (var command = new SqlCommand("dbo.sp_Roles_ContarUsuariosActivos", connection) { CommandType = CommandType.StoredProcedure })
            {
                command.Parameters.AddWithValue("@RolId", rolId);
                connection.Open();
                object resultado = command.ExecuteScalar();
                return resultado == null || resultado == DBNull.Value ? 0 : Convert.ToInt32(resultado);
            }
        }

        /// <summary>Inserta o actualiza según <see cref="Rol.RolId"/> (0 = nuevo). Devuelve el Id resultante.</summary>
        public int Guardar(Rol rol)
        {
            return rol.RolId > 0 ? Actualizar(rol) : Crear(rol);
        }

        private static int Crear(Rol r)
        {
            using (var connection = DbHelper.GetConnection())
            using (var command = new SqlCommand("dbo.sp_Roles_Crear", connection) { CommandType = CommandType.StoredProcedure })
            {
                command.Parameters.AddWithValue("@Nombre", r.Nombre);
                command.Parameters.AddWithValue("@Descripcion", DbHelper.OrDbNull(r.Descripcion));
                command.Parameters.AddWithValue("@PuedeEvaluar", r.PuedeEvaluar);
                command.Parameters.AddWithValue("@PuedeAdministrarCatalogo", r.PuedeAdministrarCatalogo);
                command.Parameters.AddWithValue("@PuedeAdministrarUsuarios", r.PuedeAdministrarUsuarios);
                command.Parameters.AddWithValue("@Activo", r.Activo);

                var output = new SqlParameter("@RolId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                command.Parameters.Add(output);

                connection.Open();
                command.ExecuteNonQuery();
                return (int)output.Value;
            }
        }

        private static int Actualizar(Rol r)
        {
            DbHelper.ExecuteNonQuery("dbo.sp_Roles_Actualizar", new[]
            {
                new SqlParameter("@RolId", r.RolId),
                new SqlParameter("@Nombre", r.Nombre),
                new SqlParameter("@Descripcion", DbHelper.OrDbNull(r.Descripcion)),
                new SqlParameter("@PuedeEvaluar", r.PuedeEvaluar),
                new SqlParameter("@PuedeAdministrarCatalogo", r.PuedeAdministrarCatalogo),
                new SqlParameter("@PuedeAdministrarUsuarios", r.PuedeAdministrarUsuarios),
                new SqlParameter("@Activo", r.Activo)
            });
            return r.RolId;
        }

        private static Rol Map(IDataRecord r)
        {
            return new Rol
            {
                RolId = r.GetInt32(r.GetOrdinal("RolId")),
                Nombre = r.GetString(r.GetOrdinal("Nombre")),
                Descripcion = DbHelper.GetNullableString(r, "Descripcion"),
                PuedeEvaluar = r.GetBoolean(r.GetOrdinal("PuedeEvaluar")),
                PuedeAdministrarCatalogo = r.GetBoolean(r.GetOrdinal("PuedeAdministrarCatalogo")),
                PuedeAdministrarUsuarios = r.GetBoolean(r.GetOrdinal("PuedeAdministrarUsuarios")),
                Activo = r.GetBoolean(r.GetOrdinal("Activo"))
            };
        }

        private static Rol MapAdmin(IDataRecord r)
        {
            Rol rol = Map(r);
            rol.TotalUsuariosActivos = r.GetInt32(r.GetOrdinal("TotalUsuariosActivos"));
            return rol;
        }
    }
}
