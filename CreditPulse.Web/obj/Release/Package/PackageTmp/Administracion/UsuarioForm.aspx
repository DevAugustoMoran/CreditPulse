<%@ Page Title="Usuario" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="UsuarioForm.aspx.cs" Inherits="CreditPulse.Web.Administracion.UsuarioForm" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1><asp:Literal ID="litTitulo" runat="server">Nuevo Usuario</asp:Literal></h1>
    <p class="cp-page-subtitle">Datos de acceso y rol asignado.</p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="pnlMensajeError" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        <asp:Literal ID="litMensajeError" runat="server" />
    </asp:Panel>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Datos del Usuario</h2>
            </div>
        </div>

        <div class="cp-form-grid cp-form-grid-2">
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtNombreCompleto">Nombre completo *</asp:Label>
                <asp:TextBox ID="txtNombreCompleto" runat="server" CssClass="cp-input" placeholder="Nombre y apellido" MaxLength="200" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombreCompleto" CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Requerido." ValidationGroup="Usuario" />
            </div>
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtEmail">Correo electrónico *</asp:Label>
                <asp:TextBox ID="txtEmail" runat="server" CssClass="cp-input" TextMode="Email" placeholder="nombre.usuario@creditpulse.com" MaxLength="150" autocomplete="off" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail" CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Requerido." ValidationGroup="Usuario" />
            </div>
        </div>

        <div class="cp-form-grid cp-form-grid-2">
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="ddlRol">Rol *</asp:Label>
                <asp:DropDownList ID="ddlRol" runat="server" CssClass="cp-input" AppendDataBoundItems="true">
                    <asp:ListItem Text="-- Selecciona un rol --" Value="" />
                </asp:DropDownList>
                <asp:RequiredFieldValidator runat="server" ControlToValidate="ddlRol" InitialValue=""
                    CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Selecciona un rol." ValidationGroup="Usuario" />
            </div>
            <div class="cp-field">
                <asp:Label runat="server">Estado</asp:Label>
                <div class="cp-checkbox-field" style="margin-top: 6px;">
                    <asp:CheckBox ID="chkActivo" runat="server" Checked="true" />
                    <asp:Label runat="server" AssociatedControlID="chkActivo">Usuario activo (puede iniciar sesión)</asp:Label>
                </div>
            </div>
        </div>
    </section>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Contraseña</h2>
                <p class="cp-card-subtitle" id="pContrasenaHint" runat="server">La contraseña debe tener 10+ caracteres, con letras, números y símbolos.</p>
            </div>
        </div>

        <div class="cp-form-grid cp-form-grid-2">
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtPassword"><asp:Literal ID="litLabelPassword" runat="server">Contraseña *</asp:Literal></asp:Label>
                <asp:TextBox ID="txtPassword" runat="server" CssClass="cp-input" TextMode="Password" autocomplete="new-password" />
            </div>
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtPasswordConfirmar">Confirmar contraseña</asp:Label>
                <asp:TextBox ID="txtPasswordConfirmar" runat="server" CssClass="cp-input" TextMode="Password" autocomplete="new-password" />
                <asp:CompareValidator runat="server" ControlToValidate="txtPasswordConfirmar" ControlToCompare="txtPassword"
                    CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Las contraseñas no coinciden." ValidationGroup="Usuario" />
            </div>
        </div>
    </section>

    <div class="cp-form-actions">
        <asp:HyperLink runat="server" NavigateUrl="~/Administracion/Usuarios.aspx" CssClass="cp-btn-secondary">Cancelar</asp:HyperLink>
        <asp:Button ID="btnGuardar" runat="server" Text="Guardar Usuario" CssClass="cp-btn-primary" OnClick="btnGuardar_Click" ValidationGroup="Usuario" />
    </div>

</asp:Content>
