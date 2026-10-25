<%@ Page Title="Ingresar" Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="CreditPulse.Web.Login" %>
<!DOCTYPE html>
<html lang="es">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>CP Analítica &middot; Acceso al sistema</title>
    <script type="text/javascript">try { var t = localStorage.getItem('cp-theme'); document.documentElement.setAttribute('data-theme', t === 'light' ? 'light' : 'dark'); } catch (e) { }</script>
    <link rel="icon" type="image/x-icon" href="Content/img/favicon.ico" />
    <link rel="icon" type="image/png" sizes="64x64" href="Content/img/favicon-64.png" />
    <link rel="apple-touch-icon" href="Content/img/apple-touch-icon.png" />
    <link rel="stylesheet" href="Content/site.css" />
</head>
<body class="cp-login-body">
    <form id="frmLogin" runat="server">
        <button type="button" class="cp-theme-toggle cp-theme-toggle-floating" data-cp-theme-toggle aria-label="Cambiar entre modo claro y oscuro" title="Cambiar a modo claro">
            <svg class="cp-icon-sun" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="4" /><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" /></svg>
            <svg class="cp-icon-moon" viewBox="0 0 24 24" aria-hidden="true"><path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z" /></svg>
        </button>
        <div class="cp-login-shell">

            <section class="cp-login-hero">
                <img class="cp-hero-logo" src="Content/img/logo-cp-analitica.jpg" alt="CP Analítica - Motor de riesgos" width="210" />

                <h1>Decisiones crediticias<br />más claras, en segundos.</h1>
                <p>
                    Evalúa el perfil de cada solicitante contra las políticas de
                    cada producto y obtén un veredicto por semáforo: Verde,
                    Amarillo o Rojo, respaldado por la información de la
                    Superintendencia de Bancos.
                </p>

                <div class="cp-hero-card">
                    <div class="cp-hero-card-label">Resultado de ejemplo</div>
                    <div class="cp-hero-card-row">
                        <span class="badge badge-verde">Aprobado</span>
                        <span class="cp-hero-card-score">Score SIB 742</span>
                    </div>
                    <div class="cp-hero-meter">
                        <div class="cp-hero-meter-fill" style="width: 87%;"></div>
                    </div>
                </div>
            </section>

            <section class="cp-login-panel">
                <div class="cp-login-card">
                    <div class="cp-login-eyebrow">Acceso al sistema</div>
                    <h2>Bienvenido de nuevo</h2>
                    <p class="cp-login-sub">Ingresa tus credenciales para continuar</p>

                    <asp:Panel ID="pnlMensaje" runat="server" CssClass="cp-alert cp-alert-error" Visible="false">
                        <asp:Literal ID="litMensaje" runat="server" />
                    </asp:Panel>

                    <div class="cp-field">
                        <asp:Label runat="server" AssociatedControlID="txtEmail">Correo electrónico</asp:Label>
                        <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" CssClass="cp-input" placeholder="nombre.usuario@creditpulse.com" autocomplete="username" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail"
                            CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Ingresa tu correo." ValidationGroup="Login" />
                    </div>

                    <div class="cp-field">
                        <asp:Label runat="server" AssociatedControlID="txtPassword">Contraseña</asp:Label>
                        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="cp-input" placeholder="&bull;&bull;&bull;&bull;&bull;&bull;&bull;&bull;&bull;&bull;" autocomplete="current-password" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPassword"
                            CssClass="cp-field-error" Display="Dynamic" ErrorMessage="Ingresa tu contraseña." ValidationGroup="Login" />
                    </div>

                    <asp:Button ID="btnIngresar" runat="server" Text="Ingresar al Sistema &rarr;" CssClass="cp-btn-primary cp-btn-block"
                        OnClick="btnIngresar_Click" ValidationGroup="Login" />
                </div>
            </section>
        </div>
    </form>
    <script type="text/javascript" src="Content/cp-theme.js"></script>
</body>
</html>
