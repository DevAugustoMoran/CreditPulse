<%@ Page Title="Evaluaciones Batch" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Batch.aspx.cs" Inherits="CreditPulse.Web.Evaluaciones.Batch" %>

<asp:Content ID="ContentTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <h1>Evaluaciones Batch</h1>
    <p class="cp-page-subtitle">Precalifica varios clientes a la vez subiendo un archivo Excel.</p>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="pnlMensajeError" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
        <asp:Literal ID="litMensajeError" runat="server" />
    </asp:Panel>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>1. Descarga la plantilla</h2>
                <p class="cp-card-subtitle">
                    El archivo debe tener las columnas Primer Nombre, Segundo Nombre, Tercer Nombre (opcional),
                    Primer Apellido, Segundo Apellido, NIT, DPI y Fecha de Nacimiento. Todos los clientes se evalúan
                    contra el producto <strong>Precalificación General</strong>. El DPI de cada fila se valida
                    antes de consultar a la SIB (13 dígitos, departamento, municipio y dígito verificador);
                    una fila con DPI inválido se reporta con error y el resto del archivo continúa.
                </p>
            </div>
            <a class="cp-btn-secondary" href="<%: ResolveUrl("~/Content/Plantillas/PlantillaEvaluacionBatch.xlsx") %>">&#8681; Descargar plantilla (.xlsx)</a>
        </div>
    </section>

    <section class="cp-card">
        <div class="cp-card-header">
            <div>
                <h2>2. Sube tu archivo</h2>
                <p class="cp-card-subtitle">
                    Formato aceptado: .xlsx (Excel moderno). Tamaño máximo 10 MB. Archivos grandes (miles de filas)
                    se procesan en segundo plano: la pantalla siguiente muestra el avance en tiempo real y se
                    actualiza sola hasta que termina.
                </p>
            </div>
        </div>

        <div class="cp-field" style="max-width: 480px;">
            <asp:Label runat="server" AssociatedControlID="fileUploadExcel">Archivo Excel</asp:Label>
            <asp:FileUpload ID="fileUploadExcel" runat="server" CssClass="cp-input" />
        </div>

        <div class="cp-form-actions" style="justify-content: flex-start; margin-top: 18px;">
            <asp:Button ID="btnProcesar" runat="server" Text="Procesar Archivo" CssClass="cp-btn-primary" OnClick="btnProcesar_Click" />
        </div>
    </section>

</asp:Content>
