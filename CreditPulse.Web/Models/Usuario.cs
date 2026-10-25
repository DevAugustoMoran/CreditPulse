using System;

namespace CreditPulse.Web.Models
{
    public class Usuario
    {
        public int UsuarioId { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string NombreCompleto { get; set; }
        public int RolId { get; set; }
        public string RolNombre { get; set; }
        public bool Activo { get; set; }
        public int IntentosFallidos { get; set; }

        // Solo se cargan en los listados de administración
        // (UsuarioRepository.MapAdmin / sp_Usuarios_ObtenerTodosAdmin);
        // el login (MapConPermisos) no los necesita y los deja en su
        // valor por defecto.
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoLogin { get; set; }

        // Permisos del rol asignado (ver Database/08_UsuariosRoles.sql y
        // Models/Permiso.cs). Se cargan junto con el usuario al iniciar
        // sesión (sp_Usuarios_ObtenerPorEmail) para no tener que repetir
        // el JOIN contra Roles en cada verificación de acceso.
        public bool PuedeEvaluar { get; set; }
        public bool PuedeAdministrarCatalogo { get; set; }
        public bool PuedeAdministrarUsuarios { get; set; }

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
