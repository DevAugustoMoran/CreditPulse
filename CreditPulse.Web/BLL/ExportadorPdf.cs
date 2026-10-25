using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace CreditPulse.Web.BLL
{
    /// <summary>
    /// Escribe un reporte como PDF (A4 horizontal) sin librerías externas:
    /// usa las fuentes estándar Helvetica / Helvetica-Bold, que todo lector de
    /// PDF trae incorporadas. Incluye título, resumen, tabla con encabezado
    /// repetido en cada página, filas alternadas, colores Verde/Amarillo/Rojo
    /// y pie "Página X de Y".
    /// </summary>
    public static class ExportadorPdf
    {
        private const double AnchoPagina = 841.89, AltoPagina = 595.28;
        private const double Margen = 34;
        private const double TamanoLetra = 8, Interlineado = 10, Relleno = 4;
        private const int MaxLineasPorCelda = 3;
        private const double AltoPie = 22;

        // Anchos de Helvetica y Helvetica-Bold (milésimas de em) para los caracteres 32..126.
        private static readonly int[] AnchosNormal = { 278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278, 556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556, 1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556, 333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556, 556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584 };
        private static readonly int[] AnchosNegrita = { 278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278, 556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611, 975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556, 333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611, 611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584 };

        private static readonly Encoding Ansi = Encoding.GetEncoding(1252);

        /// <param name="logoJpeg">JPEG baseline RGB opcional para el encabezado de la primera página (null = sin logo).</param>
        public static void Escribir(Stream destino, TablaReporte tabla, byte[] logoJpeg)
        {
            int logoAncho = 0, logoAlto = 0;
            if (logoJpeg != null && !LeerDimensionesJpeg(logoJpeg, out logoAncho, out logoAlto)) logoJpeg = null;

            List<ColumnaReporte> columnas = tabla.Columnas.Where(c => c.EnPdf).ToList();
            List<int> indices = new List<int>();
            for (int i = 0; i < tabla.Columnas.Count; i++) if (tabla.Columnas[i].EnPdf) indices.Add(i);

            double utilizable = AnchoPagina - 2 * Margen;
            double sumaPesos = columnas.Sum(c => c.Ancho);
            double[] anchos = columnas.Select(c => utilizable * c.Ancho / sumaPesos).ToArray();

            var paginas = new List<StringBuilder>();
            StringBuilder pg = null;
            double y = 0; // distancia desde el borde superior

            Action<bool> nuevaPagina = primera =>
            {
                pg = new StringBuilder();
                paginas.Add(pg);
                y = Margen;

                if (primera)
                {
                    double xTexto = Margen;
                    if (logoJpeg != null)
                    {
                        double alto = 44, ancho = alto * logoAncho / logoAlto;
                        pg.AppendFormat(CultureInfo.InvariantCulture, "q {0:0.##} 0 0 {1:0.##} {2:0.##} {3:0.##} cm /Im1 Do Q\n", ancho, alto, Margen, AltoPagina - Margen - alto);
                        xTexto = Margen + ancho + 12;
                    }

                    Texto(pg, xTexto, y + 15, Cadena(tabla.Titulo), true, 16, 0.12, 0.23, 0.54);
                    double yy = y + 31;
                    if (!string.IsNullOrEmpty(tabla.Subtitulo)) { Texto(pg, xTexto, yy, tabla.Subtitulo, false, 9.5, 0.2, 0.2, 0.2); yy += 13; }
                    if (!string.IsNullOrEmpty(tabla.GeneradoEn)) { Texto(pg, xTexto, yy, tabla.GeneradoEn, false, 8.5, 0.4, 0.4, 0.4); yy += 12; }
                    foreach (string linea in tabla.Resumen) { Texto(pg, xTexto, yy, linea, false, 9, 0.2, 0.2, 0.2); yy += 12; }
                    y = Math.Max(y + (logoJpeg != null ? 50 : 0), yy) + 8;
                }
                else
                {
                    Texto(pg, Margen, y + 8, tabla.Titulo + (string.IsNullOrEmpty(tabla.Subtitulo) ? string.Empty : " · " + tabla.Subtitulo), false, 8.5, 0.4, 0.4, 0.4);
                    y += 20;
                }

                // Encabezado de la tabla
                List<string[]> lineasEnc = columnas.Select((c, i) => Envolver(c.TituloPdf, anchos[i] - 2 * Relleno, true, MaxLineasPorCelda)).ToList();
                int maxLin = lineasEnc.Max(l => l.Length);
                double altoEnc = maxLin * Interlineado + 2 * Relleno - 1;
                Rect(pg, Margen, y, utilizable, altoEnc, 0.12, 0.23, 0.54);
                double x = Margen;
                for (int i = 0; i < columnas.Count; i++)
                {
                    double yl = y + Relleno + 7;
                    foreach (string linea in lineasEnc[i])
                    {
                        double w = Ancho(linea, true);
                        double xt = x + (anchos[i] - w) / 2;
                        Texto(pg, Math.Max(x + 1, xt), yl, linea, true, TamanoLetra, 1, 1, 1);
                        yl += Interlineado;
                    }
                    x += anchos[i];
                }
                y += altoEnc;
            };

            nuevaPagina(true);

            int numeroFila = 0;
            foreach (string[] datos in tabla.Filas)
            {
                var lineas = new List<string[]>();
                for (int i = 0; i < columnas.Count; i++)
                {
                    int origen = indices[i];
                    string valor = origen < datos.Length ? datos[origen] : string.Empty;
                    lineas.Add(Envolver(valor, anchos[i] - 2 * Relleno, columnas[i].EsColor, MaxLineasPorCelda));
                }

                int maxLineas = Math.Max(1, lineas.Max(l => l.Length));
                double alto = maxLineas * Interlineado + 2 * Relleno - 1;

                if (y + alto > AltoPagina - Margen - AltoPie) nuevaPagina(false);

                if (numeroFila % 2 == 1) Rect(pg, Margen, y, utilizable, alto, 0.95, 0.96, 0.99);

                double x = Margen;
                for (int i = 0; i < columnas.Count; i++)
                {
                    double r = 0.1, g = 0.1, b = 0.1;
                    bool negrita = false;
                    if (columnas[i].EsColor)
                    {
                        string v = (datos[indices[i]] ?? string.Empty).Trim().ToLowerInvariant();
                        negrita = true;
                        if (v == "verde") { r = 0.08; g = 0.5; b = 0.24; }
                        else if (v == "amarillo") { r = 0.71; g = 0.33; b = 0.04; }
                        else if (v == "rojo") { r = 0.73; g = 0.11; b = 0.11; }
                        else negrita = false;
                    }

                    double yl = y + Relleno + 6;
                    foreach (string linea in lineas[i])
                    {
                        double xt = columnas[i].Numerica ? x + anchos[i] - Relleno - Ancho(linea, negrita) : x + Relleno;
                        Texto(pg, xt, yl, linea, negrita, TamanoLetra, r, g, b);
                        yl += Interlineado;
                    }
                    x += anchos[i];
                }

                // línea divisoria inferior
                pg.AppendFormat(CultureInfo.InvariantCulture, "0.85 0.87 0.91 RG 0.4 w {0:0.##} {1:0.##} m {2:0.##} {1:0.##} l S\n", Margen, AltoPagina - (y + alto), Margen + utilizable);
                y += alto;
                numeroFila++;
            }

            if (tabla.Filas.Count == 0)
            {
                Texto(pg, Margen + Relleno, y + 16, "No hay registros para los filtros seleccionados.", false, 9, 0.4, 0.4, 0.4);
            }

            // Pies de página (necesitan conocer el total).
            for (int i = 0; i < paginas.Count; i++)
            {
                StringBuilder p = paginas[i];
                p.AppendFormat(CultureInfo.InvariantCulture, "0.8 0.82 0.87 RG 0.5 w {0:0.##} {1:0.##} m {2:0.##} {1:0.##} l S\n", Margen, Margen + 12, AnchoPagina - Margen);
                Texto(p, Margen, AltoPagina - Margen + 3, "CP Analítica (CreditPulse Analítica) · Motor de riesgos", false, 7.5, 0.45, 0.45, 0.45);
                string num = "Página " + (i + 1) + " de " + paginas.Count;
                Texto(p, AnchoPagina - Margen - Ancho(num, false) * 7.5 / TamanoLetra, AltoPagina - Margen + 3, num, false, 7.5, 0.45, 0.45, 0.45);
            }

            EscribirDocumento(destino, paginas, tabla.Titulo, logoJpeg, logoAncho, logoAlto);
        }

        // ---------- Dibujo ----------

        /// <summary>Texto con la línea base a <paramref name="yDesdeArriba"/> puntos del borde superior.</summary>
        private static void Texto(StringBuilder pg, double x, double yDesdeArriba, string texto, bool negrita, double tamano, double r, double g, double b)
        {
            if (string.IsNullOrEmpty(texto)) return;
            pg.AppendFormat(CultureInfo.InvariantCulture, "{0:0.###} {1:0.###} {2:0.###} rg BT /{3} {4:0.##} Tf {5:0.##} {6:0.##} Td ({7}) Tj ET\n",
                r, g, b, negrita ? "F2" : "F1", tamano, x, AltoPagina - yDesdeArriba, Escapar(texto));
        }

        private static void Rect(StringBuilder pg, double x, double yDesdeArriba, double ancho, double alto, double r, double g, double b)
        {
            pg.AppendFormat(CultureInfo.InvariantCulture, "{0:0.###} {1:0.###} {2:0.###} rg {3:0.##} {4:0.##} {5:0.##} {6:0.##} re f\n",
                r, g, b, x, AltoPagina - yDesdeArriba - alto, ancho, alto);
        }

        private static string Cadena(string s) { return s ?? string.Empty; }

        private static string Escapar(string texto)
        {
            var sb = new StringBuilder(texto.Length + 4);
            foreach (char c in texto)
            {
                if (c < 32) { sb.Append(' '); continue; }
                if (c == '(' || c == ')' || c == '\\') sb.Append('\\');
                sb.Append(c);
            }
            return sb.ToString();
        }

        // ---------- Medición y ajuste de texto ----------

        private static int AnchoCaracter(char c, bool negrita)
        {
            int[] tabla = negrita ? AnchosNegrita : AnchosNormal;
            if (c >= 32 && c <= 126) return tabla[c - 32];
            if (c == '\t') return tabla[0];

            // Letras con tilde / ñ: mismo ancho que la letra base.
            string descompuesto = c.ToString().Normalize(NormalizationForm.FormD);
            if (descompuesto.Length > 0 && descompuesto[0] >= 32 && descompuesto[0] <= 126 && descompuesto[0] != c)
                return tabla[descompuesto[0] - 32];

            return 556;
        }

        private static double Ancho(string texto, bool negrita)
        {
            double suma = 0;
            foreach (char c in texto) suma += AnchoCaracter(c, negrita);
            return suma * TamanoLetra / 1000.0;
        }

        /// <summary>Parte el texto en líneas que caben en <paramref name="anchoMax"/>; si pasa de <paramref name="maxLineas"/>, corta con "...".</summary>
        private static string[] Envolver(string texto, double anchoMax, bool negrita, int maxLineas)
        {
            if (string.IsNullOrEmpty(texto)) return new[] { string.Empty };

            texto = texto.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
            var lineas = new List<string>();
            var actual = new StringBuilder();

            Action cerrar = () => { lineas.Add(actual.ToString()); actual.Clear(); };

            foreach (string palabraOriginal in texto.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string palabra = palabraOriginal;

                // Palabra más larga que la columna: se parte por caracteres.
                while (Ancho(palabra, negrita) > anchoMax && palabra.Length > 1)
                {
                    if (actual.Length > 0) cerrar();
                    int corte = 1;
                    while (corte < palabra.Length && Ancho(palabra.Substring(0, corte + 1), negrita) <= anchoMax) corte++;
                    lineas.Add(palabra.Substring(0, corte));
                    palabra = palabra.Substring(corte);
                }

                string candidata = actual.Length == 0 ? palabra : actual + " " + palabra;
                if (Ancho(candidata, negrita) <= anchoMax)
                {
                    actual.Clear();
                    actual.Append(candidata);
                }
                else
                {
                    cerrar();
                    actual.Append(palabra);
                }
            }
            if (actual.Length > 0) cerrar();
            if (lineas.Count == 0) lineas.Add(string.Empty);

            if (lineas.Count > maxLineas)
            {
                lineas = lineas.Take(maxLineas).ToList();
                string ultima = lineas[maxLineas - 1];
                while (ultima.Length > 0 && Ancho(ultima + "...", negrita) > anchoMax) ultima = ultima.Substring(0, ultima.Length - 1);
                lineas[maxLineas - 1] = ultima + "...";
            }
            return lineas.ToArray();
        }

        // ---------- Estructura del archivo PDF ----------

        private static void EscribirDocumento(Stream destino, List<StringBuilder> paginas, string titulo, byte[] logo, int logoAncho, int logoAlto)
        {
            bool hayLogo = logo != null;
            int idPrimeraPagina = hayLogo ? 6 : 5;   // 1 catálogo, 2 páginas, 3-4 fuentes, [5 imagen]
            int idInfo = idPrimeraPagina + paginas.Count * 2;
            int totalObjetos = idInfo;

            var posiciones = new long[totalObjetos + 1];
            var salida = new CuentaBytes(destino);

            Action<string> texto = s => salida.Write(Ansi.GetBytes(s));
            Action<int> inicio = id => { posiciones[id] = salida.Posicion; texto(id + " 0 obj\n"); };

            texto("%PDF-1.4\n");
            salida.Write(new byte[] { (byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n' });

            inicio(1);
            texto("<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            inicio(2);
            var kids = new StringBuilder();
            for (int i = 0; i < paginas.Count; i++) kids.Append(idPrimeraPagina + i * 2).Append(" 0 R ");
            texto("<< /Type /Pages /Kids [ " + kids + "] /Count " + paginas.Count + " >>\nendobj\n");

            inicio(3);
            texto("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
            inicio(4);
            texto("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

            if (hayLogo)
            {
                inicio(5);
                texto("<< /Type /XObject /Subtype /Image /Width " + logoAncho + " /Height " + logoAlto +
                      " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length " + logo.Length + " >>\nstream\n");
                salida.Write(logo);
                texto("\nendstream\nendobj\n");
            }

            string recursos = "/Resources << /Font << /F1 3 0 R /F2 4 0 R >>" + (hayLogo ? " /XObject << /Im1 5 0 R >>" : string.Empty) + " >>";
            for (int i = 0; i < paginas.Count; i++)
            {
                int idPagina = idPrimeraPagina + i * 2, idContenido = idPagina + 1;

                inicio(idPagina);
                texto("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + AnchoPagina.ToString("0.##", CultureInfo.InvariantCulture) + " " +
                      AltoPagina.ToString("0.##", CultureInfo.InvariantCulture) + "] " + recursos + " /Contents " + idContenido + " 0 R >>\nendobj\n");

                byte[] comprimido = ZlibComprimir(Ansi.GetBytes(paginas[i].ToString()));
                inicio(idContenido);
                texto("<< /Filter /FlateDecode /Length " + comprimido.Length + " >>\nstream\n");
                salida.Write(comprimido);
                texto("\nendstream\nendobj\n");
            }

            inicio(idInfo);
            texto("<< /Title (" + Escapar(Cadena(titulo)) + ") /Producer (CP Analitica) >>\nendobj\n");

            long inicioXref = salida.Posicion;
            texto("xref\n0 " + (totalObjetos + 1) + "\n0000000000 65535 f \n");
            for (int id = 1; id <= totalObjetos; id++) texto(posiciones[id].ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");
            texto("trailer\n<< /Size " + (totalObjetos + 1) + " /Root 1 0 R /Info " + idInfo + " 0 R >>\nstartxref\n" + inicioXref + "\n%%EOF\n");
        }

        /// <summary>Flujo zlib (cabecera + deflate + Adler-32), lo que exige /FlateDecode.</summary>
        private static byte[] ZlibComprimir(byte[] datos)
        {
            using (var memoria = new MemoryStream())
            {
                memoria.WriteByte(0x78);
                memoria.WriteByte(0x9C);
                using (var deflate = new DeflateStream(memoria, CompressionLevel.Optimal, true))
                {
                    deflate.Write(datos, 0, datos.Length);
                }

                uint a = 1, b = 0;
                foreach (byte x in datos)
                {
                    a = (a + x) % 65521;
                    b = (b + a) % 65521;
                }
                uint adler = (b << 16) | a;
                memoria.WriteByte((byte)(adler >> 24));
                memoria.WriteByte((byte)(adler >> 16));
                memoria.WriteByte((byte)(adler >> 8));
                memoria.WriteByte((byte)adler);
                return memoria.ToArray();
            }
        }

        /// <summary>Lee ancho y alto de un JPEG (marcador SOF) y confirma que es RGB de 3 componentes.</summary>
        private static bool LeerDimensionesJpeg(byte[] d, out int ancho, out int alto)
        {
            ancho = alto = 0;
            if (d.Length < 4 || d[0] != 0xFF || d[1] != 0xD8) return false;

            int i = 2;
            while (i + 9 < d.Length)
            {
                if (d[i] != 0xFF) { i++; continue; }
                byte marcador = d[i + 1];
                if (marcador == 0xFF) { i++; continue; }
                if (marcador == 0xD8 || (marcador >= 0xD0 && marcador <= 0xD7) || marcador == 0x01) { i += 2; continue; }

                int longitud = (d[i + 2] << 8) | d[i + 3];
                bool esSof = marcador >= 0xC0 && marcador <= 0xCF && marcador != 0xC4 && marcador != 0xC8 && marcador != 0xCC;
                if (esSof)
                {
                    alto = (d[i + 5] << 8) | d[i + 6];
                    ancho = (d[i + 7] << 8) | d[i + 8];
                    int componentes = d[i + 9];
                    return componentes == 3 && marcador == 0xC0 && ancho > 0 && alto > 0; // solo baseline RGB
                }
                i += 2 + longitud;
            }
            return false;
        }

        /// <summary>Envuelve el flujo de salida para saber en qué byte se está (las referencias xref lo necesitan).</summary>
        private sealed class CuentaBytes
        {
            private readonly Stream _destino;
            public long Posicion { get; private set; }

            public CuentaBytes(Stream destino) { _destino = destino; }

            public void Write(byte[] datos)
            {
                _destino.Write(datos, 0, datos.Length);
                Posicion += datos.Length;
            }
        }
    }
}
