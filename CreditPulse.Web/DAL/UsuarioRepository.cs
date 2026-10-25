using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.DAL
{
    public class UsuarioRepository
    {
        public Usuario ObtenerPorEmail(string email)
        {
            return DbHelper.ExecuteReaderSingle(
                "dbo.sp_Usuarios_ObtenerPorEmail",
                new[] { new SqlParameter("@Email", email) },
                MapConPermisos);
        }

        /// <summary>Usuario con los permisos de su rol, buscado por Id (lo usa la autenticación por token; requiere Database/14_AuthTokens.sql).</summary>
        public Usuario ObtenerPorIdConPermisos(int usuarioId)
        {
            return DbHelper.ExecuteReaderSingle(
                "dbo.sp_Usuarios_ObtenerPorId",
                new[] { new SqlParameter("@UsuarioId", usuarioId) },
                MapConPermisos);
        }

        public void RegistrarLoginExitoso(int usuarioId)
        {
            DbHelper.ExecuteNonQuery(
                "dbo.sp_Usuarios_RegistrarLoginExitoso",
                new[] { new SqlParameter("@UsuarioId", usuarioId) });
        }

        public void RegistrarLoginFallido(string email)
        {
            DbHelper.ExecuteNonQuery(
                "dbo.sp_Usuarios_RegistrarLoginFallido",
                new[] { new SqlParameter("@Email", email) });
        }

        // ---------- Administración ----------

        public List<Usuario> ObtenerTodosAdmin()
        {
            return DbHelper.ExecuteReader("dbo.sp_Usuarios_ObtenerTodosAdmin", null, MapAdmin);
        }

        public Usuario ObtenerPorIdAdmin(int usuarioId)
        {
            return DbHelper.ExecuteReaderSingle(
                "dbo.sp_Usuarios_ObtenerPorIdAdmin",
                new[] { new SqlParameter("@UsuarioId", usuarioId) },
                Map);
        }

        /// <summary>Crea un usuario nuevo con contraseña ya calculada (ver Security.PasswordHasher). Devuelve el Id generado.</summary>
        public int Crear(Usuario u)
        {
            using (var connection = DbHelper.GetConnection())
            using (var command = new SqlCommand("dbo.sp_Usuarios_Crear", connection) { CommandType = CommandType.StoredProcedure })
            {
                command.Parameters.AddWithValue("@Email", u.Email);
                command.Parameters.AddWithValue("@PasswordHash", u.PasswordHash);
                command.Parameters.AddWithValue("@PasswordSalt", u.PasswordSalt);
                command.Parameters.AddWithValue("@NombreCompleto", u.NombreCompleto);
                command.Parameters.AddWithValue("@RolId", u.RolId);
                command.Parameters.AddWithValue("@Activo", u.Activo);

                var output = new SqlParameter("@UsuarioId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                command.Parameters.Add(output);

                connection.Open();
                command.ExecuteNonQuery();
                return (int)output.Value;
            }
        }

        /// <summary>Actualiza los datos del usuario sin tocar la contraseña.</summary>
        public void Actualizar(Usuario u)
        {
            DbHelper.ExecuteNonQuery("dbo.sp_Usuarios_Actualizar", new[]
            {
                new SqlParameter("@UsuarioId", u.UsuarioId),
                new SqlParameter("@Email", u.Email),
                new SqlParameter("@NombreCompleto", u.NombreCompleto),
                new SqlParameter("@RolId", u.RolId),
                new SqlParameter("@Activo", u.Activo)
            });
        }

        /// <summary>Resetea la contraseña de un usuario existente (y, de paso, sus intentos fallidos).</summary>
        public void ActualizarPassword(int usuarioId, string passwordHash, string passwordSalt)
        {
            DbHelper.ExecuteNonQuery("dbo.sp_Usuarios_ActualizarPassword", new[]
            {
                new SqlParameter("@UsuarioId", usuarioId),
                new SqlParameter("@PasswordHash", passwordHash),
                new SqlParameter("@PasswordSalt", passwordSalt)
            });
        }

        public void CambiarEstado(int usuarioId, bool activo)
        {
            DbHelper.ExecuteNonQuery("dbo.sp_Usuarios_CambiarEstado", new[]
            {
                new SqlParameter("@UsuarioId", usuarioId),
                new SqlParameter("@Activo", activo)
            });
        }

        private static Usuario Map(IDataRecord r)
        {
            return new Usuario
            {
                UsuarioId = r.GetInt32(r.GetOrdinal("UsuarioId")),
                Email = r.GetString(r.GetOrdinal("Email")),
                NombreCompleto = r.GetString(r.GetOrdinal("NombreCompleto")),
                RolId = r.GetInt32(r.GetOrdinal("RolId")),
                Activo = r.GetBoolean(r.GetOrdinal("Activo"))
            };
        }

        private static Usuario MapAdmin(IDataRecord r)
        {
            Usuario u = Map(r);
            u.RolNombre = r.GetString(r.GetOrdinal("RolNombre"));
            u.IntentosFallidos = r.GetInt32(r.GetOrdinal("IntentosFallidos"));
            u.FechaCreacion = r.GetDateTime(r.GetOrdinal("FechaCreacion"));

            int ordinalUltimoLogin = r.GetOrdinal("UltimoLogin");
            u.UltimoLogin = r.IsDBNull(ordinalUltimoLogin) ? (DateTime?)null : r.GetDateTime(ordinalUltimoLogin);

            return u;
        }

        /// <summary>Usado solo por el login: incluye hash/salt y los permisos del rol.</summary>
        private static Usuario MapConPermisos(IDataRecord r)
        {
            return new Usuario
            {
                UsuarioId = r.GetInt32(r.GetOrdinal("UsuarioId")),
                Email = r.GetString(r.GetOrdinal("Email")),
                PasswordHash = r.GetString(r.GetOrdinal("PasswordHash")),
                PasswordSalt = r.GetString(r.GetOrdinal("PasswordSalt")),
                NombreCompleto = r.GetString(r.GetOrdinal("NombreCompleto")),
                RolId = r.GetInt32(r.GetOrdinal("RolId")),
                RolNombre = r.GetString(r.GetOrdinal("RolNombre")),
                Activo = r.GetBoolean(r.GetOrdinal("Activo")),
                IntentosFallidos = r.GetInt32(r.GetOrdinal("IntentosFallidos")),
                PuedeEvaluar = r.GetBoolean(r.GetOrdinal("PuedeEvaluar")),
                PuedeAdministrarCatalogo = r.GetBoolean(r.GetOrdinal("PuedeAdministrarCatalogo")),
                PuedeAdministrarUsuarios = r.GetBoolean(r.GetOrdinal("PuedeAdministrarUsuarios"))
            };
        }
    }
}
