<%@ Page Title="Acceso Denegado" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AccesoDenegado.aspx.cs" Inherits="CreditPulse.Web.AccesoDenegado" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1>Acceso Denegado</h1>
    <p class="cp-page-subtitle">Tu rol no tiene permiso para ver esta pantalla.</p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>No tienes permiso para esta pantalla</h2>
                <p class="cp-card-subtitle">
                    Tu rol actual es <strong><asp:Literal ID="litRolActual" runat="server" /></strong>.
                    Si crees que deberías tener acceso, contacta a un administrador del sistema.
                </p>
            </div>
        </div>

        <div class="cp-form-actions" style="justify-content: flex-start;">
            <asp:HyperLink runat="server" NavigateUrl="~/Default.aspx" CssClass="cp-btn-primary">Volver al Dashboard</asp:HyperLink>
        </div>
    </section>

</asp:Content>
