using System;
using CreditPulse.Web.DAL;
using CreditPulse.Web.Models;
using CreditPulse.Web.Security;

namespace CreditPulse.Web.BLL
{
    public enum LoginStatus
    {
        Exitoso,
        CredencialesInvalidas,
        UsuarioInactivo,
        CuentaBloqueada
    }

    public class LoginResult
    {
        public LoginStatus Status { get; set; }
        public Usuario Usuario { get; set; }
        public string MensajeError { get; set; }
    }

    /// <summary>
    /// Autenticación básica (Fase 1). El manejo de roles/permisos por
    /// pantalla (Administrador, Consultas, Analista, Cajero, ...) descrito en
    /// el levantamiento de requerimientos queda para la Fase 2; por ahora
    /// todo usuario autenticado tiene acceso a las pantallas del núcleo.
    /// </summary>
    public class AuthService
    {
        private const int MaxIntentosFallidos = 5;

        private readonly UsuarioRepository _usuarioRepository;

        public AuthService() : this(new UsuarioRepository()) { }

        public AuthService(UsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public LoginResult IniciarSesion(string email, string password)
        {
            var usuario = _usuarioRepository.ObtenerPorEmail((email ?? string.Empty).Trim().ToLowerInvariant());

            if (usuario == null)
            {
                return new LoginResult { Status = LoginStatus.CredencialesInvalidas, MensajeError = "Usuario o contraseña incorrectos." };
            }

            if (!usuario.Activo)
            {
                return new LoginResult { Status = LoginStatus.UsuarioInactivo, MensajeError = "El usuario está inactivo. Contacte al administrador." };
            }

            if (usuario.IntentosFallidos >= MaxIntentosFallidos)
            {
                return new LoginResult { Status = LoginStatus.CuentaBloqueada, MensajeError = "La cuenta fue bloqueada por múltiples intentos fallidos. Contacte al administrador." };
            }

            bool passwordValida = PasswordHasher.Verificar(password ?? string.Empty, usuario.PasswordHash, usuario.PasswordSalt);

            if (!passwordValida)
            {
                _usuarioRepository.RegistrarLoginFallido(usuario.Email);
                return new LoginResult { Status = LoginStatus.CredencialesInvalidas, MensajeError = "Usuario o contraseña incorrectos." };
            }

            _usuarioRepository.RegistrarLoginExitoso(usuario.UsuarioId);
            return new LoginResult { Status = LoginStatus.Exitoso, Usuario = usuario };
        }
    }
}
