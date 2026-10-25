<%@ Page Title="Nueva Evaluación" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Nueva.aspx.cs" Inherits="CreditPulse.Web.Evaluaciones.Nueva" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1>Nueva Evaluación</h1>
    <p class="cp-page-subtitle">Completa el formulario para iniciar el proceso de evaluación crediticia.</p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <asp:ScriptManager ID="scriptManager" runat="server" EnablePartialRendering="true" />

    <asp:Panel ID="pnlMensajeError" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        <asp:Literal ID="litMensajeError" runat="server" />
    </asp:Panel>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Información de la Evaluación</h2>
                <p class="cp-card-subtitle">Selecciona el producto que el cliente está solicitando.</p>
            </div>
        </div>

        <asp:UpdatePanel ID="upProducto" runat="server">
            <ContentTemplate>
                <div class="cp-form-grid cp-form-grid-1">
                    <div class="cp-field">
                        <asp:Label runat="server" AssociatedControlID="ddlProducto">Producto *</asp:Label>
                        <asp:DropDownList ID="ddlProducto" runat="server" CssClass="cp-input" AutoPostBack="true"
                            OnSelectedIndexChanged="ddlProducto_SelectedIndexChanged" AppendDataBoundItems="true">
                            <asp:ListItem Text="-- Selecciona un producto --" Value="" />
                        </asp:DropDownList>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="ddlProducto" InitialValue=""
                            CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Selecciona un producto." ValidationGroup="Evaluacion" />
                    </div>
                </div>
            </ContentTemplate>
        </asp:UpdatePanel>
    </section>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>Datos Personales</h2>
            </div>
        </div>

        <div class="cp-form-grid cp-form-grid-3">
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtPrimerNombre">Primer Nombre *</asp:Label>
                <asp:TextBox ID="txtPrimerNombre" runat="server" CssClass="cp-input" placeholder="María" MaxLength="100" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPrimerNombre" CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Requerido." ValidationGroup="Evaluacion" />
            </div>
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtSegundoNombre">Segundo Nombre</asp:Label>
                <asp:TextBox ID="txtSegundoNombre" runat="server" CssClass="cp-input" placeholder="Fernanda" MaxLength="100" />
            </div>
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtTercerNombre">Tercer Nombre</asp:Label>
                <asp:TextBox ID="txtTercerNombre" runat="server" CssClass="cp-input" placeholder="(si el cliente tiene tres nombres)" MaxLength="100" />
            </div>
        </div>

        <div class="cp-form-grid cp-form-grid-2">
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtPrimerApellido">Primer Apellido *</asp:Label>
                <asp:TextBox ID="txtPrimerApellido" runat="server" CssClass="cp-input" placeholder="García" MaxLength="100" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPrimerApellido" CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Requerido." ValidationGroup="Evaluacion" />
            </div>
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtSegundoApellido">Segundo Apellido</asp:Label>
                <asp:TextBox ID="txtSegundoApellido" runat="server" CssClass="cp-input" placeholder="López" MaxLength="100" />
            </div>
        </div>

        <div class="cp-form-grid cp-form-grid-3">
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtNit">NIT *</asp:Label>
                <asp:TextBox ID="txtNit" runat="server" CssClass="cp-input" placeholder="1234-5" MaxLength="20" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNit" CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Requerido." ValidationGroup="Evaluacion" />
            </div>
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtDpi">DPI *</asp:Label>
                <asp:TextBox ID="txtDpi" runat="server" CssClass="cp-input" placeholder="3478953870101" MaxLength="20" inputmode="numeric" autocomplete="off" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtDpi" CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Requerido." ValidationGroup="Evaluacion" />
                <asp:CustomValidator ID="cvDpi" runat="server" ControlToValidate="txtDpi" CssClass="cp-field-error" Display="Dynamic"
                    ClientValidationFunction="cpValidarDpi" OnServerValidate="cvDpi_ServerValidate"
                    ErrorMessage="DPI inválido." ValidationGroup="Evaluacion" />
                <span class="cp-hint">13 dígitos: registro (8) + verificador (1) + departamento (2) + municipio (2).</span>
            </div>
            <div class="cp-field">
                <asp:Label runat="server" AssociatedControlID="txtFechaNacimiento">Fecha de Nacimiento *</asp:Label>
                <asp:TextBox ID="txtFechaNacimiento" runat="server" CssClass="cp-input" TextMode="Date" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtFechaNacimiento" CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Requerido." ValidationGroup="Evaluacion" />
            </div>
        </div>
    </section>

    <asp:UpdatePanel ID="upDatosAdicionales" runat="server">
        <ContentTemplate>
            <asp:Panel ID="pnlDatosAdicionales" runat="server" CssClass="cp-card" Visible="false">
                <div class="cp-card-header">
                    <div>
                        <h2>Datos Adicionales</h2>
                        <p class="cp-card-subtitle">Información laboral y económica requerida por el producto seleccionado.</p>
                    </div>
                </div>

                <div class="cp-form-grid cp-form-grid-3">
                    <asp:Panel ID="pnlIngresos" runat="server" CssClass="cp-field">
                        <asp:Label runat="server" AssociatedControlID="txtIngresos">Ingresos Mensuales (Q) *</asp:Label>
                        <asp:TextBox ID="txtIngresos" runat="server" CssClass="cp-input" placeholder="8500.00" TextMode="Number" step="0.01" />
                    </asp:Panel>
                    <asp:Panel ID="pnlTipoEmpleo" runat="server" CssClass="cp-field">
                        <asp:Label runat="server" AssociatedControlID="txtTipoEmpleo">Tipo de Empleo *</asp:Label>
                        <asp:TextBox ID="txtTipoEmpleo" runat="server" CssClass="cp-input" placeholder="Desarrollador de Software" MaxLength="150" />
                    </asp:Panel>
                    <asp:Panel ID="pnlAntiguedad" runat="server" CssClass="cp-field">
                        <asp:Label runat="server" AssociatedControlID="txtAntiguedad">Antigüedad Laboral (meses) *</asp:Label>
                        <asp:TextBox ID="txtAntiguedad" runat="server" CssClass="cp-input" placeholder="36" TextMode="Number" />
                    </asp:Panel>
                </div>
            </asp:Panel>
        </ContentTemplate>
    </asp:UpdatePanel>

    <script type="text/javascript">
        // Misma validación que BLL/DpiValidator.cs (formato, departamento,
        // municipio y dígito verificador) para avisar antes de enviar el
        // formulario. El servidor la repite siempre: esta copia es solo comodidad.
        var cpDpiVerificarDigito = <%= CreditPulse.Web.BLL.DpiValidator.ValidarDigitoPorDefecto ? "true" : "false" %>;
        var cpDpiMunicipios = [17, 8, 16, 16, 13, 14, 19, 8, 24, 21, 9, 30, 33, 21, 8, 17, 14, 5, 11, 11, 7, 17];
        var cpDpiDepartamentos = ["Guatemala", "El Progreso", "Sacatepéquez", "Chimaltenango", "Escuintla", "Santa Rosa", "Sololá", "Totonicapán", "Quetzaltenango", "Suchitepéquez", "Retalhuleu", "San Marcos", "Huehuetenango", "Quiché", "Baja Verapaz", "Alta Verapaz", "Petén", "Izabal", "Zacapa", "Chiquimula", "Jalapa", "Jutiapa"];

        function cpMensajeDpi(valor) {
            var d = (valor || '').replace(/[\s-]/g, '');
            if (d.length === 0) return null;
            if (!/^[0-9]+$/.test(d)) return 'El DPI solo puede contener números.';
            if (d.length !== 13) return 'El DPI debe tener exactamente 13 dígitos (tiene ' + d.length + ').';
            var depto = parseInt(d.substr(9, 2), 10), muni = parseInt(d.substr(11, 2), 10);
            if (depto < 1 || depto > 22) return 'Los dígitos 10 y 11 del DPI (' + d.substr(9, 2) + ') no corresponden a ningún departamento (01 a 22).';
            var max = cpDpiMunicipios[depto - 1];
            if (muni < 1 || muni > max) return 'Los dígitos 12 y 13 del DPI (' + d.substr(11, 2) + ') no son un municipio válido de ' + cpDpiDepartamentos[depto - 1] + ' (01 a ' + max + ').';
            if (cpDpiVerificarDigito) {
                var total = 0;
                for (var i = 0; i < 8; i++) { total += parseInt(d.charAt(i), 10) * (i + 2); }
                if (total % 11 !== parseInt(d.charAt(8), 10)) return 'El dígito verificador del DPI (posición 9) no coincide con el número de registro. Revisa que esté bien escrito.';
            }
            return null;
        }

        function cpValidarDpi(source, args) {
            var mensaje = cpMensajeDpi(args.Value);
            args.IsValid = (mensaje === null);
            if (mensaje !== null) { source.errormessage = mensaje; source.innerHTML = mensaje; }
        }
    </script>

    <div class="cp-form-actions">
        <asp:HyperLink runat="server" NavigateUrl="~/Default.aspx" CssClass="cp-btn-secondary">Cancelar</asp:HyperLink>
        <asp:Button ID="btnEvaluar" runat="server" Text="Evaluar Cliente" CssClass="cp-btn-primary"
            OnClick="btnEvaluar_Click" ValidationGroup="Evaluacion" />
    </div>

</asp:Content>
