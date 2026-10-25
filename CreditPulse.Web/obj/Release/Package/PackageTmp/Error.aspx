<%@ Page Title="Error" Language="C#" AutoEventWireup="true" CodeBehind="Error.aspx.cs" Inherits="CreditPulse.Web.ErrorPage" %>
<!DOCTYPE html>
<html lang="es">
<head runat="server">
    <meta charset="utf-8" />
    <title>CP Analítica &middot; Ocurrió un error</title>
    <script type="text/javascript">try { var t = localStorage.getItem('cp-theme'); document.documentElement.setAttribute('data-theme', t === 'light' ? 'light' : 'dark'); } catch (e) { }</script>
    <link rel="icon" type="image/x-icon" href="Content/img/favicon.ico" />
    <link rel="stylesheet" href="Content/site.css" />
</head>
<body class="cp-error-body">
    <form id="frmError" runat="server">
        <div class="cp-error-shell">
            <div class="cp-brand cp-brand-hero">
                <img class="cp-brand-logo" src="Content/img/logo-icono.png" alt="" width="46" height="46" />
                <span class="cp-brand-text">
                    <span class="cp-brand-name">CP Analítica</span>
                    <span class="cp-brand-tagline">Motor de riesgos</span>
                </span>
            </div>
            <h1>Algo salió mal</h1>
            <p>Ocurrió un error inesperado al procesar tu solicitud. Intenta nuevamente o contacta al administrador del sistema.</p>
            <a class="cp-btn-primary" href="Default.aspx">Volver al Dashboard</a>
        </div>
    </form>
</body>
</html>
