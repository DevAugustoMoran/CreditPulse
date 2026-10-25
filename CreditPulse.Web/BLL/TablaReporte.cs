using System.Collections.Generic;

namespace CreditPulse.Web.BLL
{
    /// <summary>Una columna de un reporte tabular (sirve igual para CSV, Excel y PDF).</summary>
    public class ColumnaReporte
    {
        public ColumnaReporte(string titulo, double ancho, bool numerica = false, bool enPdf = true, bool esColor = false, string tituloPdf = null)
        {
            Titulo = titulo;
            Ancho = ancho;
            Numerica = numerica;
            EnPdf = enPdf;
            EsColor = esColor;
            TituloPdf = tituloPdf ?? titulo;
        }

        /// <summary>Encabezado completo (CSV y Excel).</summary>
        public string Titulo { get; private set; }

        /// <summary>Encabezado más corto para el PDF, donde el espacio es limitado.</summary>
        public string TituloPdf { get; private set; }

        /// <summary>Peso relativo del ancho de la columna en el PDF.</summary>
        public double Ancho { get; private set; }

        /// <summary>true = en Excel se guarda como número (y en el PDF se alinea a la derecha). DPI, NIT y similares NO son numéricos: llevan ceros a la izquierda.</summary>
        public bool Numerica { get; private set; }

        /// <summary>false = la columna solo va en CSV y Excel (el PDF no tiene espacio para las 26 columnas del batch).</summary>
        public bool EnPdf { get; private set; }

        /// <summary>true = el valor es Verde / Amarillo / Rojo y se pinta de ese color en Excel y PDF.</summary>
        public bool EsColor { get; private set; }
    }

    /// <summary>Contenido de un reporte listo para escribirse en cualquier formato de exportación.</summary>
    public class TablaReporte
    {
        public TablaReporte()
        {
            Resumen = new List<string>();
            Columnas = new List<ColumnaReporte>();
            Filas = new List<string[]>();
        }

        public string Titulo { get; set; }
        public string Subtitulo { get; set; }

        /// <summary>Líneas de totales que van arriba de la tabla.</summary>
        public List<string> Resumen { get; private set; }

        /// <summary>Texto "Generado el ... (hora de Guatemala)".</summary>
        public string GeneradoEn { get; set; }

        public List<ColumnaReporte> Columnas { get; private set; }

        /// <summary>Cada fila tiene un texto por columna, en el mismo orden que <see cref="Columnas"/>.</summary>
        public List<string[]> Filas { get; private set; }
    }
}
