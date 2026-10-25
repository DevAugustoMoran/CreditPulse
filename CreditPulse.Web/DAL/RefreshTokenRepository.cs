using System;
using System.Data;
using System.Data.SqlClient;

namespace CreditPulse.Web.DAL
{
    /// <summary>Un refresh token guardado (solo su hash; ver Database/14_AuthTokens.sql).</summary>
    public class RefreshTokenRegistro
    {
        public long RefreshTokenId { get; set; }
        public int UsuarioId { get; set; }
        public string TokenHash { get; set; }
        public DateTime FechaExpira { get; set; }
        public DateTime? FechaRevocado { get; set; }
        public string ReemplazadoPorHash { get; set; }
    }

    public class RefreshTokenRepository
    {
        public void Crear(int usuarioId, string tokenHash, DateTime expiraUtc, string ip)
        {
            DbHelper.ExecuteNonQuery("dbo.sp_RefreshTokens_Crear", new[]
            {
                new SqlParameter("@UsuarioId", usuarioId),
                new SqlParameter("@TokenHash", SqlDbType.Char, 64) { Value = tokenHash },
                new SqlParameter("@FechaExpira", SqlDbType.DateTime2) { Value = expiraUtc },
                new SqlParameter("@IpCreacion", SqlDbType.NVarChar, 45) { Value = DbHelper.OrDbNull(ip) }
            });
        }

        public RefreshTokenRegistro ObtenerPorHash(string tokenHash)
        {
            return DbHelper.ExecuteReaderSingle(
                "dbo.sp_RefreshTokens_ObtenerPorHash",
                new[] { new SqlParameter("@TokenHash", SqlDbType.Char, 64) { Value = tokenHash } },
                r => new RefreshTokenRegistro
                {
                    RefreshTokenId = r.GetInt64(r.GetOrdinal("RefreshTokenId")),
                    UsuarioId = r.GetInt32(r.GetOrdinal("UsuarioId")),
                    TokenHash = r.GetString(r.GetOrdinal("TokenHash")),
                    FechaExpira = r.GetDateTime(r.GetOrdinal("FechaExpira")),
                    FechaRevocado = DbHelper.GetNullable<DateTime>(r, "FechaRevocado"),
                    ReemplazadoPorHash = DbHelper.GetNullableString(r, "ReemplazadoPorHash")
                });
        }

        /// <summary>Marca el token como usado/revocado (opcionalmente enlazándolo con el que lo reemplaza).</summary>
        public void Revocar(string tokenHash, string reemplazadoPorHash)
        {
            DbHelper.ExecuteReader("dbo.sp_RefreshTokens_Revocar", new[]
            {
                new SqlParameter("@TokenHash", SqlDbType.Char, 64) { Value = tokenHash },
                new SqlParameter("@ReemplazadoPorHash", SqlDbType.Char, 64) { Value = DbHelper.OrDbNull(reemplazadoPorHash) }
            }, r => 0);
        }

        public void RevocarTodosDeUsuario(int usuarioId)
        {
            DbHelper.ExecuteNonQuery("dbo.sp_RefreshTokens_RevocarTodosDeUsuario", new[] { new SqlParameter("@UsuarioId", usuarioId) });
        }
    }
}
