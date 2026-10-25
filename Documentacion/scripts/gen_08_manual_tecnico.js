const { titlePage, h1, h2, h3, p, pb, bullets, numbered, table, cell, image, caption, buildAndSave, Paragraph, PageBreak, TextRun, COLOR } = require("./common");

const DIAG = "/home/claude/CreditPulse/Documentacion/Diagramas/";

function codeBlock(lines) {
  return new Paragraph({
    shading: { fill: "F5F5F5" },
    spacing: { before: 120, after: 200 },
    border: {
      top: { color: COLOR.border, space: 4, style: "single", size: 4 },
      bottom: { color: COLOR.border, space: 4, style: "single", size: 4 },
      left: { color: COLOR.border, space: 8, style: "single", size: 4 },
      right: { color: COLOR.border, space: 4, style: "single", size: 4 },
    },
    children: lines.flatMap((l, i) => [
      new TextRun({ text: l, font: "Consolas", size: 17, color: "222222", break: i === 0 ? 0 : 1 }),
    ]),
  });
}

function apiTable() {
  const widths = [1300, 4900, 3700];
  const header = [cell("Método", { header: true, width: widths[0] }), cell("Ruta", { header: true, width: widths[1] }), cell("Propósito", { header: true, width: widths[2] })];
  const rows = [
    ["POST", "/api/v1/evaluaciones", "Solicita una evaluación crediticia completa (SIB + motor de políticas) y la guarda."],
    ["GET", "/api/v1/evaluaciones/{id}", "Consulta una evaluación ya realizada (por API, formulario o lote)."],
    ["GET", "/api/v1/sib/perfil?dpi=...&nit=...&nombre=...", "Consulta los datos de la SIB para un cliente, sin crear una evaluación."],
    ["GET", "/api/v1/evaluaciones/{id}/variables-score", "Devuelve el modelo de referencia de variables ponderadas para el cálculo de un score."],
  ].map((r) => [cell(r[0], { width: widths[0], bold: true }), cell(r[1], { width: widths[1] }), cell(r[2], { width: widths[2] })]);
  return table(widths, [header, ...rows]);
}

const body = [
  ...titlePage({
    titulo: "Manual Técnico",
    subtitulo: "Arquitectura, estructura del código, seguridad, APIs y despliegue",
    curso: "Análisis de Sistemas II",
    autor: "Augusto Ardón",
    fecha: "Septiembre 2026",
  }),

  h1("1. Visión general de la arquitectura"),
  p("CreditPulse se organiza en capas: la interfaz (páginas WebForms .aspx.cs) invoca la capa de lógica de negocio (BLL), que a su vez invoca la capa de acceso a datos (DAL), la cual habla con SQL Server exclusivamente a través de procedimientos almacenados. Las APIs REST de integración (carpeta Api/) conviven en el mismo proyecto y proceso que las páginas WebForms, y reutilizan la misma capa BLL — no duplican lógica de negocio."),
  p("La solución (CreditPulse.sln) contiene dos proyectos: CreditPulse.Web (la aplicación principal, con sus páginas, su lógica de negocio y sus APIs de integración) y CreditPulse.SibSimulador (un segundo proyecto ASP.NET Web API 2, independiente y sin referencia de código compartida, que simula a la Superintendencia de Bancos como si fuera un sistema externo real)."),
  image(DIAG + "Componentes.png", 520, 465),
  caption("Figura 1. Componentes y despliegue (ver el documento «Diagramas UML» para el detalle completo de los 5 diagramas)."),

  h2("1.1 Capas y responsabilidades"),
  ...bullets([
    "Models: clases de datos (POCOs) y enumeraciones que representan las entidades del dominio (Usuario, Rol, Producto, Politica, Evaluacion, SibPerfil, Permiso, etc.).",
    "BLL (lógica de negocio): PolicyEngine (motor de reglas configurables), EvaluationService (orquesta la evaluación completa), BatchEvaluationService (procesamiento de lotes en segundo plano), SibMockService / SibApiClientService (implementaciones de ISibService), SibXmlParser, AdminCatalogoService y AdminUsuariosService.",
    "DAL: acceso a datos vía ADO.NET, invocando siempre procedimientos almacenados — nunca SQL dinámico embebido.",
    "Security: PasswordHasher (hash PBKDF2-HMAC-SHA256 con salt) y el helper de sesión.",
    "Api: los controladores REST (EvaluacionesApiController, SibApiController), su filtro de autenticación (ApiKeyAuthorizeAttribute) y los modelos de request/response propios de la integración.",
  ]),

  h2("1.2 Motor de políticas (el corazón del sistema)"),
  p("El PolicyEngine interpreta cada fila de la tabla Politicas: un TipoRegla (por ejemplo, SCORE_BURO_MINIMO o RCI_BANCARIO) identifica el algoritmo a ejecutar, y un ParametrosJson (por ejemplo, {\"verde\":650,\"amarillo\":580}) trae los umbrales de esa instancia de la regla. Cada política se evalúa de forma independiente y produce un color con una explicación textual; el resultado general de la evaluación se calcula después, tomando el peor color entre todas las políticas activas del producto (jerarquía de semáforo: un Rojo domina, luego un Amarillo, y solo es Verde si todas lo son). Ajustar un umbral o agregar una política nueva a un producto es una operación de datos, no un cambio de código fuente."),

  h2("1.3 Abstracción de la consulta a la SIB"),
  p("La interfaz ISibService (ConsultarPerfil(dpi, nit, nombre) : string) desacopla a EvaluationService de cómo se obtiene el perfil crediticio del solicitante. Existen dos implementaciones intercambiables por configuración (appSetting UsarSibMock en Web.config):"),
  ...bullets([
    "SibMockService (UsarSibMock=true, valor por defecto): genera un XML plausible de forma determinística a partir del DPI —mismo DPI, mismo perfil simulado siempre—, útil para pruebas repetibles de los tres colores de semáforo a propósito, corriendo en el mismo proceso que CreditPulse.Web.",
    "SibApiClientService (UsarSibMock=false): consulta por HTTP al proyecto CreditPulse.SibSimulador (GET api/perfil?dpi=...&nit=...&nombre=...), reutilizando SibXmlParser para interpretar la respuesta igual que con el simulador en memoria.",
  ]),
  pb("Nota: ", "la Superintendencia de Bancos no ofrece hoy una API pública para consumo automatizado de terceros (solo autoconsulta personal), por lo que se mantiene este simulador. El día que exista un servicio oficial, basta con implementar ISibService contra ese webservice y registrarlo en EvaluationService.CrearSibService() — el resto de la aplicación no cambia."),

  new Paragraph({ children: [new PageBreak()] }),
  h1("2. Modelo de seguridad"),
  h2("2.1 Autenticación y contraseñas"),
  p("El inicio de sesión usa el correo electrónico como usuario. Las contraseñas se almacenan con hash PBKDF2-HMAC-SHA256 (100,000 iteraciones) y un salt aleatorio por usuario, ambos en Base64 (Security/PasswordHasher.cs) — nunca en texto plano ni cifradas de forma reversible. La política de contraseñas exige 10 o más caracteres con letra, número y símbolo. Tras varios intentos fallidos consecutivos, la cuenta se bloquea (IntentosFallidos en la tabla Usuarios); un administrador la desbloquea reseteando la contraseña desde Administracion/UsuarioForm.aspx."),

  h2("2.2 Permisos por rol (RBAC por banderas)"),
  p("Cada rol declara tres permisos independientes en la tabla Roles: PuedeEvaluar, PuedeAdministrarCatalogo y PuedeAdministrarUsuarios. BasePage.PermisoRequerido pide uno de estos permisos (no un nombre de rol fijo), y BasePage.OnInit lo valida antes de ejecutar el Page_Load de cualquier página protegida, redirigiendo a AccesoDenegado.aspx si el permiso no está presente — incluso si la pantalla se abre escribiendo la URL directamente. Esto permite que un rol nuevo, creado desde Administracion/RolForm.aspx con los permisos correctos marcados, tenga acceso funcional real de inmediato, sin tocar código."),

  h2("2.3 machineKey fijo"),
  p("El Web.config define un machineKey con validationKey y decryptionKey fijos (en vez del valor AutoGenerate por defecto). Esto es necesario porque tanto el hosting gratuito (Somee.com) como cualquier entorno cuyo proceso de trabajo pueda reciclarse por inactividad invalidan las claves autogeneradas cada vez que el worker process se reinicia — sin una clave fija, las cookies de Forms Authentication (la sesión del usuario) se invalidarían inesperadamente cada vez que el sitio despierta de un período de inactividad."),

  h2("2.4 Autenticación de las APIs de integración"),
  p("Las tres rutas bajo /api/v1/ exigen el header X-Api-Key con el valor del appSetting ApiKeyIntegraciones, validado por Api/Filters/ApiKeyAuthorizeAttribute.cs, registrado como filtro global en WebApiConfig.Register — así, un endpoint nuevo bajo /api/v1/ queda protegido automáticamente, sin depender de que el desarrollador recuerde decorarlo. Sin el header, o con un valor incorrecto, la respuesta es 401 Unauthorized. Las evaluaciones creadas por la API quedan atribuidas a una cuenta de servicio (integracion@creditpulse.com, rol \"Integración API\", con el único permiso PuedeEvaluar) en vez de a un analista humano, porque Evaluaciones.UsuarioId es obligatorio para trazabilidad."),
  pb("Nota importante de Web.config: ", "la aplicación usa Forms Authentication con <deny users=\"?\" /> a nivel global, lo cual redirigiría cualquier solicitud no autenticada al Login. Como las APIs se autentican con su propia llave (no con la sesión de Forms Authentication), existe un bloque <location path=\"api\"> que exime a /api/ de esa regla — sin este bloque, Postman u otro sistema externo recibiría el HTML de la página de Login en vez de la respuesta JSON esperada."),

  new Paragraph({ children: [new PageBreak()] }),
  h1("3. APIs de integración: referencia"),
  p("Las tres APIs devuelven JSON o XML según el header Accept del llamador (Web API 2 resuelve esto automáticamente por content negotiation); si no se envía Accept, responden JSON."),
  apiTable(),
  h2("3.1 Ejemplo: crear una evaluación"),
  codeBlock([
    "POST /api/v1/evaluaciones",
    "X-Api-Key: <tu llave>",
    "Content-Type: application/json",
    "",
    "{",
    '  "productoCodigo": "TARJETA_CREDITO",',
    '  "primerNombre": "Ana", "primerApellido": "López",',
    '  "nit": "1234-5", "dpi": "3478953810101",',
    '  "fechaNacimiento": "1990-03-14",',
    '  "ingresosMensuales": 6500, "tipoEmpleo": "Asalariado",',
    '  "antiguedadLaboralMeses": 30',
    "}",
  ]),
  codeBlock([
    "// Respuesta 201 Created",
    "{",
    '  "evaluacionId": 481, "resultadoGeneral": "AMARILLO",',
    '  "resultadoGeneralEtiqueta": "Requiere Revisión",',
    '  "politicas": [ { "nombre": "Edad Mínima", "resultado": "VERDE", ... },',
    '                 { "nombre": "RCI Bancario", "resultado": "AMARILLO", ... } ],',
    '  "sib": { "scoreBuro": 605, "nivelEndeudamiento": 52.30, "enListaNegra": false },',
    '  "fechaEvaluacion": "2026-09-24T22:40:00"',
    "}",
  ]),
  h2("3.2 API de variables ponderadas para el score"),
  p("GET /api/v1/evaluaciones/{id}/variables-score no expone \"el cálculo interno de un score propio\" —CreditPulse no calcula ni guarda un score numérico propio; el que se ve en pantalla (SibScoreBuro) es el que reporta la SIB tal cual—. En su lugar, expone un modelo de referencia de 8 variables y pesos sugeridos, pensado para que otro sistema pueda calcular su propio score con la fórmula que prefiera:"),
  (function () {
    const widths = [2600, 1200, 5900];
    const header = [cell("Variable", { header: true, width: widths[0] }), cell("Peso", { header: true, width: widths[1], align: "center" }), cell("Origen", { header: true, width: widths[2] })];
    const rows = [
      ["Score de Buró", "25%", "SIB (ScoreBuro)"],
      ["Nivel de Endeudamiento", "15%", "SIB (NivelEndeudamiento)"],
      ["RCI Bancario", "15%", "Calculado (ver sección 4)"],
      ["Moras últimos 12 meses", "15%", "SIB (MorasUltimos12Meses)"],
      ["Días de Mora Máximo", "5%", "SIB (DiasMoraMaximo)"],
      ["Ingresos Mensuales declarados", "10%", "Datos de la solicitud"],
      ["Antigüedad Laboral", "10%", "Datos de la solicitud"],
      ["Edad", "5%", "Calculada de la fecha de nacimiento"],
    ].map((r) => [cell(r[0], { width: widths[0] }), cell(r[1], { width: widths[1], align: "center" }), cell(r[2], { width: widths[2] })]);
    return table(widths, [header, ...rows]);
  })(),
  p("Cada variable se devuelve con su valor crudo (en su unidad real) y su valor normalizado a escala 0-100 (donde 100 es mejor para el cliente), para que sea comparable entre variables con unidades distintas. La respuesta incluye también un scoreSugerido0a100 (la suma ponderada de los normalizados, como un ejemplo de cómo combinarlas) y una nota aclarando que no es \"el score oficial\" de nada."),

  new Paragraph({ children: [new PageBreak()] }),
  h1("4. Política RCI Bancario"),
  p("RCI = Relación Cuota/Ingreso: mide qué tan posible es que el cliente pague un crédito, dividiendo sus deudas entre sus ingresos. La deuda mensual se estima a partir de lo que ya reporta la SIB (aplicando el porcentaje de NivelEndeudamiento sobre los IngresosReportados al buró), y esa deuda estimada se compara contra el ingreso declarado en la solicitud actual:"),
  codeBlock(["RCI% = (NivelEndeudamiento_SIB / 100 × IngresosReportados_SIB)", "        / IngresosMensuales_declarados × 100"]),
  p("Es deliberadamente distinta de la política \"Endeudamiento\" (que solo compara el porcentaje crudo del buró contra un umbral fijo, sin cruzarlo con nada más): si el ingreso declarado ahora coincide con el reportado al buró, el RCI da el mismo resultado que Endeudamiento; si declaró un ingreso distinto, el RCI refleja mejor su capacidad de pago real con el ingreso que está declarando en este momento. Si la evaluación no tiene ingreso mensual declarado (posible en el producto General de carga batch), la política no falla ni asume un valor: da Amarillo con el mensaje \"No se puede calcular el RCI sin un ingreso mensual declarado\"."),

  h1("5. Evaluación por lotes a gran escala"),
  p("El envío de un archivo Excel no procesa las filas dentro de la misma solicitud HTTP: Batch.aspx.cs valida y lee el archivo a memoria, crea el registro del Lote en estado \"Procesando\" y encola el trabajo pesado con HostingEnvironment.QueueBackgroundWorkItem (BatchEvaluationService), redirigiendo de inmediato al reporte del lote. El procesamiento ocurre en bloques de 200 filas: después de cada bloque se guarda el progreso (Lotes.TotalProcesados) mediante inserciones masivas con parámetros con valores de tabla (EvaluacionPoliticaTVP, LoteEvaluacionTVP), reduciendo drásticamente el número de viajes de ida y vuelta a SQL Server frente a un INSERT por fila. Si el proceso se interrumpe a la mitad, las filas ya procesadas quedan correctamente guardadas y contadas, y el lote queda marcado \"Error\" con el motivo, en vez de simplemente colgado. La pantalla de progreso se autorrefresca cada 3 segundos mientras el lote está \"Procesando\" — una solución deliberadamente simple, sin SignalR ni sondeo por JavaScript, suficiente para el alcance del proyecto."),

  new Paragraph({ children: [new PageBreak()] }),
  h1("6. Estructura del proyecto"),
  codeBlock([
    "CreditPulse/",
    "├── CreditPulse.sln",
    "├── CreditPulse.Web/            Aplicación principal (WebForms + APIs)",
    "│   ├── Web.config              Connection string, Forms Auth, políticas,",
    "│   │                            UsarSibMock/SibApiBaseUrl, ApiKeyIntegraciones",
    "│   ├── Site.Master(.cs)        Layout general (sidebar + topbar)",
    "│   ├── Login.aspx / Default.aspx (Dashboard) / AccesoDenegado.aspx",
    "│   ├── Evaluacion/             Nueva, Resultado, Batch, LoteResultado",
    "│   ├── Administracion/         Productos, Politicas, Usuarios, Roles",
    "│   ├── Api/                    Controladores REST + filtro X-Api-Key",
    "│   ├── Models/  DAL/  BLL/  Security/  Content/",
    "├── CreditPulse.SibSimulador/   Simulador SIB como servicio HTTP independiente",
    "└── Database/                  11 scripts SQL incrementales + Utilidades/",
  ]),

  h1("7. Despliegue"),
  h2("7.1 Entorno de desarrollo"),
  ...numbered([
    "Ejecutar en orden los 11 scripts de Database/ sobre una instancia de SQL Server Express 2022.",
    "Ajustar el connectionString en CreditPulse.Web/Web.config.",
    "Abrir CreditPulse.sln en Visual Studio 2019/2022 con la carga de trabajo de desarrollo web y ASP.NET, y restaurar los paquetes NuGet (Newtonsoft.Json, ExcelDataReader/DataSet).",
    "Ejecutar (F5) con CreditPulse.Web como proyecto de inicio; con UsarSibMock=true no es necesario levantar CreditPulse.SibSimulador.",
  ]),
  h2("7.2 Entorno publicado (producción del curso)"),
  p("La aplicación se publica en un plan gratuito de hosting ASP.NET con base de datos SQL Server incluida (Somee.com), mediante publicación FTP desde Visual Studio (Publish → Directorio FTP con el nombre relativo del sitio, no la ruta absoluta que muestra el panel de control del proveedor). Este entorno impone dos particularidades documentadas explícitamente:"),
  ...bullets([
    "Cuota de almacenamiento reducida: se debe monitorear el espacio usado y ejecutar periódicamente el script de limpieza de datos de prueba (Database/Utilidades/LimpiarDatosEvaluaciones.sql, ver \"Documentación de la Base de Datos\") después de pruebas de carga masiva.",
    "El proveedor de hosting inserta un pequeño script publicitario en las respuestas cuyo Content-Type es text/html. Por eso, la configuración de negociación de contenido de las APIs (WebApiConfig.cs) resuelve application/json como formato por defecto cuando el llamador no especifica un Accept — evitando que ese script se inserte dentro de una respuesta JSON y la corrompa para un parser estricto.",
  ]),

  h1("8. Solución de problemas comunes"),
  ...bullets([
    "\"No se encuentra Newtonsoft.Json\": restaurar paquetes NuGet desde la solución.",
    "Error de conexión a SQL Server: revisar el connectionString contra el nombre real de la instancia y el modo de autenticación.",
    "La página de Login no aparece o redirige en bucle: confirmar que se ejecutó el script de datos semilla (crea el usuario administrador de prueba).",
    "Postman recibe HTML de Login en vez de JSON: falta el bloque <location path=\"api\"> en Web.config que exime a las APIs de Forms Authentication (ver sección 2.4).",
    "CreditPulse.SibSimulador no arranca en el puerto esperado: Visual Studio puede asignar otro puerto si el sugerido está ocupado; ajustar SibApiBaseUrl en Web.config con la URL real.",
  ]),
];

buildAndSave("/home/claude/CreditPulse/Documentacion/Docs/08_Manual_Tecnico.docx", { docTitle: "Manual Técnico", body })
  .then(() => console.log("OK: 08_Manual_Tecnico.docx"));
