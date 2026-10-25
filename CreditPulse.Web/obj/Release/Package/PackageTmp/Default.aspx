<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="CreditPulse.Web.Default" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1>Dashboard</h1>
    <p class="cp-page-subtitle">Resumen general de evaluaciones crediticias.</p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <section class="cp-cards-row">
        <div class="cp-stat-card">
            <div class="cp-stat-label">Total de evaluaciones</div>
            <div class="cp-stat-value"><asp:Literal ID="litTotal" runat="server" /></div>
        </div>
        <div class="cp-stat-card cp-stat-verde">
            <div class="cp-stat-label">Aprobadas (Verde)</div>
            <div class="cp-stat-value"><asp:Literal ID="litVerde" runat="server" /></div>
        </div>
        <div class="cp-stat-card cp-stat-amarillo">
            <div class="cp-stat-label">Revisión manual (Amarillo)</div>
            <div class="cp-stat-value"><asp:Literal ID="litAmarillo" runat="server" /></div>
        </div>
        <div class="cp-stat-card cp-stat-rojo">
            <div class="cp-stat-label">Rechazadas (Rojo)</div>
            <div class="cp-stat-value"><asp:Literal ID="litRojo" runat="server" /></div>
        </div>
    </section>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Buscar evaluación</h2>
                <p class="cp-card-subtitle">Recupera el detalle de una evaluación existente por su número.</p>
            </div>
        </div>
        <div class="cp-search-row">
            <asp:TextBox ID="txtNumeroBusqueda" runat="server" CssClass="cp-input" placeholder="Ej. 100234" />
            <asp:Button ID="btnBuscar" runat="server" Text="Buscar" CssClass="cp-btn-primary" OnClick="btnBuscar_Click" />
        </div>
        <asp:Literal ID="litBusquedaError" runat="server" />
    </section>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Cargas Batch recientes</h2>
                <p class="cp-card-subtitle">Últimos archivos procesados en Evaluaciones Batch.</p>
            </div>
            <asp:HyperLink ID="hlNuevaCargaBatch" runat="server" NavigateUrl="~/Evaluacion/Batch.aspx" CssClass="cp-btn-secondary">+ Nueva Carga Batch</asp:HyperLink>
        </div>

        <div class="cp-table-wrap">
            <asp:GridView ID="gvLotes" runat="server" AutoGenerateColumns="false" CssClass="cp-table"
            GridLines="None" EmptyDataText="Todavía no se ha procesado ninguna carga batch.">
            <Columns>
                <asp:BoundField DataField="LoteId" HeaderText="No. Lote" />
                <asp:BoundField DataField="NombreArchivo" HeaderText="Archivo" />
                <asp:TemplateField HeaderText="Estado">
                    <ItemTemplate>
                        <%# RenderEstadoLote((CreditPulse.Web.Models.Lote)Container.DataItem) %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="TotalRegistros" HeaderText="Registros" />
                <asp:BoundField DataField="TotalVerde" HeaderText="Verde" />
                <asp:BoundField DataField="TotalAmarillo" HeaderText="Amarillo" />
                <asp:BoundField DataField="TotalRojo" HeaderText="Rojo" />
                <asp:BoundField DataField="TotalErrores" HeaderText="Errores" />
                <asp:TemplateField HeaderText="Fecha">
                    <ItemTemplate><%# CreditPulse.Web.BLL.ZonaHoraria.Formato(Eval("FechaCreacion")) %></ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="">
                    <ItemTemplate>
                        <a href='<%# ResolveUrl("~/Evaluacion/LoteResultado.aspx?id=" + Eval("LoteId")) %>' class="cp-link">Ver detalle</a>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
        </div>
    </section>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Evaluaciones recientes</h2>
                <p class="cp-card-subtitle">Últimas evaluaciones registradas en el sistema.</p>
            </div>
            <asp:HyperLink ID="hlNuevaEvaluacion" runat="server" NavigateUrl="~/Evaluacion/Nueva.aspx" CssClass="cp-btn-primary">+ Nueva Evaluación</asp:HyperLink>
        </div>

        <div class="cp-table-wrap">
            <asp:GridView ID="gvRecientes" runat="server" AutoGenerateColumns="false" CssClass="cp-table"
            GridLines="None" EmptyDataText="Todavía no hay evaluaciones registradas.">
            <Columns>
                <asp:BoundField DataField="EvaluacionId" HeaderText="No. Evaluación" />
                <asp:BoundField DataField="ClienteNombre" HeaderText="Cliente" />
                <asp:BoundField DataField="ProductoNombre" HeaderText="Producto" />
                <asp:TemplateField HeaderText="Resultado">
                    <ItemTemplate>
                        <span class='badge <%# ((CreditPulse.Web.Models.ResultadoSemaforo)Eval("ResultadoGeneral")).CssClass() %>'>
                            <%# ((CreditPulse.Web.Models.ResultadoSemaforo)Eval("ResultadoGeneral")).Etiqueta() %>
                        </span>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Fecha">
                    <ItemTemplate><%# CreditPulse.Web.BLL.ZonaHoraria.Formato(Eval("FechaCreacion")) %></ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="">
                    <ItemTemplate>
                        <a href='<%# ResolveUrl("~/Evaluacion/Resultado.aspx?id=" + Eval("EvaluacionId")) %>' class="cp-link">Ver detalle</a>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
        </div>
    </section>

</asp:Content>
