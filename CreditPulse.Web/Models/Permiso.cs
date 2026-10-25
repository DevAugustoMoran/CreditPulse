using System;

namespace CreditPulse.Web.Models
{
    /// <summary>
    /// Acciones que un rol puede o no tener habilitadas. Reemplaza el
    /// esquema anterior (comparar Usuario.RolNombre contra las constantes
    /// de RolesDeSistema) por permisos declarados en la tabla Roles
    /// (ver Database/08_UsuariosRoles.sql), para que un rol creado desde
    /// Administracion/RolForm.aspx pueda recibir acceso real a pantallas
    /// sin necesidad de tocar código.
    /// </summary>
    public enum Permiso
    {
        /// <summary>Crear y capturar evaluaciones, individuales y por lote (Nueva Evaluación, Batch).</summary>
        Evaluar,

        /// <summary>Administrar el catálogo de Productos y Políticas.</summary>
        AdministrarCatalogo,

        /// <summary>Administrar Usuarios y Roles.</summary>
        AdministrarUsuarios
    }
}
