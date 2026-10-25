<%@ Page Title="Reporte de Batch" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ReporteBatch.aspx.cs" Inherits="CreditPulse.Web.Reportes.ReporteBatch" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1>Reporte de Clientes por Batch</h1>
    <p class="cp-page-subtitle">Lista de clientes evaluados en un batch, con la información recolectada de la SIB.</p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="pnlMensajeError" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        <asp:Literal ID="litMensajeError" runat="server" />
    </asp:Panel>

    <section class="cp-card cp-no-print">
        <div class="cp-card-header">
            <div>
                <h2>Selecciona el batch</h2>
                <p class="cp-card-subtitle">Cada carga de Evaluaciones Batch recibe un número de batch. Elígelo de la lista o escribe su número.</p>
            </div>
        </div>

        <div class="cp-form-grid cp-form-grid-2">
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="ddlLotes">Batches recientes</asp:Label>
                <asp:DropDownList ID="ddlLotes" runat="server" CssClass="cp-input" AutoPostBack="true" OnSelectedIndexChanged="ddlLotes_SelectedIndexChanged" />
            </div>
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtLote">Número de batch</asp:Label>
                <asp:TextBox ID="txtLote" runat="server" CssClass="cp-input" TextMode="Number" placeholder="Ej. 12" />
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

    <asp:Panel ID="pnlResultado" runat="server" Visible="false">

        <section class="cp-card">
            <div class="cp-result-headline">
                <div>
                    <div class="cp-result-cliente">Batch #<asp:Literal ID="litLoteNumero" runat="server" /></div>
                    <div class="cp-result-meta">
                        Archivo: <asp:Literal ID="litArchivo" runat="server" /> &middot;
                        Fecha de carga: <asp:Literal ID="litFechaLote" runat="server" /> &middot;
                        Analista: <asp:Literal ID="litAnalista" runat="server" />
                    </div>
                </div>
                <div><asp:Literal ID="litEstadoLote" runat="server" /></div>
            </div>
        </section>

        <section class="cp-cards-row cp-cards-row-5">
            <div class="cp-stat-card">
                <div class="cp-stat-label">Registros</div>
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
                <div class="cp-stat-label">Con error</div>
                <div class="cp-stat-value"><asp:Literal ID="litErrores" runat="server" /></div>
            </div>
        </section>

        <section class="cp-card">
            <div class="cp-table-wrap">
                <asp:GridView ID="gvBatch" runat="server" AutoGenerateColumns="false" CssClass="cp-table" GridLines="None"
                    EmptyDataText="Este batch todavía no tiene filas procesadas.">
                    <Columns>
                        <asp:TemplateField HeaderText="Batch">
                            <ItemTemplate>#<%# Eval("LoteId") %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="FilaExcel" HeaderText="Fila" />
                        <asp:TemplateField HeaderText="Cliente">
                            <ItemTemplate><%# RenderCliente((CreditPulse.Web.Models.ReporteBatchFila)Container.DataItem) %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Color">
                            <ItemTemplate><%# RenderColor((CreditPulse.Web.Models.ReporteBatchFila)Container.DataItem) %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Score SIB">
                            <ItemTemplate><%# RenderScore((CreditPulse.Web.Models.ReporteBatchFila)Container.DataItem) %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Endeudamiento">
                            <ItemTemplate><%# RenderEndeudamiento((CreditPulse.Web.Models.ReporteBatchFila)Container.DataItem) %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Ingresos SIB">
                            <ItemTemplate><%# RenderIngresos((CreditPulse.Web.Models.ReporteBatchFila)Container.DataItem) %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Créditos">
                            <ItemTemplate><%# RenderCreditos((CreditPulse.Web.Models.ReporteBatchFila)Container.DataItem) %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Moras (12 m)">
                            <ItemTemplate><%# RenderMoras((CreditPulse.Web.Models.ReporteBatchFila)Container.DataItem) %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Listas de riesgo">
                            <ItemTemplate><%# RenderListas((CreditPulse.Web.Models.ReporteBatchFila)Container.DataItem) %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Fecha de evaluación">
                            <ItemTemplate><%# RenderFecha((CreditPulse.Web.Models.ReporteBatchFila)Container.DataItem) %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="">
                            <ItemTemplate><%# RenderAccion((CreditPulse.Web.Models.ReporteBatchFila)Container.DataItem) %></ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>

            <p class="cp-hint cp-no-print">
                La exportación a CSV incluye además: historial completo (créditos cancelados, días de mora máximo),
                cliente existente en el banco, fuente y fecha de la consulta a la SIB.
            </p>

            <div class="cp-pager cp-no-print">
                <asp:Button ID="btnAnterior" runat="server" Text="&larr; Anterior" CssClass="cp-btn-secondary" OnClick="btnAnterior_Click" />
                <span class="cp-pager-info"><asp:Literal ID="litPagina" runat="server" /></span>
                <asp:Button ID="btnSiguiente" runat="server" Text="Siguiente &rarr;" CssClass="cp-btn-secondary" OnClick="btnSiguiente_Click" />
            </div>
        </section>
    </asp:Panel>

</asp:Content>
