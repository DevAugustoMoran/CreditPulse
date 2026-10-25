using System;
using System.Security.Cryptography;
using System.Text;
using CreditPulse.Web.DAL;
using CreditPulse.Web.Models;
using CreditPulse.Web.Security;

namespace CreditPulse.Web.BLL
{
    /// <summary>Resultado de emitir o renovar tokens: el par de tokens, o el motivo del rechazo con su código HTTP sugerido.</summary>
    public class TokenResult
    {
        public bool Exito { get; set; }
        public int CodigoHttp { get; set; }
        public string Mensaje { get; set; }

        public string AccessToken { get; set; }
        public DateTime AccessExpiraUtc { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshExpiraUtc { get; set; }
        public Usuario Usuario { get; set; }

        public static TokenResult Fallo(int codigoHttp, string mensaje)
        {
            return new TokenResult { Exito = false, CodigoHttp = codigoHttp, Mensaje = mensaje };
        }
    }

    /// <summary>
    /// Autenticación por token para las APIs: token de acceso (JWT de
    /// 15 minutos) + refresh token (7 días, de un solo uso).
    ///
    ///   Emitir   : correo + contraseña (mismas cuentas, y mismo bloqueo por
    ///              5 intentos fallidos, que el login web) → par de tokens.
    ///   Renovar  : refresh token → NUEVO par; el refresh token usado queda
    ///              revocado (rotación). Si alguien presenta un refresh token
    ///              que ya se usó, se asume que fue robado y se revocan todos
    ///              los refresh tokens de ese usuario.
    ///   Revocar  : cierra la sesión de API (invalida un refresh token).
    ///
    /// Del refresh token solo se guarda su hash SHA-256 (ver
    /// Database/14_AuthTokens.sql): ni leyendo la base de datos se pueden usar.
    /// </summary>
    public class TokenService
    {
        private readonly AuthService _authService;
        private readonly UsuarioRepository _usuarioRepository;
        private readonly RefreshTokenRepository _refreshRepository;
        private readonly JwtSettings _cfg;

        public TokenService()
            : this(new AuthService(), new UsuarioRepository(), new RefreshTokenRepository(), JwtSettings.Actual)
        {
        }

        public TokenService(AuthService authService, UsuarioRepository usuarioRepository, RefreshTokenRepository refreshRepository, JwtSettings cfg)
        {
            _authService = authService;
            _usuarioRepository = usuarioRepository;
            _refreshRepository = refreshRepository;
            _cfg = cfg;
        }

        public bool EstaConfigurado { get { return _cfg.EstaConfigurado; } }

        public TokenResult Emitir(string email, string password, string ip)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
                return TokenResult.Fallo(400, "Se requieren \"email\" y \"password\".");

            LoginResult login = _authService.IniciarSesion(email, password);

            switch (login.Status)
            {
                case LoginStatus.Exitoso:
                    return CrearPar(login.Usuario, ip);
                case LoginStatus.CredencialesInvalidas:
                    return TokenResult.Fallo(401, login.MensajeError);
                default:
                    // Usuario inactivo o cuenta bloqueada: credenciales válidas o no, no puede entrar.
                    return TokenResult.Fallo(403, login.MensajeError);
            }
        }

        public TokenResult Renovar(string refreshToken, string ip)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return TokenResult.Fallo(400, "Se requiere \"refreshToken\".");

            string hash = Hash(refreshToken.Trim());
            RefreshTokenRegistro registro = _refreshRepository.ObtenerPorHash(hash);

            if (registro == null)
                return TokenResult.Fallo(401, "Refresh token inválido.");

            if (registro.FechaRevocado.HasValue)
            {
                // Un refresh token es de un solo uso: si vuelve a aparecer, alguien tiene una copia.
                // Se cierran todas las sesiones de API de ese usuario por seguridad.
                _refreshRepository.RevocarTodosDeUsuario(registro.UsuarioId);
                return TokenResult.Fallo(401, "El refresh token ya fue utilizado o revocado. Inicia sesión de nuevo.");
            }

            if (DateTime.SpecifyKind(registro.FechaExpira, DateTimeKind.Utc) <= DateTime.UtcNow)
                return TokenResult.Fallo(401, "El refresh token expiró. Inicia sesión de nuevo.");

            Usuario usuario = _usuarioRepository.ObtenerPorIdConPermisos(registro.UsuarioId);
            if (usuario == null || !usuario.Activo)
            {
                _refreshRepository.RevocarTodosDeUsuario(registro.UsuarioId);
                return TokenResult.Fallo(403, "El usuario está inactivo.");
            }

            TokenResult nuevo = CrearPar(usuario, ip);
            _refreshRepository.Revocar(hash, Hash(nuevo.RefreshToken));
            return nuevo;
        }

        public void Revocar(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return;
            _refreshRepository.Revocar(Hash(refreshToken.Trim()), null);
        }

        /// <summary>
        /// Valida un token de acceso: firma y vencimiento (JwtHelper) y que el
        /// usuario siga existiendo y activo. Devuelve null si no es válido.
        /// </summary>
        public Usuario ValidarAccessToken(string accessToken)
        {
            JwtClaims claims = JwtHelper.Validar(_cfg, accessToken, DateTime.UtcNow);
            if (claims == null) return null;

            Usuario usuario = _usuarioRepository.ObtenerPorIdConPermisos(claims.UsuarioId);
            return usuario != null && usuario.Activo ? usuario : null;
        }

        private TokenResult CrearPar(Usuario usuario, string ip)
        {
            DateTime ahora = DateTime.UtcNow;

            DateTime accessExpira;
            string access = JwtHelper.Crear(_cfg, usuario, ahora, out accessExpira);

            string refresh = GenerarRefreshToken();
            DateTime refreshExpira = ahora.AddDays(_cfg.RefreshDays);
            _refreshRepository.Crear(usuario.UsuarioId, Hash(refresh), refreshExpira, ip);

            return new TokenResult
            {
                Exito = true,
                CodigoHttp = 200,
                AccessToken = access,
                AccessExpiraUtc = accessExpira,
                RefreshToken = refresh,
                RefreshExpiraUtc = refreshExpira,
                Usuario = usuario
            };
        }

        /// <summary>64 bytes aleatorios criptográficamente seguros, en base64url.</summary>
        private static string GenerarRefreshToken()
        {
            var bytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return JwtHelper.Base64Url(bytes);
        }

        /// <summary>SHA-256 en hexadecimal (64 caracteres), lo único que se guarda del refresh token.</summary>
        public static string Hash(string token)
        {
            using (var sha = SHA256.Create())
            {
                byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
                var sb = new StringBuilder(64);
                foreach (byte b in h) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
