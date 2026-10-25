using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Hosting;

namespace CreditPulse.Web.BLL
{
    public enum FormatoExportacion
    {
        Csv,
        Excel,
        Pdf
    }

    /// <summary>
    /// Convierte un reporte (<see cref="TablaReporte"/>) al formato pedido y lo
    /// entrega como descarga. Lo usan las pantallas de Reportes.
    /// </summary>
    public static class ExportacionReporte
    {
        public static byte[] Generar(FormatoExportacion formato, TablaReporte tabla, string nombreHoja, byte[] logoJpeg, out string contentType, out string extension)
        {
            switch (formato)
            {
                case FormatoExportacion.Excel:
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    extension = "xlsx";
                    using (var memoria = new MemoryStream())
                    {
                        ExportadorXlsx.Escribir(memoria, tabla, nombreHoja);
                        return memoria.ToArray();
                    }

                case FormatoExportacion.Pdf:
                    contentType = "application/pdf";
                    extension = "pdf";
                    using (var memoria = new MemoryStream())
                    {
                        ExportadorPdf.Escribir(memoria, tabla, logoJpeg);
                        return memoria.ToArray();
                    }

                default:
                    contentType = "text/csv; charset=utf-8";
                    extension = "csv";
                    return ReporteService.ComoCsvBytes(w => ReporteService.EscribirCsv(w, tabla));
            }
        }

        /// <summary>Envía el reporte como descarga y termina la respuesta (de lo contrario el HTML de la página se añadiría al final del archivo).</summary>
        public static void Enviar(HttpResponse respuesta, FormatoExportacion formato, TablaReporte tabla, string nombreBase, string nombreHoja)
        {
            string contentType, extension;
            byte[] contenido = Generar(formato, tabla, nombreHoja, LeerLogo(), out contentType, out extension);

            respuesta.Clear();
            respuesta.ContentType = contentType;
            respuesta.AddHeader("Content-Disposition", "attachment; filename=" + nombreBase + "." + extension);
            respuesta.AddHeader("Content-Length", contenido.Length.ToString());
            respuesta.BinaryWrite(contenido);
            respuesta.End();
        }

        private static byte[] LeerLogo()
        {
            try
            {
                string ruta = HostingEnvironment.MapPath("~/Content/img/logo-icono.jpg");
                return !string.IsNullOrEmpty(ruta) && File.Exists(ruta) ? File.ReadAllBytes(ruta) : null;
            }
            catch (Exception)
            {
                return null; // sin logo, el PDF sale igual
            }
        }
    }
}
