using System;

namespace CreditPulse.Web.Models
{
    /// <summary>
    /// Regla de negocio configurable asociada a un producto. El PolicyEngine
    /// interpreta <see cref="TipoRegla"/> y <see cref="ParametrosJson"/> para
    /// decidir el color de semáforo que le corresponde a esta política.
    /// </summary>
    public class Politica
    {
        public int PoliticaId { get; set; }
        public int ProductoId { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string TipoRegla { get; set; }
        public string ParametrosJson { get; set; }
        public int Orden { get; set; }

        /// <summary>Una política inactiva se guarda pero el PolicyEngine no la evalúa.</summary>
        public bool Activo { get; set; }
    }
}
