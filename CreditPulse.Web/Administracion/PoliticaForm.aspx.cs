using System;
using CreditPulse.Web.BLL;
using CreditPulse.Web.DAL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Administracion
{
    public partial class PoliticaForm : BasePage
    {
        private readonly AdminCatalogoService _adminService = new AdminCatalogoService();
        private readonly ProductoRepository _productoRepository = new ProductoRepository();

        private int _politicaId;

        protected override Permiso? PermisoRequerido
        {
            get { return Permiso.AdministrarCatalogo; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            int.TryParse(Request.QueryString["id"], out _politicaId);

            if (!IsPostBack)
            {
                CargarCatalogoTipoRegla();

                Producto producto;

                if (_politicaId > 0)
                {
                    Politica politica = _adminService.ObtenerPolitica(_politicaId);
                    if (politica == null)
                    {
                        Response.Redirect("~/Administracion/Productos.aspx", false);
                        return;
                    }

                    producto = _productoRepository.ObtenerPorIdAdmin(politica.ProductoId);
                    if (producto == null)
                    {
                        Response.Redirect("~/Administracion/Productos.aspx", false);
                        return;
                    }

                    litTitulo.Text = "Editar Política";
                    hfProductoId.Value = politica.ProductoId.ToString();
                    txtCodigo.Text = politica.Codigo;
                    txtNombre.Text = politica.Nombre;
                    txtDescripcion.Text = politica.Descripcion;
                    txtOrden.Text = politica.Orden.ToString();
                    txtParametrosJson.Text = politica.ParametrosJson;
                    chkActivo.Checked = politica.Activo;
                    ddlTipoRegla.SelectedValue = politica.TipoRegla;
                }
                else
                {
                    int productoId;
                    int.TryParse(Request.QueryString["productoId"], out productoId);

                    producto = _productoRepository.ObtenerPorIdAdmin(productoId);
                    if (producto == null)
                    {
                        Response.Redirect("~/Administracion/Productos.aspx", false);
                        return;
                    }

                    litTitulo.Text = "Nueva Política";
                    hfProductoId.Value = productoId.ToString();
                    chkActivo.Checked = true;
                    txtOrden.Text = "0";
                }

                litNombreProducto.Text = Server.HtmlEncode(producto.Nombre);
                hlCancelar.NavigateUrl = ResolveUrl("~/Administracion/Politicas.aspx?productoId=" + producto.ProductoId);
                ActualizarEjemploParametros();
            }
        }

        private void CargarCatalogoTipoRegla()
        {
            ddlTipoRegla.DataSource = TiposDeRegla.Catalogo;
            ddlTipoRegla.DataTextField = "NombreAmigable";
            ddlTipoRegla.DataValueField = "Codigo";
            ddlTipoRegla.DataBind();
        }

        protected void ddlTipoRegla_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActualizarEjemploParametros();
        }

        private void ActualizarEjemploParametros()
        {
            string tipoRegla = ddlTipoRegla.SelectedValue;

            foreach (var info in TiposDeRegla.Catalogo)
            {
                if (string.Equals(info.Codigo, tipoRegla, StringComparison.OrdinalIgnoreCase))
                {
                    litEjemploParametros.Text = Server.HtmlEncode(info.EjemploParametros);
                    return;
                }
            }

            litEjemploParametros.Text = "--";
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            int productoId;
            if (!int.TryParse(hfProductoId.Value, out productoId) || productoId <= 0)
            {
                MostrarError("No se pudo determinar el producto de esta política. Vuelve a intentarlo desde la lista de productos.");
                return;
            }

            int orden;
            int.TryParse(txtOrden.Text, out orden);

            var politica = new Politica
            {
                PoliticaId = _politicaId,
                ProductoId = productoId,
                Codigo = txtCodigo.Text.Trim(),
                Nombre = txtNombre.Text.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(txtDescripcion.Text) ? null : txtDescripcion.Text.Trim(),
                TipoRegla = ddlTipoRegla.SelectedValue,
                ParametrosJson = txtParametrosJson.Text.Trim(),
                Orden = orden,
                Activo = chkActivo.Checked
            };

            ResultadoGuardado resultado = _adminService.GuardarPolitica(politica);
            if (!resultado.Exitoso)
            {
                MostrarError(resultado.MensajeError);
                return;
            }

            Response.Redirect("~/Administracion/Politicas.aspx?productoId=" + productoId, false);
        }

        private void MostrarError(string mensaje)
        {
            litMensajeError.Text = Server.HtmlEncode(mensaje);
            pnlMensajeError.Visible = true;
        }
    }
}
