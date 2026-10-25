<%@ Page Title="Políticas" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Politicas.aspx.cs" Inherits="CreditPulse.Web.Administracion.Politicas" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1>Políticas de <asp:Literal ID="litNombreProducto" runat="server" /></h1>
    <p class="cp-page-subtitle">Reglas que evalúa el motor para este producto. El resultado general toma la peor severidad entre todas las políticas activas.</p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="pnlMensajeError" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        <asp:Literal ID="litMensajeError" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlNoEncontrado" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        No se encontró el producto solicitado.
    </asp:Panel>

    <asp:Panel ID="pnlContenido" runat="server">

        <section class="cp-card">
            <div class="cp-card-header">
                <div>
                    <h2>Políticas configuradas</h2>
                    <p class="cp-card-subtitle">Una política inactiva se conserva pero el motor no la evalúa.</p>
                </div>
                <asp:HyperLink ID="hlNuevaPolitica" runat="server" CssClass="cp-btn-primary">+ Nueva Política</asp:HyperLink>
            </div>

            <div class="cp-table-wrap">
                <asp:GridView ID="gvPoliticas" runat="server" AutoGenerateColumns="false" CssClass="cp-table" GridLines="None"
                DataKeyNames="PoliticaId" OnRowCommand="gvPoliticas_RowCommand">
                <Columns>
                    <asp:BoundField DataField="Orden" HeaderText="Orden" ItemStyle-Width="70" />
                    <asp:BoundField DataField="Codigo" HeaderText="Código" />
                    <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
                    <asp:TemplateField HeaderText="Tipo de Regla">
                        <ItemTemplate>
                            <%# RenderTipoRegla((string)Eval("TipoRegla")) %>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Estado" ItemStyle-Width="110">
                        <ItemTemplate>
                            <span class='<%# (bool)Eval("Activo") ? "badge badge-verde" : "badge badge-neutro" %>'><%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %></span>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="">
                        <ItemTemplate>
                            <div class="cp-table-actions">
                                <asp:HyperLink runat="server" CssClass="cp-link" NavigateUrl='<%# "~/Administracion/PoliticaForm.aspx?id=" + Eval("PoliticaId") %>'>Editar</asp:HyperLink>
                                <asp:LinkButton runat="server" CssClass='<%# (bool)Eval("Activo") ? "cp-link cp-link-danger" : "cp-link" %>'
                                    CommandName="ToggleEstado" CommandArgument='<%# Eval("PoliticaId") %>' CausesValidation="false"
                                    Text='<%# (bool)Eval("Activo") ? "Desactivar" : "Activar" %>' />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
                <EmptyDataTemplate>
                    <div class="cp-empty-state">Este producto todavía no tiene políticas configuradas.</div>
                </EmptyDataTemplate>
            </asp:GridView>
            </div>
        </section>

        <div class="cp-form-actions">
            <asp:HyperLink runat="server" NavigateUrl="~/Administracion/Productos.aspx" CssClass="cp-btn-secondary">Volver a Productos</asp:HyperLink>
        </div>

    </asp:Panel>

</asp:Content>
