<%@ Page Title="Productos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Productos.aspx.cs" Inherits="CreditPulse.Web.Administracion.Productos" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1>Productos</h1>
    <p class="cp-page-subtitle">Catálogo de productos bancarios evaluables y sus políticas asociadas.</p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="pnlMensajeError" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        <asp:Literal ID="litMensajeError" runat="server" />
    </asp:Panel>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Productos configurados</h2>
                <p class="cp-card-subtitle">Un producto inactivo no aparece en Nueva Evaluación ni en la plantilla Batch, pero conserva su historial.</p>
            </div>
            <a class="cp-btn-primary" href="<%: ResolveUrl("~/Administracion/ProductoForm.aspx") %>">+ Nuevo Producto</a>
        </div>

        <div class="cp-table-wrap">
            <asp:GridView ID="gvProductos" runat="server" AutoGenerateColumns="false" CssClass="cp-table" GridLines="None"
            DataKeyNames="ProductoId" OnRowCommand="gvProductos_RowCommand">
            <Columns>
                <asp:BoundField DataField="Codigo" HeaderText="Código" />
                <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
                <asp:BoundField DataField="TotalPoliticas" HeaderText="Políticas" ItemStyle-Width="90" />
                <asp:TemplateField HeaderText="Estado" ItemStyle-Width="110">
                    <ItemTemplate>
                        <span class='<%# (bool)Eval("Activo") ? "badge badge-verde" : "badge badge-neutro" %>'><%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %></span>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="">
                    <ItemTemplate>
                        <div class="cp-table-actions">
                            <asp:HyperLink runat="server" CssClass="cp-link" NavigateUrl='<%# "~/Administracion/ProductoForm.aspx?id=" + Eval("ProductoId") %>'>Editar</asp:HyperLink>
                            <asp:HyperLink runat="server" CssClass="cp-link" NavigateUrl='<%# "~/Administracion/Politicas.aspx?productoId=" + Eval("ProductoId") %>'>Políticas</asp:HyperLink>
                            <asp:LinkButton runat="server" CssClass='<%# (bool)Eval("Activo") ? "cp-link cp-link-danger" : "cp-link" %>'
                                CommandName="ToggleEstado" CommandArgument='<%# Eval("ProductoId") %>' CausesValidation="false"
                                Text='<%# (bool)Eval("Activo") ? "Desactivar" : "Activar" %>' />
                        </div>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
            <EmptyDataTemplate>
                <div class="cp-empty-state">Todavía no hay productos configurados.</div>
            </EmptyDataTemplate>
        </asp:GridView>
        </div>
    </section>

</asp:Content>
