using System;
using System.Configuration;
using System.Globalization;

namespace CreditPulse.Web.BLL
{
    /// <summary>
    /// Conversión de fechas para MOSTRAR en pantalla.
    ///
    /// Toda fecha de la base de datos se guarda en UTC (SYSUTCDATETIME() en
    /// SQL Server / DateTime.UtcNow en C#). Eso es correcto y no cambia: es
    /// lo que consumen las APIs de integración. Lo que fallaba era el
    /// momento de mostrarla: el Dashboard imprimía el valor UTC tal cual
    /// (6 horas adelantado respecto a Guatemala) y el detalle usaba
    /// DateTime.ToLocalTime(), que depende de la zona horaria del SERVIDOR
    /// -en un hosting compartido casi nunca es la de Guatemala-.
    ///
    /// Esta clase convierte siempre a la zona configurada en el appSetting
    /// "ZonaHorariaId" (por defecto "Central America Standard Time" =
    /// UTC-6 sin horario de verano, que es la hora oficial de Guatemala),
    /// sin importar dónde esté alojado el servidor.
    /// </summary>
    public static class ZonaHoraria
    {
        private const string FormatoFechaHora = "dd/MM/yyyy HH:mm";
        private static readonly object Candado = new object();
        private static TimeZoneInfo _zona;

        /// <summary>Zona horaria de presentación (Guatemala por defecto).</summary>
        public static TimeZoneInfo Zona
        {
            get
            {
                if (_zona == null)
                {
                    lock (Candado)
                    {
                        if (_zona == null) _zona = Resolver();
                    }
                }
                return _zona;
            }
        }

        /// <summary>
        /// Convierte una fecha guardada en UTC (DateTime.Kind puede venir
        /// "Unspecified" cuando sale de SQL Server: se asume UTC) a la hora
        /// de Guatemala.
        /// </summary>
        public static DateTime DesdeUtc(DateTime utc)
        {
            DateTime comoUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(comoUtc, Zona);
        }

        /// <summary>Texto "dd/MM/yyyy HH:mm" en hora de Guatemala para una fecha UTC.</summary>
        public static string Formato(DateTime utc)
        {
            return DesdeUtc(utc).ToString(FormatoFechaHora, CultureInfo.InvariantCulture);
        }

        /// <summary>Igual que <see cref="Formato(DateTime)"/>, para un valor que puede venir de Eval() en el markup (objeto o null).</summary>
        public static string Formato(object utc)
        {
            if (utc == null || utc == DBNull.Value) return string.Empty;
            return Formato(Convert.ToDateTime(utc, CultureInfo.InvariantCulture));
        }

        /// <summary>Fecha y hora actual en Guatemala.</summary>
        public static DateTime Ahora()
        {
            return DesdeUtc(DateTime.UtcNow);
        }

        /// <summary>
        /// Inicio del mes (00:00 hora de Guatemala) expresado en UTC, para
        /// filtrar columnas guardadas en UTC con límites de mes "de
        /// Guatemala": un mes termina a las 24:00 locales, no a las 24:00 UTC.
        /// </summary>
        public static DateTime InicioDeMesEnUtc(int anio, int mes)
        {
            var local = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(local, Zona);
        }

        /// <summary>Inicio del mes siguiente (exclusivo) en UTC: el límite superior del filtro mensual.</summary>
        public static DateTime FinDeMesEnUtc(int anio, int mes)
        {
            DateTime siguiente = new DateTime(anio, mes, 1).AddMonths(1);
            return InicioDeMesEnUtc(siguiente.Year, siguiente.Month);
        }

        private static TimeZoneInfo Resolver()
        {
            string[] candidatos =
            {
                ConfigurationManager.AppSettings["ZonaHorariaId"],
                "Central America Standard Time", // Windows / IIS
                "America/Guatemala"               // Linux / macOS (IANA)
            };

            foreach (string id in candidatos)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id.Trim());
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            // Último recurso: Guatemala no usa horario de verano, así que un
            // desfase fijo de UTC-6 es exacto.
            return TimeZoneInfo.CreateCustomTimeZone("GT-UTC-6", TimeSpan.FromHours(-6), "Guatemala (UTC-6)", "Guatemala (UTC-6)");
        }
    }
}
