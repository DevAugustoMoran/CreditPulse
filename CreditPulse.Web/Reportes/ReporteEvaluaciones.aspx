<%@ Page Title="Reporte de Evaluaciones" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ReporteEvaluaciones.aspx.cs" Inherits="CreditPulse.Web.Reportes.ReporteEvaluaciones" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1>Reporte de Evaluaciones</h1>
    <p class="cp-page-subtitle">Evaluaciones realizadas en un mes: color, cliente, score crediticio y fecha.</p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="pnlMensajeError" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        <asp:Literal ID="litMensajeError" runat="server" />
    </asp:Panel>

    <section class="cp-card cp-no-print">
        <div class="cp-card-header">
            <div>
                <h2>Filtros</h2>
                <p class="cp-card-subtitle">El mes se cuenta en hora de Guatemala (del día 1 a las 00:00 al último día a las 23:59).</p>
            </div>
        </div>

        <div class="cp-form-grid cp-form-grid-3">
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="ddlMes">Mes</asp:Label>
                <asp:DropDownList ID="ddlMes" runat="server" CssClass="cp-input" />
            </div>
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="ddlAnio">Año</asp:Label>
                <asp:DropDownList ID="ddlAnio" runat="server" CssClass="cp-input" />
            </div>
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="ddlColor">Color de la evaluación</asp:Label>
                <asp:DropDownList ID="ddlColor" runat="server" CssClass="cp-input">
                    <asp:ListItem Value="" Text="Todos los colores" />
                    <asp:ListItem Value="VERDE" Text="Verde (aprobadas)" />
                    <asp:ListItem Value="AMARILLO" Text="Amarillo (revisión manual)" />
                    <asp:ListItem Value="ROJO" Text="Rojo (rechazadas)" />
                </asp:DropDownList>
            </div>
        </div>

        <div class="cp-form-actions" style="justify-content: flex-start; margin: 14px 0 0;">
            <asp:Button ID="btnGenerar" runat="server" Text="Generar reporte" CssClass="cp-btn-primary" OnClick="btnGenerar_Click" />
            <asp:Button ID="btnExportarExcel" runat="server" Text="Descargar Excel" CssClass="cp-btn-secondary" OnClick="btnExportarExcel_Click" />
            <asp:Button ID="btnExportarPdf" runat="server" Text="Descargar PDF" CssClass="cp-btn-secondary" OnClick="btnExportarPdf_Click" />
            <asp:Button ID="btnExportar" runat="server" Text="Descargar CSV" CssClass="cp-btn-secondary" OnClick="btnExportar_Click" />
            <button type="button" class="cp-btn-secondary" onclick="window.print();">Imprimir</button>
        </div>
    </section>

    <asp:Panel ID="pnlResultado" runat="server">

        <h2 class="cp-report-title"><asp:Literal ID="litTitulo" runat="server" /></h2>

        <section class="cp-cards-row cp-cards-row-5">
            <div class="cp-stat-card">
                <div class="cp-stat-label">Total de evaluaciones</div>
                <div class="cp-stat-value"><asp:Literal ID="litTotal" runat="server" /></div>
            </div>
            <div class="cp-stat-card cp-stat-verde">
                <div class="cp-stat-label">Verde</div>
                <div class="cp-stat-value"><asp:Literal ID="litVerde" runat="server" /></div>
            </div>
            <div class="cp-stat-card cp-stat-amarillo">
                <div class="cp-stat-label">Amarillo</div>
                <div class="cp-stat-value"><asp:Literal ID="litAmarillo" runat="server" /></div>
            </div>
            <div class="cp-stat-card cp-stat-rojo">
                <div class="cp-stat-label">Rojo</div>
                <div class="cp-stat-value"><asp:Literal ID="litRojo" runat="server" /></div>
            </div>
            <div class="cp-stat-card">
                <div class="cp-stat-label">Score promedio (SIB)</div>
                <div class="cp-stat-value"><asp:Literal ID="litScorePromedio" runat="server" /></div>
            </div>
        </section>

        <section class="cp-card">
            <div class="cp-table-wrap">
                <asp:GridView ID="gvEvaluaciones" runat="server" AutoGenerateColumns="false" CssClass="cp-table" GridLines="None"
                    EmptyDataText="No hay evaluaciones en el período seleccionado.">
                    <Columns>
                        <asp:BoundField DataField="EvaluacionId" HeaderText="No. Evaluación" />
                        <asp:TemplateField HeaderText="Cliente">
                            <ItemTemplate><%# RenderCliente((CreditPulse.Web.Models.ReporteEvaluacionFila)Container.DataItem) %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="ProductoNombre" HeaderText="Producto" />
                        <asp:TemplateField HeaderText="Color">
                            <ItemTemplate><%# RenderColor((CreditPulse.Web.Models.ReporteEvaluacionFila)Container.DataItem) %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Score crediticio">
                            <ItemTemplate><%# RenderScore((CreditPulse.Web.Models.ReporteEvaluacionFila)Container.DataItem) %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Fecha de evaluación">
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

            <div class="cp-pager cp-no-print">
                <asp:Button ID="btnAnterior" runat="server" Text="&larr; Anterior" CssClass="cp-btn-secondary" OnClick="btnAnterior_Click" />
                <span class="cp-pager-info"><asp:Literal ID="litPagina" runat="server" /></span>
                <asp:Button ID="btnSiguiente" runat="server" Text="Siguiente &rarr;" CssClass="cp-btn-secondary" OnClick="btnSiguiente_Click" />
            </div>
        </section>
    </asp:Panel>

</asp:Content>
