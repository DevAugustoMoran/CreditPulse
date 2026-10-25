using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CreditPulse.Web.DAL;
using CreditPulse.Web.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CreditPulse.Web.BLL
{
    /// <summary>Resultado de una operación de guardado en el módulo de administración.</summary>
    public class ResultadoGuardado
    {
        public bool Exitoso { get; set; }
        public string MensajeError { get; set; }
        public int Id { get; set; }

        public static ResultadoGuardado Ok(int id)
        {
            return new ResultadoGuardado { Exitoso = true, Id = id };
        }

        public static ResultadoGuardado Error(string mensaje)
        {
            return new ResultadoGuardado { Exitoso = false, MensajeError = mensaje };
        }
    }

    /// <summary>
    /// Reglas de negocio del módulo de Administración (Productos y Políticas).
    /// Valida los datos capturados en las pantallas Administracion/*.aspx antes
    /// de delegar el guardado a la DAL, y traduce errores de SQL (por ejemplo,
    /// violaciones de código duplicado) a mensajes entendibles para el usuario.
    /// </summary>
    public class AdminCatalogoService
    {
        private readonly ProductoRepository _productoRepository;
        private readonly PoliticaRepository _politicaRepository;

        public AdminCatalogoService() : this(new ProductoRepository(), new PoliticaRepository()) { }

        public AdminCatalogoService(ProductoRepository productoRepository, PoliticaRepository politicaRepository)
        {
            _productoRepository = productoRepository;
            _politicaRepository = politicaRepository;
        }

        // ==================== Productos ====================

        public List<Producto> ListarProductos()
        {
            return _productoRepository.ObtenerTodosAdmin();
        }

        public Producto ObtenerProducto(int productoId)
        {
            return _productoRepository.ObtenerPorIdAdmin(productoId);
        }

        public ResultadoGuardado GuardarProducto(Producto producto)
        {
            string error = ValidarProducto(producto);
            if (error != null) return ResultadoGuardado.Error(error);

            producto.Codigo = producto.Codigo.Trim().ToUpperInvariant();
            producto.Nombre = producto.Nombre.Trim();

            try
            {
                int id = _productoRepository.Guardar(producto);
                return ResultadoGuardado.Ok(id);
            }
            catch (SqlException ex) when (EsViolacionDeCodigoDuplicado(ex, "UQ_Productos_Codigo"))
            {
                return ResultadoGuardado.Error($"Ya existe un producto con el código '{producto.Codigo}'. Elige otro código.");
            }
        }

        public ResultadoGuardado CambiarEstadoProducto(int productoId, bool activo)
        {
            var producto = _productoRepository.ObtenerPorIdAdmin(productoId);
            if (producto == null) return ResultadoGuardado.Error("El producto no existe.");

            producto.Activo = activo;
            _productoRepository.Guardar(producto);
            return ResultadoGuardado.Ok(producto.ProductoId);
        }

        private static string ValidarProducto(Producto p)
        {
            if (p == null) return "Datos de producto inválidos.";
            if (string.IsNullOrWhiteSpace(p.Codigo)) return "El código del producto es obligatorio.";
            if (p.Codigo.Trim().Length > 30) return "El código no puede superar 30 caracteres.";
            if (string.IsNullOrWhiteSpace(p.Nombre)) return "El nombre del producto es obligatorio.";
            if (p.Nombre.Trim().Length > 150) return "El nombre no puede superar 150 caracteres.";
            if (!string.IsNullOrEmpty(p.Descripcion) && p.Descripcion.Length > 400) return "La descripción no puede superar 400 caracteres.";
            return null;
        }

        // ==================== Políticas ====================

        public List<Politica> ListarPoliticas(int productoId)
        {
            return _politicaRepository.ObtenerTodosPorProducto(productoId);
        }

        public Politica ObtenerPolitica(int politicaId)
        {
            return _politicaRepository.ObtenerPorIdAdmin(politicaId);
        }

        public ResultadoGuardado GuardarPolitica(Politica politica)
        {
            string error = ValidarPolitica(politica);
            if (error != null) return ResultadoGuardado.Error(error);

            politica.Codigo = politica.Codigo.Trim().ToUpperInvariant();
            politica.Nombre = politica.Nombre.Trim();

            try
            {
                int id = _politicaRepository.Guardar(politica);
                return ResultadoGuardado.Ok(id);
            }
            catch (SqlException ex) when (EsViolacionDeCodigoDuplicado(ex, "UQ_Politicas_Producto_Codigo"))
            {
                return ResultadoGuardado.Error($"Ya existe una política con el código '{politica.Codigo}' para este producto. Elige otro código.");
            }
        }

        public ResultadoGuardado CambiarEstadoPolitica(int politicaId, bool activo)
        {
            var politica = _politicaRepository.ObtenerPorIdAdmin(politicaId);
            if (politica == null) return ResultadoGuardado.Error("La política no existe.");

            politica.Activo = activo;
            _politicaRepository.Guardar(politica);
            return ResultadoGuardado.Ok(politica.PoliticaId);
        }

        private static string ValidarPolitica(Politica p)
        {
            if (p == null) return "Datos de política inválidos.";
            if (p.ProductoId <= 0) return "Debe seleccionar un producto.";
            if (string.IsNullOrWhiteSpace(p.Codigo)) return "El código de la política es obligatorio.";
            if (p.Codigo.Trim().Length > 40) return "El código no puede superar 40 caracteres.";
            if (string.IsNullOrWhiteSpace(p.Nombre)) return "El nombre de la política es obligatorio.";
            if (p.Nombre.Trim().Length > 150) return "El nombre no puede superar 150 caracteres.";
            if (!string.IsNullOrEmpty(p.Descripcion) && p.Descripcion.Length > 400) return "La descripción no puede superar 400 caracteres.";

            if (string.IsNullOrWhiteSpace(p.TipoRegla)) return "Debe seleccionar un tipo de regla.";
            if (!TiposDeRegla.EsValido(p.TipoRegla)) return $"'{p.TipoRegla}' no es un tipo de regla reconocido por el motor de políticas.";

            if (string.IsNullOrWhiteSpace(p.ParametrosJson)) return "Los parámetros (JSON) son obligatorios. Ejemplo: " + EjemploParametrosPara(p.TipoRegla);
            if (p.ParametrosJson.Length > 500) return "Los parámetros no pueden superar 500 caracteres.";

            try
            {
                JObject.Parse(p.ParametrosJson);
            }
            catch (JsonReaderException)
            {
                return "Los parámetros no son un JSON válido. Ejemplo: " + EjemploParametrosPara(p.TipoRegla);
            }

            return null;
        }

        private static string EjemploParametrosPara(string tipoRegla)
        {
            foreach (var info in TiposDeRegla.Catalogo)
            {
                if (string.Equals(info.Codigo, tipoRegla, StringComparison.OrdinalIgnoreCase)) return info.EjemploParametros;
            }
            return "{}";
        }

        // ==================== Utilidades ====================

        /// <summary>
        /// SQL Server reporta 2627 (violación de PK/UNIQUE constraint) o 2601
        /// (violación de índice único) cuando se repite un código. Se revisa
        /// el nombre del constraint en el mensaje para armar un texto claro
        /// en español en vez de mostrar la excepción cruda al usuario.
        /// </summary>
        private static bool EsViolacionDeCodigoDuplicado(SqlException ex, string nombreConstraint)
        {
            return (ex.Number == 2627 || ex.Number == 2601) &&
                   ex.Message.IndexOf(nombreConstraint, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
