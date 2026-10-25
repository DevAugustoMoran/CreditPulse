using System;
using System.IO;
using System.Web.Hosting;
using CreditPulse.Web.BLL;
using CreditPulse.Web.Models;
using CreditPulse.Web.Security;

namespace CreditPulse.Web.Evaluaciones
{
    public partial class Batch : BasePage
    {
        private readonly BatchEvaluationService _batchEvaluationService = new BatchEvaluationService();

        protected override Permiso? PermisoRequerido
        {
            get { return Permiso.Evaluar; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnProcesar_Click(object sender, EventArgs e)
        {
            if (!fileUploadExcel.HasFile)
            {
                MostrarError("Selecciona un archivo Excel antes de continuar.");
                return;
            }

            string extension = Path.GetExtension(fileUploadExcel.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
            {
                MostrarError("El archivo debe ser .xlsx (Excel moderno). Si tienes un .xls antiguo, ábrelo en Excel y usa \"Guardar como\" → \"Libro de Excel (.xlsx)\".");
                return;
            }

            try
            {
                var usuario = SessionHelper.ObtenerUsuarioActual();

                LotePendiente pendiente;
                using (Stream flujo = fileUploadExcel.PostedFile.InputStream)
                {
                    // Síncrono y rápido: solo lee el Excel a memoria y crea el
                    // encabezado del lote en estado "Procesando" (ver
                    // BatchEvaluationService.PrepararLote). El trabajo pesado
                    // -evaluar cada fila contra la SIB y guardarla- queda
                    // encolado para correr en segundo plano, así que esta
                    // solicitud responde de inmediato sin importar cuántas
                    // filas tenga el archivo.
                    pendiente = _batchEvaluationService.PrepararLote(flujo, fileUploadExcel.FileName, usuario.UsuarioId);
                }

                // No se captura nada de HttpContext/Session dentro del
                // delegado: el hilo en segundo plano corre fuera de esta
                // solicitud, así que "pendiente" (ya con todo lo necesario
                // en memoria) es lo único que necesita.
                HostingEnvironment.QueueBackgroundWorkItem(cancellationToken =>
                {
                    _batchEvaluationService.ProcesarEnSegundoPlano(pendiente);
                });

                Response.Redirect("~/Evaluacion/LoteResultado.aspx?id=" + pendiente.LoteId, false);
            }
            catch (InvalidOperationException opEx)
            {
                MostrarError(opEx.Message);
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo procesar el archivo: " + ex.Message);
            }
        }

        private void MostrarError(string mensaje)
        {
            litMensajeError.Text = Server.HtmlEncode(mensaje);
            pnlMensajeError.Visible = true;
        }
    }
}
