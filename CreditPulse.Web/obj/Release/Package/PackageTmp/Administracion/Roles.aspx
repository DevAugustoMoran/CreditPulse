<%@ Page Title="Roles" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Roles.aspx.cs" Inherits="CreditPulse.Web.Administracion.Roles" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1>Roles</h1>
    <p class="cp-page-subtitle">Qué pantallas puede usar cada rol. Un usuario hereda los permisos del rol que tiene asignado.</p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="pnlMensajeError" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        <asp:Literal ID="litMensajeError" runat="server" />
    </asp:Panel>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Roles configurados</h2>
                <p class="cp-card-subtitle">Un rol inactivo no se puede asignar a usuarios nuevos. No se puede desactivar (ni quitarle "Administrar Usuarios") mientras tenga usuarios activos.</p>
            </div>
            <div class="cp-card-header-actions">
                <a class="cp-btn-secondary" href="<%: ResolveUrl("~/Administracion/Usuarios.aspx") %>">Ver Usuarios</a>
                <a class="cp-btn-primary" href="<%: ResolveUrl("~/Administracion/RolForm.aspx") %>">+ Nuevo Rol</a>
            </div>
        </div>

        <div class="cp-table-wrap">
            <asp:GridView ID="gvRoles" runat="server" AutoGenerateColumns="false" CssClass="cp-table" GridLines="None"
            DataKeyNames="RolId" OnRowCommand="gvRoles_RowCommand">
            <Columns>
                <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
                <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
                <asp:TemplateField HeaderText="Permisos">
                    <ItemTemplate>
                        <div class="cp-tag-list">
                            <asp:Literal runat="server" Visible='<%# (bool)Eval("PuedeEvaluar") %>' Text="<span class='cp-tag'>Evaluar</span>" />
                            <asp:Literal runat="server" Visible='<%# (bool)Eval("PuedeAdministrarCatalogo") %>' Text="<span class='cp-tag'>Administrar Catálogo</span>" />
                            <asp:Literal runat="server" Visible='<%# (bool)Eval("PuedeAdministrarUsuarios") %>' Text="<span class='cp-tag'>Administrar Usuarios</span>" />
                        </div>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="TotalUsuariosActivos" HeaderText="Usuarios" ItemStyle-Width="90" />
                <asp:TemplateField HeaderText="Estado" ItemStyle-Width="110">
                    <ItemTemplate>
                        <span class='<%# (bool)Eval("Activo") ? "badge badge-verde" : "badge badge-neutro" %>'><%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %></span>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="">
                    <ItemTemplate>
                        <div class="cp-table-actions">
                            <asp:HyperLink runat="server" CssClass="cp-link" NavigateUrl='<%# "~/Administracion/RolForm.aspx?id=" + Eval("RolId") %>'>Editar</asp:HyperLink>
                            <asp:LinkButton runat="server" CssClass='<%# (bool)Eval("Activo") ? "cp-link cp-link-danger" : "cp-link" %>'
                                CommandName="ToggleEstado" CommandArgument='<%# Eval("RolId") %>' CausesValidation="false"
                                Text='<%# (bool)Eval("Activo") ? "Desactivar" : "Activar" %>' />
                        </div>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
            <EmptyDataTemplate>
                <div class="cp-empty-state">Todavía no hay roles configurados.</div>
            </EmptyDataTemplate>
        </asp:GridView>
        </div>
    </section>

</asp:Content>
