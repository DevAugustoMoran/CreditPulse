using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using CreditPulse.Web.Models;
using Newtonsoft.Json.Linq;

namespace CreditPulse.Web.Security
{
    /// <summary>Parámetros de los tokens (appSettings del Web.config: JwtSecret, JwtIssuer, JwtAudience, JwtAccessMinutes, JwtRefreshDays).</summary>
    public class JwtSettings
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int AccessMinutes { get; set; }
        public int RefreshDays { get; set; }

        /// <summary>Un secreto de menos de 32 caracteres no es aceptable para HS256.</summary>
        public bool EstaConfigurado
        {
            get { return !string.IsNullOrEmpty(Secret) && Secret.Length >= 32; }
        }

        public static JwtSettings Actual
        {
            get
            {
                int minutos, dias;
                return new JwtSettings
                {
                    Secret = ConfigurationManager.AppSettings["JwtSecret"],
                    Issuer = ConfigurationManager.AppSettings["JwtIssuer"] ?? "cp-analitica",
                    Audience = ConfigurationManager.AppSettings["JwtAudience"] ?? "cp-analitica-api",
                    AccessMinutes = int.TryParse(ConfigurationManager.AppSettings["JwtAccessMinutes"], out minutos) && minutos > 0 ? minutos : 15,
                    RefreshDays = int.TryParse(ConfigurationManager.AppSettings["JwtRefreshDays"], out dias) && dias > 0 ? dias : 7
                };
            }
        }
    }

    /// <summary>Lo que se lee de un token de acceso ya verificado.</summary>
    public class JwtClaims
    {
        public int UsuarioId { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Rol { get; set; }
        public bool PuedeEvaluar { get; set; }
        public bool PuedeAdministrarCatalogo { get; set; }
        public bool PuedeAdministrarUsuarios { get; set; }
        public DateTime ExpiraUtc { get; set; }
        public string Id { get; set; }
    }

    /// <summary>
    /// JWT firmado con HMAC-SHA256 (HS256), implementado a mano con las
    /// clases del propio .NET Framework para no agregar paquetes NuGet al
    /// proyecto. Formato estándar: base64url(header).base64url(payload).base64url(firma).
    ///
    /// Un token de acceso es de vida corta (15 min por defecto) y NO se
    /// guarda en ninguna parte: su validez sale de la firma y de "exp".
    /// Para renovarlo sin volver a pedir la contraseña existe el refresh
    /// token (ver BLL.TokenService), que sí se guarda -solo su hash- y
    /// puede revocarse.
    /// </summary>
    public static class JwtHelper
    {
        private static readonly DateTime Epoca = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const int HolguraSegundos = 30;

        public static string Crear(JwtSettings cfg, Usuario usuario, DateTime ahoraUtc, out DateTime expiraUtc)
        {
            if (!cfg.EstaConfigurado)
                throw new InvalidOperationException("El appSetting \"JwtSecret\" falta o tiene menos de 32 caracteres.");

            expiraUtc = ahoraUtc.AddMinutes(cfg.AccessMinutes);

            var header = new JObject { { "alg", "HS256" }, { "typ", "JWT" } };
            var payload = new JObject
            {
                { "iss", cfg.Issuer },
                { "aud", cfg.Audience },
                { "sub", usuario.UsuarioId.ToString() },
                { "jti", Guid.NewGuid().ToString("N") },
                { "iat", SegundosUnix(ahoraUtc) },
                { "exp", SegundosUnix(expiraUtc) },
                { "email", usuario.Email },
                { "name", usuario.NombreCompleto },
                { "role", usuario.RolNombre },
                { "perm_eval", usuario.PuedeEvaluar },
                { "perm_cat", usuario.PuedeAdministrarCatalogo },
                { "perm_usr", usuario.PuedeAdministrarUsuarios }
            };

            string firmado = Base64Url(Encoding.UTF8.GetBytes(header.ToString(Newtonsoft.Json.Formatting.None)))
                           + "." + Base64Url(Encoding.UTF8.GetBytes(payload.ToString(Newtonsoft.Json.Formatting.None)));

            return firmado + "." + Base64Url(Firmar(cfg.Secret, firmado));
        }

        /// <summary>Verifica firma, algoritmo, emisor, audiencia y vencimiento. Devuelve null si el token no es válido por cualquier motivo.</summary>
        public static JwtClaims Validar(JwtSettings cfg, string token, DateTime ahoraUtc)
        {
            if (!cfg.EstaConfigurado || string.IsNullOrWhiteSpace(token)) return null;

            string[] partes = token.Trim().Split('.');
            if (partes.Length != 3) return null;

            try
            {
                JObject header = JObject.Parse(Encoding.UTF8.GetString(DesdeBase64Url(partes[0])));

                // Solo se acepta HS256: rechaza "none" y cualquier otro algoritmo (ataque clásico de confusión de algoritmo).
                if ((string)header["alg"] != "HS256") return null;

                byte[] firmaEsperada = Firmar(cfg.Secret, partes[0] + "." + partes[1]);
                byte[] firmaRecibida = DesdeBase64Url(partes[2]);
                if (!IgualesEnTiempoConstante(firmaEsperada, firmaRecibida)) return null;

                JObject payload = JObject.Parse(Encoding.UTF8.GetString(DesdeBase64Url(partes[1])));

                if ((string)payload["iss"] != cfg.Issuer) return null;
                if ((string)payload["aud"] != cfg.Audience) return null;

                long exp = (long)payload["exp"];
                DateTime expira = Epoca.AddSeconds(exp);
                if (expira.AddSeconds(HolguraSegundos) < ahoraUtc) return null;

                int usuarioId;
                if (!int.TryParse((string)payload["sub"], out usuarioId)) return null;

                return new JwtClaims
                {
                    UsuarioId = usuarioId,
                    Email = (string)payload["email"],
                    Nombre = (string)payload["name"],
                    Rol = (string)payload["role"],
                    PuedeEvaluar = payload["perm_eval"] != null && (bool)payload["perm_eval"],
                    PuedeAdministrarCatalogo = payload["perm_cat"] != null && (bool)payload["perm_cat"],
                    PuedeAdministrarUsuarios = payload["perm_usr"] != null && (bool)payload["perm_usr"],
                    ExpiraUtc = expira,
                    Id = (string)payload["jti"]
                };
            }
            catch (Exception ex) when (ex is FormatException || ex is Newtonsoft.Json.JsonException || ex is InvalidCastException || ex is ArgumentException)
            {
                return null;
            }
        }

        private static long SegundosUnix(DateTime utc)
        {
            return (long)(DateTime.SpecifyKind(utc, DateTimeKind.Utc) - Epoca).TotalSeconds;
        }

        private static byte[] Firmar(string secreto, string texto)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secreto)))
            {
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(texto));
            }
        }

        public static string Base64Url(byte[] datos)
        {
            return Convert.ToBase64String(datos).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static byte[] DesdeBase64Url(string texto)
        {
            string s = texto.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
                case 1: throw new FormatException("Base64url inválido.");
            }
            return Convert.FromBase64String(s);
        }

        private static bool IgualesEnTiempoConstante(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;

            int diferencia = 0;
            for (int i = 0; i < a.Length; i++) diferencia |= a[i] ^ b[i];
            return diferencia == 0;
        }
    }
}
