<%@ Page Title="Producto" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ProductoForm.aspx.cs" Inherits="CreditPulse.Web.Administracion.ProductoForm" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1><asp:Literal ID="litTitulo" runat="server">Nuevo Producto</asp:Literal></h1>
    <p class="cp-page-subtitle">Define el producto bancario y los datos adicionales que pedirá el formulario de evaluación.</p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="pnlMensajeError" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        <asp:Literal ID="litMensajeError" runat="server" />
    </asp:Panel>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Datos del Producto</h2>
            </div>
        </div>

        <div class="cp-form-grid cp-form-grid-2">
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtCodigo">Código *</asp:Label>
                <asp:TextBox ID="txtCodigo" runat="server" CssClass="cp-input" placeholder="TARJETA_CREDITO" MaxLength="30" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCodigo" CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Requerido." ValidationGroup="Producto" />
                <span class="cp-hint">Identificador único, sin espacios. Se usa internamente y no lo ve el cliente.</span>
            </div>
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtNombre">Nombre *</asp:Label>
                <asp:TextBox ID="txtNombre" runat="server" CssClass="cp-input" placeholder="Tarjeta de Crédito" MaxLength="150" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre" CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Requerido." ValidationGroup="Producto" />
            </div>
        </div>

        <div class="cp-form-grid cp-form-grid-1">
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtDescripcion">Descripción</asp:Label>
                <asp:TextBox ID="txtDescripcion" runat="server" CssClass="cp-input" TextMode="MultiLine" Rows="3" MaxLength="400" />
            </div>
        </div>
    </section>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Datos Adicionales del Formulario de Evaluación</h2>
                <p class="cp-card-subtitle">Marca qué información extra debe capturar Nueva Evaluación cuando se elige este producto.</p>
            </div>
        </div>

        <div class="cp-checkbox-group">
            <div class="cp-checkbox-field">
                <asp:CheckBox ID="chkRequiereIngresos" runat="server" />
                <asp:Label runat="server" AssociatedControlID="chkRequiereIngresos">Requiere Ingresos Mensuales</asp:Label>
            </div>
            <div class="cp-checkbox-field">
                <asp:CheckBox ID="chkRequiereTipoEmpleo" runat="server" />
                <asp:Label runat="server" AssociatedControlID="chkRequiereTipoEmpleo">Requiere Tipo de Empleo</asp:Label>
            </div>
            <div class="cp-checkbox-field">
                <asp:CheckBox ID="chkRequiereAntiguedadLaboral" runat="server" />
                <asp:Label runat="server" AssociatedControlID="chkRequiereAntiguedadLaboral">Requiere Antigüedad Laboral</asp:Label>
            </div>
        </div>
    </section>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Estado</h2>
            </div>
        </div>
        <div class="cp-checkbox-group">
            <div class="cp-checkbox-field">
                <asp:CheckBox ID="chkActivo" runat="server" Checked="true" />
                <asp:Label runat="server" AssociatedControlID="chkActivo">Producto activo (visible para nuevas evaluaciones)</asp:Label>
            </div>
        </div>
    </section>

    <div class="cp-form-actions">
        <asp:HyperLink runat="server" NavigateUrl="~/Administracion/Productos.aspx" CssClass="cp-btn-secondary">Cancelar</asp:HyperLink>
        <asp:Button ID="btnGuardar" runat="server" Text="Guardar Producto" CssClass="cp-btn-primary" OnClick="btnGuardar_Click" ValidationGroup="Producto" />
    </div>

</asp:Content>
