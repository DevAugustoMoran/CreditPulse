using System;
using System.Configuration;
using System.Web.Http;

namespace CreditPulse.Web
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Registra las APIs de integración (/api/v1/...) que conviven en
            // este mismo sitio WebForms (ver Api/App_Start/WebApiConfig.cs y
            // el README, sección "APIs de integración").
            GlobalConfiguration.Configure(Api.WebApiConfig.Register);

            // Nota: Evaluaciones Batch (BatchEvaluationService, ExcelDataReader)
            // solo soporta .xlsx a propósito. Leer .xls legacy requeriría
            // registrar System.Text.CodePagesEncodingProvider, que depende del
            // paquete NuGet System.Text.Encoding.CodePages: una dependencia
            // extra y frágil (versión/carpeta lib exacta) solo para un formato
            // en desuso. Se prefirió no incluirla.
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            // El tiempo de sesión configurable (appSettings:SessionTimeoutMinutes)
            // se aplica aquí para que Session y el ticket de Forms Authentication
            // expiren de forma consistente sin tener que editar dos lugares del
            // Web.config por separado.
            int minutos;
            if (int.TryParse(ConfigurationManager.AppSettings["SessionTimeoutMinutes"], out minutos) && minutos > 0)
            {
                Session.Timeout = minutos;
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            // El manejo detallado de errores se documenta en el Manual Técnico.
            // customErrors en Web.config redirige a Error.aspx en producción.
        }

        protected void Session_End(object sender, EventArgs e)
        {
        }

        protected void Application_End(object sender, EventArgs e)
        {
        }
    }
}
