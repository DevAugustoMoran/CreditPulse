using System;

namespace CreditPulse.Web.Models
{
    /// <summary>Producto bancario evaluable (tarjeta de crédito, préstamo hipotecario, etc.).</summary>
    public class Producto
    {
        public int ProductoId { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        /// <summary>Indica si el formulario de evaluación debe pedir ingresos mensuales.</summary>
        public bool RequiereIngresos { get; set; }

        /// <summary>Indica si el formulario de evaluación debe pedir tipo de empleo.</summary>
        public bool RequiereTipoEmpleo { get; set; }

        /// <summary>Indica si el formulario de evaluación debe pedir antigüedad laboral.</summary>
        public bool RequiereAntiguedadLaboral { get; set; }

        /// <summary>
        /// Un producto inactivo no aparece en el selector de Nueva Evaluación
        /// ni puede recibir evaluaciones (individuales o batch), pero sigue
        /// visible en la pantalla de administración para poder reactivarlo.
        /// </summary>
        public bool Activo { get; set; }

        /// <summary>Solo se llena cuando lo pide la pantalla de administración (conteo de políticas activas).</summary>
        public int TotalPoliticas { get; set; }
    }
}
