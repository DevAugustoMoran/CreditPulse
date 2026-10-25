<%@ Page Title="Detalle de Evaluación" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Resultado.aspx.cs" Inherits="CreditPulse.Web.Evaluaciones.Resultado" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1>Detalle de Evaluación <asp:Literal ID="litEncabezadoNumero" runat="server" /></h1>
    <p class="cp-page-subtitle">Resultado del motor de políticas y datos consultados en la SIB.</p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="pnlNoEncontrado" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        No se encontró la evaluación solicitada.
    </asp:Panel>

    <asp:Panel ID="pnlContenido" runat="server">

        <section class="cp-card cp-result-header">
            <div class="cp-result-headline">
                <div>
                    <div class="cp-result-cliente"><asp:Literal ID="litCliente" runat="server" /></div>
                    <div class="cp-result-meta">
                        <asp:Literal ID="litProducto" runat="server" /> &middot;
                        Evaluación #<asp:Literal ID="litNumero" runat="server" /> &middot;
                        <asp:Literal ID="litFecha" runat="server" /> &middot;
                        Analista: <asp:Literal ID="litAnalista" runat="server" />
                    </div>
                </div>
                <div class="cp-result-badge-wrap">
                    <span id="badgeResultado" runat="server" class="badge badge-lg">-</span>
                </div>
            </div>
        </section>

        <section class="cp-card">
            <div class="cp-tabs">
                <button type="button" class="cp-tab cp-tab-active" data-tab="tab-politicas" onclick="cpSwitchTab(this)">Políticas Evaluadas</button>
                <button type="button" class="cp-tab" data-tab="tab-sib" onclick="cpSwitchTab(this)">Información SIB</button>
            </div>

            <div id="tab-politicas" class="cp-tab-panel cp-tab-panel-active">
                <div class="cp-table-wrap">
                    <asp:GridView ID="gvPoliticas" runat="server" AutoGenerateColumns="false" CssClass="cp-table" GridLines="None">
                    <Columns>
                        <asp:BoundField DataField="Orden" HeaderText="#" ItemStyle-Width="40" />
                        <asp:BoundField DataField="NombrePolitica" HeaderText="Política" />
                        <asp:TemplateField HeaderText="Resultado">
                            <ItemTemplate>
                                <span class='badge <%# ((CreditPulse.Web.Models.ResultadoSemaforo)Eval("Resultado")).CssClass() %>'>
                                    <%# ((CreditPulse.Web.Models.ResultadoSemaforo)Eval("Resultado")) %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="Detalle" HeaderText="Detalle" />
                    </Columns>
                </asp:GridView>
                </div>

                <div class="cp-hierarchy-note">
                    <strong>Regla de jerarquía:</strong> si alguna política resulta en Rojo, la evaluación general es Rojo;
                    si no hay Rojo pero alguna es Amarillo, la evaluación general es Amarillo; solo es Verde si todas las
                    políticas resultan en Verde.
                </div>
            </div>

            <div id="tab-sib" class="cp-tab-panel">

                <asp:Panel ID="pnlSibSinDatos" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
                    No se recibió información de la Superintendencia de Bancos para esta evaluación.
                </asp:Panel>

                <asp:Panel ID="pnlSibReporte" runat="server">
                    <p class="cp-card-subtitle">
                        Este es el reporte de historial crediticio tal como lo devolvió la SIB (simulada) para
                        <asp:Literal ID="litSibNombreCliente" runat="server" />, pensado para leerse sin conocimientos técnicos.
                        El detalle técnico (XML) queda disponible más abajo para auditoría.
                    </p>

                    <div class="cp-sib-score-row">
                        <div class="cp-sib-score-card">
                            <span class="cp-hint" style="text-transform:uppercase; letter-spacing:.04em; font-size:.76rem;">Score de Buró</span>
                            <div class="cp-stat-value"><asp:Literal ID="litSibScore" runat="server" /></div>
                            <div class="cp-hero-meter"><div id="meterScore" runat="server" class="cp-hero-meter-fill"></div></div>
                            <span class="cp-hint"><asp:Literal ID="litSibScoreTexto" runat="server" /></span>
                        </div>
                        <div class="cp-kv"><span>Nivel de Endeudamiento</span><strong><asp:Literal ID="litSibEndeudamiento" runat="server" /></strong></div>
                        <div class="cp-kv"><span>Ingresos Reportados a la SIB</span><strong><asp:Literal ID="litSibIngresos" runat="server" /></strong></div>
                    </div>

                    <h3 class="cp-subheading">Historial Crediticio</h3>
                    <div class="cp-form-grid cp-form-grid-4">
                        <div class="cp-kv"><span>Créditos Activos</span><strong><asp:Literal ID="litCreditosActivos" runat="server" /></strong></div>
                        <div class="cp-kv"><span>Créditos Cancelados</span><strong><asp:Literal ID="litCreditosCancelados" runat="server" /></strong></div>
                        <div class="cp-kv"><span>Moras (últimos 12 meses)</span><strong><asp:Literal ID="litMoras" runat="server" /></strong></div>
                        <div class="cp-kv"><span>Días de Mora (máximo)</span><strong><asp:Literal ID="litDiasMora" runat="server" /></strong></div>
                    </div>

                    <h3 class="cp-subheading">Listas de Riesgo</h3>
                    <div class="cp-form-grid cp-form-grid-3">
                        <div class="cp-kv"><span>Lista Negra Local</span><strong><asp:Literal ID="litListaLocal" runat="server" /></strong></div>
                        <div class="cp-kv"><span>Lista Negra Internacional</span><strong><asp:Literal ID="litListaInternacional" runat="server" /></strong></div>
                        <div class="cp-kv"><span>Lista OFAC</span><strong><asp:Literal ID="litListaOfac" runat="server" /></strong></div>
                    </div>

                    <h3 class="cp-subheading">Referencias Internas del Banco</h3>
                    <div class="cp-form-grid cp-form-grid-2">
                        <div class="cp-kv"><span>Cliente Existente</span><strong><asp:Literal ID="litClienteExistente" runat="server" /></strong></div>
                        <div class="cp-kv"><span>Créditos Previos en el Banco</span><strong><asp:Literal ID="litCreditosPrevios" runat="server" /></strong></div>
                    </div>

                    <h3 class="cp-subheading">Metadatos de la Consulta</h3>
                    <div class="cp-form-grid cp-form-grid-3">
                        <div class="cp-kv"><span>Fecha de Consulta</span><strong><asp:Literal ID="litFechaConsulta" runat="server" /></strong></div>
                        <div class="cp-kv"><span>Fuente Consultada</span><strong><asp:Literal ID="litFuenteConsultada" runat="server" /></strong></div>
                        <div class="cp-kv"><span>Tiempo de Respuesta</span><strong><asp:Literal ID="litTiempoRespuesta" runat="server" /></strong></div>
                    </div>
                </asp:Panel>

                <div class="cp-sib-tecnico">
                    <button type="button" class="cp-sib-tecnico-toggle" onclick="cpToggleTecnico(this)">Ver XML técnico (auditoría)</button>
                    <pre class="cp-xml-box" id="boxXmlTecnico" style="display:none;"><asp:Literal ID="litSibXml" runat="server" /></pre>
                </div>
            </div>
        </section>

        <div class="cp-form-actions">
            <asp:HyperLink runat="server" NavigateUrl="~/Default.aspx" CssClass="cp-btn-secondary">Volver al Dashboard</asp:HyperLink>
            <asp:HyperLink runat="server" NavigateUrl="~/Evaluacion/Nueva.aspx" CssClass="cp-btn-primary">+ Nueva Evaluación</asp:HyperLink>
        </div>
    </asp:Panel>

    <script type="text/javascript">
        function cpSwitchTab(button) {
            var tabs = document.querySelectorAll('.cp-tab');
            var panels = document.querySelectorAll('.cp-tab-panel');
            for (var i = 0; i < tabs.length; i++) { tabs[i].classList.remove('cp-tab-active'); }
            for (var j = 0; j < panels.length; j++) { panels[j].classList.remove('cp-tab-panel-active'); }
            button.classList.add('cp-tab-active');
            document.getElementById(button.getAttribute('data-tab')).classList.add('cp-tab-panel-active');
        }

        function cpToggleTecnico(button) {
            var box = document.getElementById('boxXmlTecnico');
            if (!box) return;
            var visible = box.style.display !== 'none';
            box.style.display = visible ? 'none' : 'block';
            button.textContent = visible ? 'Ver XML técnico (auditoría)' : 'Ocultar XML técnico';
        }
    </script>

</asp:Content>
