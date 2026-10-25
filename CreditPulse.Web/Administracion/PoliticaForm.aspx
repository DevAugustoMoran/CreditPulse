<%@ Page Title="Política" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PoliticaForm.aspx.cs" Inherits="CreditPulse.Web.Administracion.PoliticaForm" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1><asp:Literal ID="litTitulo" runat="server">Nueva Política</asp:Literal></h1>
    <p class="cp-page-subtitle">Producto: <asp:Literal ID="litNombreProducto" runat="server" /></p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <asp:ScriptManager ID="scriptManager" runat="server" EnablePartialRendering="true" />
    <asp:HiddenField ID="hfProductoId" runat="server" />

    <asp:Panel ID="pnlMensajeError" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        <asp:Literal ID="litMensajeError" runat="server" />
    </asp:Panel>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Datos de la Política</h2>
            </div>
        </div>

        <div class="cp-form-grid cp-form-grid-3">
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtCodigo">Código *</asp:Label>
                <asp:TextBox ID="txtCodigo" runat="server" CssClass="cp-input" placeholder="EDAD_MINIMA" MaxLength="40" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCodigo" CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Requerido." ValidationGroup="Politica" />
            </div>
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtNombre">Nombre *</asp:Label>
                <asp:TextBox ID="txtNombre" runat="server" CssClass="cp-input" placeholder="Edad Mínima" MaxLength="150" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre" CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Requerido." ValidationGroup="Politica" />
            </div>
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtOrden">Orden</asp:Label>
                <asp:TextBox ID="txtOrden" runat="server" CssClass="cp-input" TextMode="Number" placeholder="0" />
                <span class="cp-hint">Define el orden en que aparece en el reporte.</span>
            </div>
        </div>

        <div class="cp-form-grid cp-form-grid-1">
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtDescripcion">Descripción</asp:Label>
                <asp:TextBox ID="txtDescripcion" runat="server" CssClass="cp-input" TextMode="MultiLine" Rows="2" MaxLength="400" />
            </div>
        </div>
    </section>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Regla del Motor</h2>
                <p class="cp-card-subtitle">El PolicyEngine interpreta el tipo de regla y los parámetros (JSON) para calcular el semáforo de esta política.</p>
            </div>
        </div>

        <div class="cp-form-grid cp-form-grid-1">
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="ddlTipoRegla">Tipo de Regla *</asp:Label>
                <asp:DropDownList ID="ddlTipoRegla" runat="server" CssClass="cp-input" AutoPostBack="true"
                    OnSelectedIndexChanged="ddlTipoRegla_SelectedIndexChanged" AppendDataBoundItems="true">
                    <asp:ListItem Text="-- Selecciona un tipo de regla --" Value="" />
                </asp:DropDownList>
                <asp:RequiredFieldValidator runat="server" ControlToValidate="ddlTipoRegla" InitialValue=""
                    CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Selecciona un tipo de regla." ValidationGroup="Politica" />
            </div>
        </div>

        <asp:UpdatePanel ID="upParametros" runat="server">
            <ContentTemplate>
                <div class="cp-form-grid cp-form-grid-1">
                    <div class="cp-field">
                        <asp:Label runat="server" AssociatedControlID="txtParametrosJson">Parámetros (JSON) *</asp:Label>
                        <asp:TextBox ID="txtParametrosJson" runat="server" CssClass="cp-input" TextMode="MultiLine" Rows="2" MaxLength="500" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtParametrosJson" CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Requerido." ValidationGroup="Politica" />
                        <span class="cp-hint">Ejemplo para el tipo seleccionado: <asp:Literal ID="litEjemploParametros" runat="server" /></span>
                    </div>
                </div>
            </ContentTemplate>
        </asp:UpdatePanel>
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
                <asp:Label runat="server" AssociatedControlID="chkActivo">Política activa (el motor la evalúa)</asp:Label>
            </div>
        </div>
    </section>

    <div class="cp-form-actions">
        <asp:HyperLink ID="hlCancelar" runat="server" CssClass="cp-btn-secondary">Cancelar</asp:HyperLink>
        <asp:Button ID="btnGuardar" runat="server" Text="Guardar Política" CssClass="cp-btn-primary" OnClick="btnGuardar_Click" ValidationGroup="Politica" />
    </div>

</asp:Content>
