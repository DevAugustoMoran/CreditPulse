using System;

namespace CreditPulse.Web.Models
{
    /// <summary>Un rol del sistema, con sus permisos por pantalla (ver Permiso).</summary>
    public class Rol
    {
        public int RolId { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool PuedeEvaluar { get; set; }
        public bool PuedeAdministrarCatalogo { get; set; }
        public bool PuedeAdministrarUsuarios { get; set; }
        public bool Activo { get; set; }

        /// <summary>Solo se llena en el listado de administración (Administracion/Roles.aspx).</summary>
        public int TotalUsuariosActivos { get; set; }

        public bool TienePermiso(Permiso permiso)
        {
            switch (permiso)
            {
                case Permiso.Evaluar: return PuedeEvaluar;
                case Permiso.AdministrarCatalogo: return PuedeAdministrarCatalogo;
                case Permiso.AdministrarUsuarios: return PuedeAdministrarUsuarios;
                default: return false;
            }
        }
    }
}
