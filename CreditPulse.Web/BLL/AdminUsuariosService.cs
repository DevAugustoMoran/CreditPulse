using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CreditPulse.Web.DAL;
using CreditPulse.Web.Models;
using CreditPulse.Web.Security;

namespace CreditPulse.Web.BLL
{
    /// <summary>
    /// Reglas de negocio del módulo de Administración de Usuarios y Roles.
    /// Complementa a AdminCatalogoService (Productos/Políticas) con la misma
    /// filosofía: valida en la capa BLL, delega el guardado a la DAL, y
    /// traduce errores de SQL (código duplicado) a mensajes en español.
    ///
    /// Incluye dos guardas de seguridad que Productos/Políticas no
    /// necesitan, porque aquí sí es posible que un administrador se
    /// bloquee a sí mismo por accidente:
    ///   1) Un usuario no puede desactivarse a sí mismo.
    ///   2) Un rol no se puede desactivar (ni perder su permiso de
    ///      AdministrarUsuarios) mientras todavía tenga usuarios activos
    ///      asignados, para no dejar cuentas "huérfanas" sin ninguna
    ///      pantalla de administración que las pueda recuperar.
    /// </summary>
    public class AdminUsuariosService
    {
        private readonly UsuarioRepository _usuarioRepository;
        private readonly RolRepository _rolRepository;

        public AdminUsuariosService() : this(new UsuarioRepository(), new RolRepository()) { }

        public AdminUsuariosService(UsuarioRepository usuarioRepository, RolRepository rolRepository)
        {
            _usuarioRepository = usuarioRepository;
            _rolRepository = rolRepository;
        }

        // ==================== Usuarios ====================

        public List<Usuario> ListarUsuarios()
        {
            return _usuarioRepository.ObtenerTodosAdmin();
        }

        public Usuario ObtenerUsuario(int usuarioId)
        {
            return _usuarioRepository.ObtenerPorIdAdmin(usuarioId);
        }

        /// <summary>
        /// Crea o actualiza un usuario. Si <paramref name="passwordEnClaro"/> viene vacío
        /// y el usuario ya existe, la contraseña actual no se toca (permite editar
        /// email/nombre/rol/estado sin forzar un reseteo). En un usuario nuevo la
        /// contraseña es obligatoria.
        /// </summary>
        public ResultadoGuardado GuardarUsuario(Usuario usuario, string passwordEnClaro, int usuarioIdSesion)
        {
            bool esNuevo = usuario.UsuarioId <= 0;

            if (!esNuevo && !usuario.Activo && usuario.UsuarioId == usuarioIdSesion)
            {
                return ResultadoGuardado.Error("No puedes desactivar tu propio usuario. Pide a otro administrador que lo haga.");
            }

            string error = ValidarUsuario(usuario, esNuevo, passwordEnClaro);
            if (error != null) return ResultadoGuardado.Error(error);

            usuario.Email = usuario.Email.Trim().ToLowerInvariant();
            usuario.NombreCompleto = usuario.NombreCompleto.Trim();

            try
            {
                if (esNuevo)
                {
                    string hash, salt;
                    PasswordHasher.CrearHash(passwordEnClaro, out hash, out salt);
                    usuario.PasswordHash = hash;
                    usuario.PasswordSalt = salt;

                    int id = _usuarioRepository.Crear(usuario);
                    return ResultadoGuardado.Ok(id);
                }

                _usuarioRepository.Actualizar(usuario);

                if (!string.IsNullOrEmpty(passwordEnClaro))
                {
                    string hash, salt;
                    PasswordHasher.CrearHash(passwordEnClaro, out hash, out salt);
                    _usuarioRepository.ActualizarPassword(usuario.UsuarioId, hash, salt);
                }

                return ResultadoGuardado.Ok(usuario.UsuarioId);
            }
            catch (SqlException ex) when (EsViolacionDeCodigoDuplicado(ex, "UQ_Usuarios_Email"))
            {
                return ResultadoGuardado.Error($"Ya existe un usuario con el correo '{usuario.Email}'.");
            }
        }

        /// <summary>
        /// Activa o desactiva un usuario. <paramref name="usuarioIdSesion"/> es quien
        /// ejecuta la acción: no puede desactivarse a sí mismo (evita que un
        /// administrador se bloquee por accidente y se quede sin poder revertirlo).
        /// </summary>
        public ResultadoGuardado CambiarEstadoUsuario(int usuarioId, bool activo, int usuarioIdSesion)
        {
            if (!activo && usuarioId == usuarioIdSesion)
            {
                return ResultadoGuardado.Error("No puedes desactivar tu propio usuario. Pide a otro administrador que lo haga.");
            }

            Usuario usuario = _usuarioRepository.ObtenerPorIdAdmin(usuarioId);
            if (usuario == null) return ResultadoGuardado.Error("El usuario ya no existe.");

            _usuarioRepository.CambiarEstado(usuarioId, activo);
            return ResultadoGuardado.Ok(usuarioId);
        }

        /// <summary>
        /// Desbloquea una cuenta que llegó al máximo de intentos fallidos de
        /// inicio de sesión (5), sin cambiarle la contraseña. Reutiliza
        /// sp_Usuarios_CambiarEstado, que al dejar el usuario activo también
        /// pone IntentosFallidos en 0 (no requiere un script nuevo en la base).
        /// </summary>
        public ResultadoGuardado DesbloquearUsuario(int usuarioId)
        {
            Usuario usuario = _usuarioRepository.ObtenerPorIdAdmin(usuarioId);
            if (usuario == null) return ResultadoGuardado.Error("El usuario ya no existe.");
            if (!usuario.Activo) return ResultadoGuardado.Error("El usuario está inactivo, no bloqueado. Usa \"Activar\".");

            _usuarioRepository.CambiarEstado(usuarioId, true);
            return ResultadoGuardado.Ok(usuarioId);
        }

        private string ValidarUsuario(Usuario u, bool esNuevo, string passwordEnClaro)
        {
            if (u == null) return "Datos de usuario inválidos.";
            if (string.IsNullOrWhiteSpace(u.Email)) return "El correo es obligatorio.";
            if (u.Email.Trim().Length > 150) return "El correo no puede superar 150 caracteres.";
            if (u.Email.IndexOf('@') < 1) return "El correo no es válido.";
            if (string.IsNullOrWhiteSpace(u.NombreCompleto)) return "El nombre completo es obligatorio.";
            if (u.NombreCompleto.Trim().Length > 200) return "El nombre no puede superar 200 caracteres.";
            if (u.RolId <= 0) return "Debe seleccionar un rol.";

            Rol rol = _rolRepository.ObtenerPorIdAdmin(u.RolId);
            if (rol == null) return "El rol seleccionado no existe.";
            if (!rol.Activo) return "El rol seleccionado está inactivo. Elige otro rol o reactívalo primero.";

            if (esNuevo && string.IsNullOrEmpty(passwordEnClaro))
            {
                return "La contraseña es obligatoria para un usuario nuevo.";
            }

            if (!string.IsNullOrEmpty(passwordEnClaro))
            {
                string mensajeError;
                if (!PasswordHasher.CumplePolitica(passwordEnClaro, out mensajeError)) return mensajeError;
            }

            return null;
        }

        // ==================== Roles ====================

        public List<Rol> ListarRoles()
        {
            return _rolRepository.ObtenerTodosAdmin();
        }

        public List<Rol> ListarRolesActivos()
        {
            return _rolRepository.ObtenerActivos();
        }

        public Rol ObtenerRol(int rolId)
        {
            return _rolRepository.ObtenerPorIdAdmin(rolId);
        }

        public ResultadoGuardado GuardarRol(Rol rol)
        {
            string error = ValidarRol(rol);
            if (error != null) return ResultadoGuardado.Error(error);

            // Si se le está quitando el permiso de administrar usuarios (o
            // desactivando) a un rol que ya tiene usuarios activos, esos
            // usuarios se quedarían sin ninguna pantalla desde la cual
            // recuperar el acceso. Se bloquea, igual que al desactivar
            // el rol directamente (ver CambiarEstadoRol).
            if (rol.RolId > 0)
            {
                Rol actual = _rolRepository.ObtenerPorIdAdmin(rol.RolId);
                bool perdioAdministracion = actual != null && actual.PuedeAdministrarUsuarios && !rol.PuedeAdministrarUsuarios;
                if ((perdioAdministracion || !rol.Activo) && actual != null && actual.Activo)
                {
                    int usuariosActivos = _rolRepository.ContarUsuariosActivos(rol.RolId);
                    if (usuariosActivos > 0)
                    {
                        return ResultadoGuardado.Error(
                            $"No se puede quitar el permiso de administrar usuarios (ni desactivar el rol): {usuariosActivos} usuario(s) activo(s) todavía lo tienen asignado. Reasígnalos a otro rol primero.");
                    }
                }
            }

            rol.Nombre = rol.Nombre.Trim();

            try
            {
                int id = _rolRepository.Guardar(rol);
                return ResultadoGuardado.Ok(id);
            }
            catch (SqlException ex) when (EsViolacionDeCodigoDuplicado(ex, "UQ_Roles_Nombre"))
            {
                return ResultadoGuardado.Error($"Ya existe un rol llamado '{rol.Nombre}'. Elige otro nombre.");
            }
        }

        /// <summary>Atajo usado desde el listado (botón Activar/Desactivar), sin pasar por el formulario completo.</summary>
        public ResultadoGuardado CambiarEstadoRol(int rolId, bool activo)
        {
            Rol rol = _rolRepository.ObtenerPorIdAdmin(rolId);
            if (rol == null) return ResultadoGuardado.Error("El rol ya no existe.");

            if (!activo)
            {
                int usuariosActivos = _rolRepository.ContarUsuariosActivos(rolId);
                if (usuariosActivos > 0)
                {
                    return ResultadoGuardado.Error(
                        $"No se puede desactivar: {usuariosActivos} usuario(s) activo(s) todavía tienen este rol asignado. Reasígnalos a otro rol primero.");
                }
            }

            rol.Activo = activo;
            _rolRepository.Guardar(rol);
            return ResultadoGuardado.Ok(rol.RolId);
        }

        private static string ValidarRol(Rol r)
        {
            if (r == null) return "Datos de rol inválidos.";
            if (string.IsNullOrWhiteSpace(r.Nombre)) return "El nombre del rol es obligatorio.";
            if (r.Nombre.Trim().Length > 50) return "El nombre no puede superar 50 caracteres.";
            if (!string.IsNullOrEmpty(r.Descripcion) && r.Descripcion.Length > 200) return "La descripción no puede superar 200 caracteres.";
            if (!r.PuedeEvaluar && !r.PuedeAdministrarCatalogo && !r.PuedeAdministrarUsuarios)
            {
                return "El rol debe tener al menos un permiso habilitado; de lo contrario, un usuario con ese rol solo podría ver el Dashboard.";
            }
            return null;
        }

        // ==================== Utilidades ====================

        private static bool EsViolacionDeCodigoDuplicado(SqlException ex, string nombreConstraint)
        {
            return (ex.Number == 2627 || ex.Number == 2601) &&
                   ex.Message.IndexOf(nombreConstraint, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
