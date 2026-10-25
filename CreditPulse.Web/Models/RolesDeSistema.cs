using System;

namespace CreditPulse.Web.Models
{
    /// <summary>
    /// Nombres de los 3 roles sembrados por defecto (ver
    /// Database/04_SeedData.sql y Database/08_UsuariosRoles.sql). Ya NO se
    /// usan para restringir pantallas -eso ahora lo hace BasePage.PermisoRequerido
    /// contra los permisos de la tabla Roles (ver Models.Permiso)-, así que
    /// un administrador puede crear roles nuevos con otro nombre desde
    /// Administracion/RolForm.aspx y funcionan igual. Estas constantes solo
    /// sirven como referencia de los roles "de fábrica" del sistema (por
    /// ejemplo, para mostrarlos en el Manual Técnico o en scripts de
    /// siembra de datos de prueba).
    /// </summary>
    public static class RolesDeSistema
    {
        public const string Administrador = "Administrador";
        public const string AnalistaCredito = "Analista de Crédito";
        public const string Consulta = "Consulta";
    }
}
