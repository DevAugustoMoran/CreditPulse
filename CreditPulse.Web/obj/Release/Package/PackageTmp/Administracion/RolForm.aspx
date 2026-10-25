<%@ Page Title="Rol" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RolForm.aspx.cs" Inherits="CreditPulse.Web.Administracion.RolForm" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1><asp:Literal ID="litTitulo" runat="server">Nuevo Rol</asp:Literal></h1>
    <p class="cp-page-subtitle">Define el rol y qué pantallas podrán usar los usuarios que lo tengan asignado.</p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="pnlMensajeError" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        <asp:Literal ID="litMensajeError" runat="server" />
    </asp:Panel>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Datos del Rol</h2>
            </div>
        </div>

        <div class="cp-form-grid cp-form-grid-2">
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtNombre">Nombre *</asp:Label>
                <asp:TextBox ID="txtNombre" runat="server" CssClass="cp-input" placeholder="Supervisor" MaxLength="50" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre" CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Requerido." ValidationGroup="Rol" />
            </div>
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtDescripcion">Descripción</asp:Label>
                <asp:TextBox ID="txtDescripcion" runat="server" CssClass="cp-input" MaxLength="200" placeholder="Para qué se usa este rol" />
            </div>
        </div>
    </section>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Permisos</h2>
                <p class="cp-card-subtitle">El Dashboard y las pantallas de resultado ya evaluado (individual o por lote) los puede ver cualquier usuario autenticado, sin importar el rol.</p>
            </div>
        </div>

        <div class="cp-checkbox-group">
            <div class="cp-checkbox-field">
                <asp:CheckBox ID="chkPuedeEvaluar" runat="server" />
                <asp:Label runat="server" AssociatedControlID="chkPuedeEvaluar">Evaluar &mdash; Nueva Evaluación y Evaluaciones Batch</asp:Label>
            </div>
            <div class="cp-checkbox-field">
                <asp:CheckBox ID="chkPuedeAdministrarCatalogo" runat="server" />
                <asp:Label runat="server" AssociatedControlID="chkPuedeAdministrarCatalogo">Administrar Catálogo &mdash; Productos y Políticas</asp:Label>
            </div>
            <div class="cp-checkbox-field">
                <asp:CheckBox ID="chkPuedeAdministrarUsuarios" runat="server" />
                <asp:Label runat="server" AssociatedControlID="chkPuedeAdministrarUsuarios">Administrar Usuarios &mdash; Usuarios y Roles (esta pantalla)</asp:Label>
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
                <asp:Label runat="server" AssociatedControlID="chkActivo">Rol activo (se puede asignar a usuarios nuevos)</asp:Label>
            </div>
        </div>
    </section>

    <div class="cp-form-actions">
        <asp:HyperLink runat="server" NavigateUrl="~/Administracion/Roles.aspx" CssClass="cp-btn-secondary">Cancelar</asp:HyperLink>
        <asp:Button ID="btnGuardar" runat="server" Text="Guardar Rol" CssClass="cp-btn-primary" OnClick="btnGuardar_Click" ValidationGroup="Rol" />
    </div>

</asp:Content>
