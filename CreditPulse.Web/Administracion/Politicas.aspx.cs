using System;
using System.Web.UI.WebControls;
using CreditPulse.Web.BLL;
using CreditPulse.Web.DAL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Administracion
{
    public partial class Politicas : BasePage
    {
        private readonly AdminCatalogoService _adminService = new AdminCatalogoService();
        private readonly ProductoRepository _productoRepository = new ProductoRepository();
        private int _productoId;

        protected override Permiso? PermisoRequerido
        {
            get { return Permiso.AdministrarCatalogo; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!int.TryParse(Request.QueryString["productoId"], out _productoId))
            {
                MostrarNoEncontrado();
                return;
            }

            Producto producto = _productoRepository.ObtenerPorIdAdmin(_productoId);
            if (producto == null)
            {
                MostrarNoEncontrado();
                return;
            }

            litNombreProducto.Text = Server.HtmlEncode(producto.Nombre);
            hlNuevaPolitica.NavigateUrl = ResolveUrl("~/Administracion/PoliticaForm.aspx?productoId=" + _productoId);

            if (!IsPostBack)
            {
                CargarGrid();
            }
        }

        private void CargarGrid()
        {
            gvPoliticas.DataSource = _adminService.ListarPoliticas(_productoId);
            gvPoliticas.DataBind();
        }

        private void MostrarNoEncontrado()
        {
            pnlNoEncontrado.Visible = true;
            pnlContenido.Visible = false;
        }

        protected void gvPoliticas_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "ToggleEstado") return;

            int politicaId = Convert.ToInt32(e.CommandArgument);
            Politica politica = _adminService.ObtenerPolitica(politicaId);
            if (politica == null)
            {
                MostrarError("La política ya no existe.");
                CargarGrid();
                return;
            }

            ResultadoGuardado resultado = _adminService.CambiarEstadoPolitica(politicaId, !politica.Activo);
            if (!resultado.Exitoso)
            {
                MostrarError(resultado.MensajeError);
            }

            CargarGrid();
        }

        private void MostrarError(string mensaje)
        {
            litMensajeError.Text = Server.HtmlEncode(mensaje);
            pnlMensajeError.Visible = true;
        }

        /// <summary>Traduce el código de TipoRegla al nombre amigable del catálogo (BLL/Models/Enums.cs).</summary>
        protected string RenderTipoRegla(string tipoRegla)
        {
            foreach (var info in TiposDeRegla.Catalogo)
            {
                if (string.Equals(info.Codigo, tipoRegla, StringComparison.OrdinalIgnoreCase))
                {
                    return Server.HtmlEncode(info.NombreAmigable);
                }
            }

            return Server.HtmlEncode(tipoRegla);
        }
    }
}
