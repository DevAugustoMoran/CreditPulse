using System;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.BLL
{
    /// <summary>
    /// Agrupa los datos capturados en el formulario con el perfil devuelto
    /// por la SIB, para que <see cref="PolicyEngine"/> pueda evaluar
    /// cualquier política sin preocuparse de dónde viene cada dato.
    /// </summary>
    public class EvaluationContext
    {
        public NuevaEvaluacionRequest Solicitud { get; }
        public SibPerfil Sib { get; }
        public int EdadAnios { get; }

        public EvaluationContext(NuevaEvaluacionRequest solicitud, SibPerfil sib)
        {
            Solicitud = solicitud;
            Sib = sib;
            EdadAnios = CalcularEdad(solicitud.FechaNacimiento, DateTime.Today);
        }

        private static int CalcularEdad(DateTime fechaNacimiento, DateTime hoy)
        {
            int edad = hoy.Year - fechaNacimiento.Year;
            if (fechaNacimiento.Date > hoy.AddYears(-edad)) edad--;
            return edad;
        }
    }
}
