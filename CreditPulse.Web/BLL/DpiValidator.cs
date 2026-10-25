using System;
using System.Configuration;
using System.Linq;
using System.Text;

namespace CreditPulse.Web.BLL
{
    /// <summary>Resultado de validar un DPI (CUI): si es válido y, si no, por qué.</summary>
    public class DpiValidationResult
    {
        public bool EsValido { get; set; }

        /// <summary>DPI normalizado (solo dígitos). Vacío si no tenía forma de número.</summary>
        public string DpiNormalizado { get; set; }

        /// <summary>Motivo del rechazo, listo para mostrar al usuario. Null cuando es válido.</summary>
        public string Mensaje { get; set; }

        /// <summary>Nombre del departamento codificado en el DPI (solo si el código es válido).</summary>
        public string Departamento { get; set; }
    }

    /// <summary>
    /// Validación inicial del DPI guatemalteco (CUI - Código Único de
    /// Identificación, 13 dígitos), más allá de "tiene 13 caracteres".
    ///
    /// Estructura:  NNNNNNNN V DD MM
    ///   - NNNNNNNN : número de registro (8 dígitos).
    ///   - V        : dígito verificador del número de registro.
    ///   - DD       : código de departamento de nacimiento/vecindad (01 a 22).
    ///   - MM       : código de municipio dentro de ese departamento
    ///                (de 01 hasta el total de municipios que tiene).
    ///
    /// Dígito verificador: cada uno de los 8 dígitos del número se
    /// multiplica por su posición + 1 (2, 3, ..., 9), se suma todo y el
    /// residuo de dividir entre 11 debe ser igual a V.
    ///
    /// Es una validación de FORMATO, no una consulta al RENAP: confirma que
    /// el número está bien formado, no que la persona exista. Se aplica en
    /// la interfaz web (Nueva Evaluación y Evaluaciones Batch). Las APIs
    /// /api/v1 NO la aplican a propósito, para no cambiar el comportamiento
    /// del servicio externo que ya las consume.
    /// </summary>
    public static class DpiValidator
    {
        public const int Longitud = 13;

        private static readonly string[] Departamentos =
        {
            "Guatemala", "El Progreso", "Sacatepéquez", "Chimaltenango", "Escuintla", "Santa Rosa",
            "Sololá", "Totonicapán", "Quetzaltenango", "Suchitepéquez", "Retalhuleu", "San Marcos",
            "Huehuetenango", "Quiché", "Baja Verapaz", "Alta Verapaz", "Petén", "Izabal",
            "Zacapa", "Chiquimula", "Jalapa", "Jutiapa"
        };

        // Cantidad de municipios por departamento, en el mismo orden que
        // Departamentos. Huehuetenango se deja en 33 (Unión Cantinil, creado
        // en 2013) para no rechazar DPIs válidos de ese municipio.
        private static readonly int[] MunicipiosPorDepartamento =
        {
            17, 8, 16, 16, 13, 14,
            19, 8, 24, 21, 9, 30,
            33, 21, 8, 17, 14, 5,
            11, 11, 7, 17
        };

        /// <summary>
        /// Indica si debe verificarse también el dígito verificador
        /// (appSetting "DpiValidarDigitoVerificador", por defecto true). Se
        /// puede apagar para trabajar con DPIs de prueba que solo respetan
        /// el formato y los códigos de departamento/municipio.
        /// </summary>
        public static bool ValidarDigitoPorDefecto
        {
            get
            {
                bool valor;
                return !bool.TryParse(ConfigurationManager.AppSettings["DpiValidarDigitoVerificador"], out valor) || valor;
            }
        }

        public static DpiValidationResult Validar(string dpi)
        {
            return Validar(dpi, ValidarDigitoPorDefecto);
        }

        public static DpiValidationResult Validar(string dpi, bool validarDigitoVerificador)
        {
            string limpio = Normalizar(dpi);

            if (limpio.Length == 0)
                return Invalido(limpio, "El DPI es obligatorio.");

            if (!limpio.All(c => c >= '0' && c <= '9'))
                return Invalido(limpio, "El DPI solo puede contener números.");

            if (limpio.Length != Longitud)
                return Invalido(limpio, string.Format("El DPI debe tener exactamente {0} dígitos (tiene {1}).", Longitud, limpio.Length));

            int departamento = int.Parse(limpio.Substring(9, 2));
            int municipio = int.Parse(limpio.Substring(11, 2));

            if (departamento < 1 || departamento > Departamentos.Length)
                return Invalido(limpio, string.Format("Los dígitos 10 y 11 del DPI ({0:00}) no corresponden a ningún departamento (01 a {1}).", departamento, Departamentos.Length));

            int maxMunicipios = MunicipiosPorDepartamento[departamento - 1];
            string nombreDepartamento = Departamentos[departamento - 1];

            if (municipio < 1 || municipio > maxMunicipios)
                return Invalido(limpio, string.Format("Los dígitos 12 y 13 del DPI ({0:00}) no son un municipio válido de {1} (01 a {2}).", municipio, nombreDepartamento, maxMunicipios));

            if (validarDigitoVerificador)
            {
                int esperado = CalcularDigitoVerificador(limpio.Substring(0, 8));
                int recibido = limpio[8] - '0';
                if (esperado != recibido)
                    return Invalido(limpio, "El dígito verificador del DPI (posición 9) no coincide con el número de registro. Revisa que esté bien escrito.");
            }

            return new DpiValidationResult { EsValido = true, DpiNormalizado = limpio, Departamento = nombreDepartamento };
        }

        /// <summary>Quita espacios y guiones ("3478 95381 0101" o "3478-95381-0101" → solo dígitos).</summary>
        public static string Normalizar(string dpi)
        {
            if (string.IsNullOrWhiteSpace(dpi)) return string.Empty;

            var sb = new StringBuilder(dpi.Length);
            foreach (char c in dpi)
            {
                if (c == ' ' || c == '-' || c == '\t') continue;
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Dígito verificador esperado para los primeros 8 dígitos del DPI.</summary>
        public static int CalcularDigitoVerificador(string ochoDigitos)
        {
            int total = 0;
            for (int i = 0; i < 8; i++)
            {
                total += (ochoDigitos[i] - '0') * (i + 2);
            }
            return total % 11;
        }

        private static DpiValidationResult Invalido(string normalizado, string mensaje)
        {
            return new DpiValidationResult { EsValido = false, DpiNormalizado = normalizado, Mensaje = mensaje };
        }
    }
}
