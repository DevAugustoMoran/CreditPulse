<%@ Page Title="Resultado del Lote" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="LoteResultado.aspx.cs" Inherits="CreditPulse.Web.Evaluaciones.LoteResultado" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="HeadContent" runat="server">
    <meta id="metaRefresh" runat="server" http-equiv="refresh" content="3" visible="false" />
</asp:Content>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1>Resultado del Lote <asp:Literal ID="litEncabezadoNumero" runat="server" /></h1>
    <p class="cp-page-subtitle"><asp:Literal ID="litNombreArchivo" runat="server" /></p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="pnlNoEncontrado" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        No se encontró el lote solicitado.
    </asp:Panel>

    <asp:Panel ID="pnlContenido" runat="server">

        <asp:Panel ID="pnlProcesando" runat="server" CssClass="cp-alert cp-alert-info" Visible="false">
            <strong>Procesando el archivo&hellip;</strong> <asp:Literal ID="litProgresoTexto" runat="server" />
            <div class="cp-progreso-bar"><div id="progresoFill" runat="server" class="cp-progreso-fill" style="width:0%;"></div></div>
            <p class="cp-hint" style="margin-top: 8px;">Esta página se actualiza sola cada pocos segundos. Puedes navegar a otra pantalla y volver más tarde: el reporte sigue aquí, con el avance guardado.</p>
        </asp:Panel>

        <asp:Panel ID="pnlErrorLote" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
            <strong>El proceso se interrumpió antes de terminar.</strong> <asp:Literal ID="litMensajeErrorLote" runat="server" />
            <p class="cp-hint" style="margin-top: 6px;">Las filas que ya se alcanzaron a procesar antes del error se guardaron y se muestran abajo; el resto del archivo no se evaluó.</p>
        </asp:Panel>

        <section class="cp-cards-row cp-cards-row-5">
            <div class="cp-stat-card">
                <div class="cp-stat-label">Total procesados</div>
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
            <div class="cp-stat-card cp-stat-rojo">
                <div class="cp-stat-label">Filas con error</div>
                <div class="cp-stat-value"><asp:Literal ID="litErrores" runat="server" /></div>
            </div>
        </section>

        <section class="cp-card">
            <div class="cp-card-header">
                <div>
                    <h2>Detalle por cliente</h2>
                    <p class="cp-card-subtitle">
                        Analista: <asp:Literal ID="litAnalista" runat="server" /> &middot;
                        <asp:Literal ID="litFecha" runat="server" />
                    </p>
                </div>
            </div>

            <div class="cp-table-wrap">
                <asp:GridView ID="gvDetalle" runat="server" AutoGenerateColumns="false" CssClass="cp-table" GridLines="None">
                <Columns>
                    <asp:BoundField DataField="FilaExcel" HeaderText="Fila" ItemStyle-Width="50" />
                    <asp:BoundField DataField="NombreCompletoOriginal" HeaderText="Cliente" />
                    <asp:BoundField DataField="NitOriginal" HeaderText="NIT" />
                    <asp:BoundField DataField="DpiOriginal" HeaderText="DPI" />
                    <asp:TemplateField HeaderText="Resultado">
                        <ItemTemplate>
                            <%# RenderResultado((CreditPulse.Web.Models.LoteEvaluacion)Container.DataItem) %>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="">
                        <ItemTemplate>
                            <%# RenderAccion((CreditPulse.Web.Models.LoteEvaluacion)Container.DataItem) %>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
            </div>
        </section>

        <div class="cp-form-actions">
            <asp:HyperLink runat="server" NavigateUrl="~/Default.aspx" CssClass="cp-btn-secondary">Volver al Dashboard</asp:HyperLink>
            <a class="cp-btn-secondary" href="<%: ResolveUrl("~/Reportes/ReporteBatch.aspx") %>?lote=<%: Server.UrlEncode(Request.QueryString["id"] ?? "") %>">Ver reporte del batch (SIB)</a>
            <asp:HyperLink ID="hlNuevaCargaBatch" runat="server" NavigateUrl="~/Evaluacion/Batch.aspx" CssClass="cp-btn-primary">+ Nueva Carga Batch</asp:HyperLink>
        </div>
    </asp:Panel>

</asp:Content>
