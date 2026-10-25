using System;
using CreditPulse.Web.BLL;
using CreditPulse.Web.Models;

namespace CreditPulse.Web.Administracion
{
    public partial class ProductoForm : BasePage
    {
        private readonly AdminCatalogoService _adminService = new AdminCatalogoService();
        private int _productoId;

        protected override Permiso? PermisoRequerido
        {
            get { return Permiso.AdministrarCatalogo; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            int.TryParse(Request.QueryString["id"], out _productoId);

            if (!IsPostBack)
            {
                if (_productoId > 0)
                {
                    CargarProducto();
                }
                else
                {
                    litTitulo.Text = "Nuevo Producto";
                    chkActivo.Checked = true;
                }
            }
        }

        private void CargarProducto()
        {
            Producto producto = _adminService.ObtenerProducto(_productoId);
            if (producto == null)
            {
                Response.Redirect("~/Administracion/Productos.aspx", false);
                return;
            }

            litTitulo.Text = "Editar Producto";
            txtCodigo.Text = producto.Codigo;
            txtNombre.Text = producto.Nombre;
            txtDescripcion.Text = producto.Descripcion;
            chkRequiereIngresos.Checked = producto.RequiereIngresos;
            chkRequiereTipoEmpleo.Checked = producto.RequiereTipoEmpleo;
            chkRequiereAntiguedadLaboral.Checked = producto.RequiereAntiguedadLaboral;
            chkActivo.Checked = producto.Activo;
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var producto = new Producto
            {
                ProductoId = _productoId,
                Codigo = txtCodigo.Text.Trim(),
                Nombre = txtNombre.Text.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(txtDescripcion.Text) ? null : txtDescripcion.Text.Trim(),
                RequiereIngresos = chkRequiereIngresos.Checked,
                RequiereTipoEmpleo = chkRequiereTipoEmpleo.Checked,
                RequiereAntiguedadLaboral = chkRequiereAntiguedadLaboral.Checked,
                Activo = chkActivo.Checked
            };

            ResultadoGuardado resultado = _adminService.GuardarProducto(producto);
            if (!resultado.Exitoso)
            {
                MostrarError(resultado.MensajeError);
                return;
            }

            Response.Redirect("~/Administracion/Productos.aspx", false);
        }

        private void MostrarError(string mensaje)
        {
            litMensajeError.Text = Server.HtmlEncode(mensaje);
            pnlMensajeError.Visible = true;
        }
    }
}
