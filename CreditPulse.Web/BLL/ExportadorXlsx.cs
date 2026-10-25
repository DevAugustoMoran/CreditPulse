using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CreditPulse.Web.BLL
{
    /// <summary>
    /// Escribe un reporte como libro de Excel (.xlsx) sin librerías externas:
    /// un .xlsx es un ZIP con archivos XML, y aquí se arma el mínimo necesario
    /// (una hoja con título, resumen, encabezado fijo con filtros, anchos de
    /// columna y colores Verde/Amarillo/Rojo). Los textos se guardan como
    /// texto (inlineStr), así que una celda que empiece con "=" no se ejecuta
    /// como fórmula al abrir el archivo.
    /// </summary>
    public static class ExportadorXlsx
    {
        private const string NsMain = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string NsRel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        // Índices de estilo (ver Styles()).
        private const int EstiloTitulo = 2, EstiloMuted = 3, EstiloEncabezado = 1, EstiloTexto = 4, EstiloNumero = 5,
                          EstiloVerde = 6, EstiloAmarillo = 7, EstiloRojo = 8;

        public static void Escribir(Stream destino, TablaReporte tabla, string nombreHoja)
        {
            string hoja = NombreHojaValido(nombreHoja);

            using (var zip = new ZipArchive(destino, ZipArchiveMode.Create, true))
            {
                Agregar(zip, "[Content_Types].xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                    "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                    "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                    "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                    "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                    "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                    "</Types>");

                Agregar(zip, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                    "</Relationships>");

                int filaEncabezado = FilaEncabezado(tabla);
                int ultimaFila = filaEncabezado + tabla.Filas.Count;
                string ultimaColumna = NombreColumna(tabla.Columnas.Count);

                Agregar(zip, "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<workbook xmlns=\"" + NsMain + "\" xmlns:r=\"" + NsRel + "\">" +
                    "<bookViews><workbookView/></bookViews>" +
                    "<sheets><sheet name=\"" + Xml(hoja) + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                    "<definedNames><definedName name=\"_xlnm._FilterDatabase\" localSheetId=\"0\" hidden=\"1\">'" + Xml(hoja.Replace("'", "''")) + "'!$A$" + filaEncabezado + ":$" + ultimaColumna + "$" + ultimaFila + "</definedName></definedNames>" +
                    "</workbook>");

                Agregar(zip, "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                    "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                    "</Relationships>");

                Agregar(zip, "xl/styles.xml", Styles());

                ZipArchiveEntry entrada = zip.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
                using (var escritor = new StreamWriter(entrada.Open(), new UTF8Encoding(false)))
                {
                    EscribirHoja(escritor, tabla, filaEncabezado, ultimaFila, ultimaColumna);
                }
            }
        }

        private static int FilaEncabezado(TablaReporte t)
        {
            // título, subtítulo, [generado], resumen..., línea en blanco, encabezado
            int fila = 1 + 1; // título + subtítulo
            if (!string.IsNullOrEmpty(t.GeneradoEn)) fila++;
            fila += t.Resumen.Count;
            return fila + 2; // línea en blanco + el encabezado en sí
        }

        private static void EscribirHoja(TextWriter w, TablaReporte t, int filaEncabezado, int ultimaFila, string ultimaColumna)
        {
            int nCols = t.Columnas.Count;

            w.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            w.Write("<worksheet xmlns=\"" + NsMain + "\">");
            w.Write("<sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr>");
            w.Write("<dimension ref=\"A1:" + ultimaColumna + ultimaFila + "\"/>");
            w.Write("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"" + filaEncabezado + "\" topLeftCell=\"A" + (filaEncabezado + 1) + "\" activePane=\"bottomLeft\" state=\"frozen\"/>" +
                    "<selection pane=\"bottomLeft\" activeCell=\"A" + (filaEncabezado + 1) + "\" sqref=\"A" + (filaEncabezado + 1) + "\"/></sheetView></sheetViews>");
            w.Write("<sheetFormatPr defaultRowHeight=\"15\"/>");

            w.Write("<cols>");
            for (int c = 0; c < nCols; c++)
            {
                w.Write("<col min=\"" + (c + 1) + "\" max=\"" + (c + 1) + "\" width=\"" + AnchoColumna(t, c).ToString("0.##", CultureInfo.InvariantCulture) + "\" customWidth=\"1\"/>");
            }
            w.Write("</cols>");

            w.Write("<sheetData>");
            int fila = 1;
            FilaTexto(w, fila++, t.Titulo, EstiloTitulo, 24);
            FilaTexto(w, fila++, t.Subtitulo, EstiloMuted, 0);
            if (!string.IsNullOrEmpty(t.GeneradoEn)) FilaTexto(w, fila++, t.GeneradoEn, EstiloMuted, 0);
            foreach (string linea in t.Resumen) FilaTexto(w, fila++, linea, EstiloMuted, 0);
            fila++; // línea en blanco

            // Encabezado
            w.Write("<row r=\"" + fila + "\" ht=\"34\" customHeight=\"1\">");
            for (int c = 0; c < nCols; c++) Texto(w, c, fila, t.Columnas[c].Titulo, EstiloEncabezado);
            w.Write("</row>");
            fila++;

            foreach (string[] datos in t.Filas)
            {
                w.Write("<row r=\"" + fila + "\">");
                for (int c = 0; c < nCols; c++)
                {
                    string valor = c < datos.Length ? datos[c] : string.Empty;
                    ColumnaReporte col = t.Columnas[c];

                    if (string.IsNullOrEmpty(valor))
                    {
                        w.Write("<c r=\"" + NombreColumna(c + 1) + fila + "\" s=\"" + EstiloTexto + "\"/>");
                    }
                    else if (col.Numerica && EsNumero(valor))
                    {
                        w.Write("<c r=\"" + NombreColumna(c + 1) + fila + "\" s=\"" + EstiloNumero + "\"><v>" + valor + "</v></c>");
                    }
                    else
                    {
                        Texto(w, c, fila, valor, col.EsColor ? EstiloDeColor(valor) : EstiloTexto);
                    }
                }
                w.Write("</row>");
                fila++;
            }
            w.Write("</sheetData>");

            w.Write("<autoFilter ref=\"A" + filaEncabezado + ":" + ultimaColumna + ultimaFila + "\"/>");
            w.Write("<pageMargins left=\"0.4\" right=\"0.4\" top=\"0.6\" bottom=\"0.6\" header=\"0.3\" footer=\"0.3\"/>");
            w.Write("<pageSetup orientation=\"landscape\" fitToWidth=\"1\" fitToHeight=\"0\"/>");
            w.Write("</worksheet>");
        }

        private static void FilaTexto(TextWriter w, int fila, string texto, int estilo, int alto)
        {
            w.Write("<row r=\"" + fila + "\"" + (alto > 0 ? " ht=\"" + alto + "\" customHeight=\"1\"" : string.Empty) + ">");
            if (!string.IsNullOrEmpty(texto)) Texto(w, 0, fila, texto, estilo);
            w.Write("</row>");
        }

        private static void Texto(TextWriter w, int columnaCero, int fila, string texto, int estilo)
        {
            w.Write("<c r=\"" + NombreColumna(columnaCero + 1) + fila + "\" s=\"" + estilo + "\" t=\"inlineStr\"><is><t xml:space=\"preserve\">" + Xml(texto) + "</t></is></c>");
        }

        private static int EstiloDeColor(string valor)
        {
            switch ((valor ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "verde": return EstiloVerde;
                case "amarillo": return EstiloAmarillo;
                case "rojo": return EstiloRojo;
                default: return EstiloTexto;
            }
        }

        private static double AnchoColumna(TablaReporte t, int c)
        {
            int maximo = Math.Min(t.Columnas[c].Titulo.Length, 18);
            int revisadas = 0;
            foreach (string[] fila in t.Filas)
            {
                if (c < fila.Length && fila[c] != null && fila[c].Length > maximo) maximo = fila[c].Length;
                if (++revisadas >= 400) break; // con 400 filas de muestra alcanza para dimensionar
            }
            return Math.Max(10, Math.Min(46, maximo + 2));
        }

        private static string Styles()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<styleSheet xmlns=\"" + NsMain + "\">" +
                "<fonts count=\"7\">" +
                "<font><sz val=\"11\"/><name val=\"Calibri\"/></font>" +                                             // 0 normal
                "<font><b/><sz val=\"11\"/><color rgb=\"FFFFFFFF\"/><name val=\"Calibri\"/></font>" +                // 1 encabezado
                "<font><b/><sz val=\"16\"/><color rgb=\"FF1F3A8A\"/><name val=\"Calibri\"/></font>" +                // 2 título
                "<font><sz val=\"10\"/><color rgb=\"FF595959\"/><name val=\"Calibri\"/></font>" +                    // 3 texto atenuado
                "<font><b/><sz val=\"11\"/><color rgb=\"FF15803D\"/><name val=\"Calibri\"/></font>" +                // 4 verde
                "<font><b/><sz val=\"11\"/><color rgb=\"FFB45309\"/><name val=\"Calibri\"/></font>" +                // 5 amarillo
                "<font><b/><sz val=\"11\"/><color rgb=\"FFB91C1C\"/><name val=\"Calibri\"/></font>" +                // 6 rojo
                "</fonts>" +
                "<fills count=\"3\">" +
                "<fill><patternFill patternType=\"none\"/></fill>" +
                "<fill><patternFill patternType=\"gray125\"/></fill>" +
                "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF1F3A8A\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
                "</fills>" +
                "<borders count=\"2\">" +
                "<border><left/><right/><top/><bottom/><diagonal/></border>" +
                "<border><left/><right/><top/><bottom style=\"thin\"><color rgb=\"FFD9DEE8\"/></bottom><diagonal/></border>" +
                "</borders>" +
                "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                "<cellXfs count=\"9\">" +
                "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +                                                                                       // 0
                "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>" + // 1
                "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +                                                                        // 2
                "<xf numFmtId=\"0\" fontId=\"3\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +                                                                        // 3
                "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\"/>" +                                                                      // 4 texto
                "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\"/></xf>" +             // 5 número
                "<xf numFmtId=\"0\" fontId=\"4\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyBorder=\"1\"/>" +                                                      // 6
                "<xf numFmtId=\"0\" fontId=\"5\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyBorder=\"1\"/>" +                                                      // 7
                "<xf numFmtId=\"0\" fontId=\"6\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyBorder=\"1\"/>" +                                                      // 8
                "</cellXfs>" +
                "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
                "</styleSheet>";
        }

        private static void Agregar(ZipArchive zip, string ruta, string contenido)
        {
            ZipArchiveEntry entrada = zip.CreateEntry(ruta, CompressionLevel.Optimal);
            using (var escritor = new StreamWriter(entrada.Open(), new UTF8Encoding(false)))
            {
                escritor.Write(contenido);
            }
        }

        private static bool EsNumero(string valor)
        {
            double numero;
            return double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out numero)
                   && !double.IsNaN(numero) && !double.IsInfinity(numero);
        }

        /// <summary>1 → A, 26 → Z, 27 → AA.</summary>
        public static string NombreColumna(int numero)
        {
            var sb = new StringBuilder();
            while (numero > 0)
            {
                int resto = (numero - 1) % 26;
                sb.Insert(0, (char)('A' + resto));
                numero = (numero - 1) / 26;
            }
            return sb.ToString();
        }

        private static string NombreHojaValido(string nombre)
        {
            string limpio = string.IsNullOrWhiteSpace(nombre) ? "Reporte" : nombre;
            foreach (char invalido in new[] { '\\', '/', '?', '*', '[', ']', ':' }) limpio = limpio.Replace(invalido, ' ');
            limpio = limpio.Trim();
            if (limpio.Length == 0) limpio = "Reporte";
            return limpio.Length > 31 ? limpio.Substring(0, 31) : limpio;
        }

        /// <summary>Escapa para XML y descarta caracteres de control que XML 1.0 no permite.</summary>
        private static string Xml(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return string.Empty;

            var sb = new StringBuilder(texto.Length + 8);
            foreach (char c in texto)
            {
                if (c < 0x20 && c != '\t' && c != '\n' && c != '\r') continue;
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
    }
}
