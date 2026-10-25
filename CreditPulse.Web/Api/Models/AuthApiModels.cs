using System;

namespace CreditPulse.Web.Api.Models
{
    /// <summary>Cuerpo de POST /api/v1/auth/token.</summary>
    public class TokenApiRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    /// <summary>Cuerpo de POST /api/v1/auth/refresh y POST /api/v1/auth/revoke.</summary>
    public class RefreshApiRequest
    {
        public string RefreshToken { get; set; }
    }

    /// <summary>Respuesta de POST /api/v1/auth/token y /api/v1/auth/refresh.</summary>
    public class TokenApiResponse
    {
        public string TokenType { get; set; }
        public string AccessToken { get; set; }

        /// <summary>Segundos de vida que le quedan al token de acceso.</summary>
        public int ExpiresIn { get; set; }
        public DateTime ExpiresAtUtc { get; set; }

        public string RefreshToken { get; set; }
        public DateTime RefreshExpiresAtUtc { get; set; }

        public TokenUsuarioApiDto Usuario { get; set; }
    }

    public class TokenUsuarioApiDto
    {
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Rol { get; set; }
        public bool PuedeEvaluar { get; set; }
    }
}
