using System;
using System.Configuration;
using System.Security.Cryptography;

namespace CreditPulse.Web.Security
{
    /// <summary>
    /// Hashing seguro de contraseñas (PBKDF2 / HMAC-SHA256 con salt aleatorio
    /// por usuario). El levantamiento de requerimientos pide "contraseña
    /// encriptada"; se optó deliberadamente por un hash irreversible en vez
    /// de una encriptación simétrica reversible, porque es la práctica
    /// estándar de la industria para credenciales (si la base de datos se
    /// filtra, un hash con salt no permite recuperar la contraseña original,
    /// mientras que un valor "desencriptable" sí). Esta decisión se explica
    /// también en el Manual Técnico.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSizeBytes = 16;
        private const int KeySizeBytes = 32;

        private static int Iterations
        {
            get
            {
                int iterations;
                return int.TryParse(ConfigurationManager.AppSettings["PasswordHashIterations"], out iterations)
                    ? iterations
                    : 100000;
            }
        }

        /// <summary>Genera un salt aleatorio nuevo y su hash correspondiente, ambos en Base64.</summary>
        public static void CrearHash(string password, out string hashBase64, out string saltBase64)
        {
            byte[] salt = new byte[SaltSizeBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = DeriveKey(password, salt, Iterations, KeySizeBytes);

            hashBase64 = Convert.ToBase64String(hash);
            saltBase64 = Convert.ToBase64String(salt);
        }

        /// <summary>Verifica una contraseña en texto plano contra el hash y salt almacenados.</summary>
        public static bool Verificar(string password, string hashBase64, string saltBase64)
        {
            byte[] salt = Convert.FromBase64String(saltBase64);
            byte[] hashEsperado = Convert.FromBase64String(hashBase64);
            byte[] hashCalculado = DeriveKey(password, salt, Iterations, hashEsperado.Length);

            return FixedTimeEquals(hashEsperado, hashCalculado);
        }

        /// <summary>
        /// Política de contraseña exigida por el curso: 10+ caracteres,
        /// al menos una letra, un número y un símbolo especial.
        /// </summary>
        public static bool CumplePolitica(string password, out string mensajeError)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 10)
            {
                mensajeError = "La contraseña debe tener al menos 10 caracteres.";
                return false;
            }

            bool tieneLetra = false, tieneNumero = false, tieneSimbolo = false;
            foreach (char c in password)
            {
                if (char.IsLetter(c)) tieneLetra = true;
                else if (char.IsDigit(c)) tieneNumero = true;
                else if (!char.IsWhiteSpace(c)) tieneSimbolo = true;
            }

            if (!tieneLetra || !tieneNumero || !tieneSimbolo)
            {
                mensajeError = "La contraseña debe incluir letras, números y símbolos especiales.";
                return false;
            }

            mensajeError = null;
            return true;
        }

        private static byte[] DeriveKey(string password, byte[] salt, int iterations, int keyLength)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(keyLength);
            }
        }

        /// <summary>Comparación en tiempo constante para evitar ataques de temporización.</summary>
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
