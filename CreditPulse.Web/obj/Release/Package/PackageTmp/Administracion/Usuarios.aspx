<%@ Page Title="Usuarios" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Usuarios.aspx.cs" Inherits="CreditPulse.Web.Administracion.Usuarios" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1>Usuarios</h1>
    <p class="cp-page-subtitle">Cuentas que pueden iniciar sesión en CP Analítica y el rol que tiene cada una.</p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="pnlMensajeError" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        <asp:Literal ID="litMensajeError" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlMensajeOk" runat="server" CssClass="cp-alert cp-alert-success" Visible="false">
        <asp:Literal ID="litMensajeOk" runat="server" />
    </asp:Panel>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Usuarios del sistema</h2>
                <p class="cp-card-subtitle">Un usuario inactivo no puede iniciar sesión, pero conserva su historial de evaluaciones como analista. Tras 5 intentos fallidos la cuenta queda <strong>bloqueada</strong>: usa &laquo;Desbloquear&raquo; (o restablece su contraseña desde Editar).</p>
            </div>
            <div class="cp-card-header-actions">
                <a class="cp-btn-secondary" href="<%: ResolveUrl("~/Administracion/Roles.aspx") %>">Ver Roles</a>
                <a class="cp-btn-primary" href="<%: ResolveUrl("~/Administracion/UsuarioForm.aspx") %>">+ Nuevo Usuario</a>
            </div>
        </div>

        <div class="cp-table-wrap">
            <asp:GridView ID="gvUsuarios" runat="server" AutoGenerateColumns="false" CssClass="cp-table" GridLines="None"
            DataKeyNames="UsuarioId" OnRowCommand="gvUsuarios_RowCommand" OnRowDataBound="gvUsuarios_RowDataBound">
            <Columns>
                <asp:BoundField DataField="NombreCompleto" HeaderText="Nombre" />
                <asp:BoundField DataField="Email" HeaderText="Correo" />
                <asp:BoundField DataField="RolNombre" HeaderText="Rol" ItemStyle-Width="160" />
                <asp:TemplateField HeaderText="Estado" ItemStyle-Width="140">
                    <ItemTemplate>
                        <span id="badgeEstado" runat="server" class="badge">-</span>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Último acceso" ItemStyle-Width="150">
                    <ItemTemplate>
                        <%# Eval("UltimoLogin") == null ? "Nunca" : CreditPulse.Web.BLL.ZonaHoraria.Formato(Eval("UltimoLogin")) %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="">
                    <ItemTemplate>
                        <div class="cp-table-actions">
                            <asp:HyperLink runat="server" CssClass="cp-link" NavigateUrl='<%# "~/Administracion/UsuarioForm.aspx?id=" + Eval("UsuarioId") %>'>Editar</asp:HyperLink>
                            <asp:LinkButton runat="server" CssClass='<%# (bool)Eval("Activo") ? "cp-link cp-link-danger" : "cp-link" %>'
                                CommandName="ToggleEstado" CommandArgument='<%# Eval("UsuarioId") %>' CausesValidation="false"
                                Text='<%# (bool)Eval("Activo") ? "Desactivar" : "Activar" %>' />
                            <asp:LinkButton ID="lnkDesbloquear" runat="server" CssClass="cp-link cp-link-unlock" Visible="false"
                                CommandName="Desbloquear" CommandArgument='<%# Eval("UsuarioId") %>' CausesValidation="false"
                                Text="Desbloquear" />
                        </div>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
            <EmptyDataTemplate>
                <div class="cp-empty-state">Todavía no hay usuarios configurados.</div>
            </EmptyDataTemplate>
        </asp:GridView>
        </div>
    </section>

</asp:Content>
