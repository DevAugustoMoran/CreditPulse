const { titlePage, h1, h2, h3, p, pb, bullets, numbered, table, cell, image, caption, buildAndSave, PageBreak, Paragraph, TextRun, COLOR } = require("./common");

const DIAG = "/home/claude/CreditPulse/Documentacion/Diagramas/";

function dictTable(rows) {
  // rows: [campo, tipo, nulo, clave, descripcion]
  const widths = [1900, 1400, 700, 1300, 4700];
  const header = [
    cell("Campo", { header: true, width: widths[0] }),
    cell("Tipo", { header: true, width: widths[1] }),
    cell("Nulo", { header: true, width: widths[2], align: "center" }),
    cell("Clave", { header: true, width: widths[3], align: "center" }),
    cell("Descripción", { header: true, width: widths[4] }),
  ];
  const body = rows.map((r) => [
    cell(r[0], { width: widths[0], bold: true }),
    cell(r[1], { width: widths[1] }),
    cell(r[2], { width: widths[2], align: "center" }),
    cell(r[3], { width: widths[3], align: "center" }),
    cell(r[4], { width: widths[4] }),
  ]);
  return table(widths, [header, ...body]);
}

const body = [
  ...titlePage({
    titulo: "Documentación de la Base de Datos",
    subtitulo: "Diagrama entidad-relación, diccionario de datos y plan de respaldo",
    curso: "Análisis de Sistemas II",
    autor: "Augusto Ardón",
    fecha: "Septiembre 2026",
  }),

  h1("1. Motor y alcance"),
  p("CreditPulse utiliza Microsoft SQL Server como motor de base de datos: SQL Server Express 2022 en el entorno de desarrollo local y una base de datos SQL Server administrada por el proveedor de hosting en el entorno publicado. Todo el acceso a datos se realiza exclusivamente a través de procedimientos almacenados (nunca SQL dinámico embebido en la aplicación), lo cual centraliza la lógica de acceso a datos, facilita la auditoría de qué operaciones existen sobre cada tabla, y evita inyección SQL por construcción."),
  p("La base de datos se llama CreditPulseDB y se construye de forma incremental mediante 11 scripts numerados (carpeta Database/), cada uno responsable de un grupo de funcionalidad: creación de la base, tablas núcleo, procedimientos almacenados, datos semilla, evaluaciones batch, administración de catálogo, roles, usuarios y roles administrables, batch asíncrono, la política RCI Bancario y las cuentas de servicio para las APIs de integración."),

  h1("2. Diagrama Entidad-Relación"),
  p("El siguiente diagrama muestra las ocho tablas del modelo de datos y sus relaciones. Las llaves primarias se marcan con «PK» y las llaves foráneas con «FK» (las que además aceptan NULL se marcan «FK, NULL»)."),
  image(DIAG + "EntidadRelacion.png", 560, 700),
  caption("Figura 1. Diagrama entidad-relación de CreditPulseDB."),

  new Paragraph({ children: [new PageBreak()] }),
  h1("3. Diccionario de Datos"),
  p("A continuación se documenta cada tabla con el detalle de sus columnas, tipo de dato, si acepta nulos y su rol como llave."),

  h2("3.1 Roles"),
  p("Catálogo de roles del sistema. Desde la Fase de Usuarios/Roles, el acceso funcional de cada rol se determina por tres banderas de permiso, no por el nombre del rol — esto permite crear roles nuevos desde el panel de administración con acceso real e inmediato a las pantallas correspondientes."),
  dictTable([
    ["RolId", "INT IDENTITY", "No", "PK", "Identificador autonumérico del rol."],
    ["Nombre", "NVARCHAR(50)", "No", "UQ", "Nombre visible del rol (único). Ej.: 'Administrador'."],
    ["Descripcion", "NVARCHAR(200)", "Sí", "", "Descripción libre del propósito del rol."],
    ["PuedeEvaluar", "BIT", "No", "", "Permite crear evaluaciones individuales y cargas batch."],
    ["PuedeAdministrarCatalogo", "BIT", "No", "", "Permite administrar productos y políticas."],
    ["PuedeAdministrarUsuarios", "BIT", "No", "", "Permite administrar usuarios y roles."],
    ["Activo", "BIT", "No", "", "Si es 0, el rol no puede asignarse a usuarios nuevos."],
  ]),

  h2("3.2 Usuarios"),
  p("Cuentas que inician sesión en el sistema (analistas, administradores y la cuenta de servicio usada por las APIs de integración)."),
  dictTable([
    ["UsuarioId", "INT IDENTITY", "No", "PK", "Identificador autonumérico del usuario."],
    ["Email", "NVARCHAR(150)", "No", "UQ", "Correo electrónico; funciona como nombre de usuario para iniciar sesión."],
    ["PasswordHash", "NVARCHAR(200)", "No", "", "Hash PBKDF2-HMAC-SHA256 (Base64) de la contraseña. Nunca se guarda en texto plano ni cifrada de forma reversible."],
    ["PasswordSalt", "NVARCHAR(200)", "No", "", "Salt aleatorio único por usuario (Base64), usado al calcular el hash."],
    ["NombreCompleto", "NVARCHAR(200)", "No", "", "Nombre completo mostrado en la interfaz."],
    ["RolId", "INT", "No", "FK → Roles", "Rol asignado al usuario."],
    ["Activo", "BIT", "No", "", "Si es 0, el usuario no puede iniciar sesión."],
    ["IntentosFallidos", "INT", "No", "", "Contador de intentos de login fallidos consecutivos, para el bloqueo de cuenta."],
    ["FechaCreacion", "DATETIME2(0)", "No", "", "Fecha de alta del usuario (UTC)."],
    ["UltimoLogin", "DATETIME2(0)", "Sí", "", "Fecha del último inicio de sesión exitoso."],
  ]),

  h2("3.3 Productos"),
  p("Catálogo de productos de crédito evaluables (ej. Tarjeta de Crédito Clásica, Préstamo Hipotecario), más el producto especial 'General' usado por la carga masiva."),
  dictTable([
    ["ProductoId", "INT IDENTITY", "No", "PK", "Identificador autonumérico del producto."],
    ["Codigo", "NVARCHAR(30)", "No", "UQ", "Código corto único del producto (ej. TARJETA_CREDITO, GENERAL)."],
    ["Nombre", "NVARCHAR(150)", "No", "", "Nombre visible del producto."],
    ["Descripcion", "NVARCHAR(400)", "Sí", "", "Descripción libre del producto."],
    ["RequiereIngresos", "BIT", "No", "", "Si el formulario de Nueva Evaluación debe pedir ingresos mensuales."],
    ["RequiereTipoEmpleo", "BIT", "No", "", "Si el formulario debe pedir tipo de empleo."],
    ["RequiereAntiguedadLaboral", "BIT", "No", "", "Si el formulario debe pedir antigüedad laboral en meses."],
    ["Activo", "BIT", "No", "", "Un producto inactivo deja de evaluarse pero conserva su historial."],
    ["FechaCreacion", "DATETIME2(0)", "No", "", "Fecha de alta del producto (UTC)."],
  ]),

  h2("3.4 Politicas"),
  p("Reglas de negocio configurables que el motor de políticas (PolicyEngine) interpreta para cada producto. Agregar o ajustar una política es una operación de datos, no un cambio de código."),
  dictTable([
    ["PoliticaId", "INT IDENTITY", "No", "PK", "Identificador autonumérico de la política."],
    ["ProductoId", "INT", "No", "FK → Productos", "Producto al que pertenece la política."],
    ["Codigo", "NVARCHAR(40)", "No", "UQ (con ProductoId)", "Código corto de la política dentro de su producto."],
    ["Nombre", "NVARCHAR(150)", "No", "", "Nombre visible de la política (ej. 'Score de Buró de Crédito')."],
    ["Descripcion", "NVARCHAR(400)", "Sí", "", "Descripción libre de la regla."],
    ["TipoRegla", "NVARCHAR(50)", "No", "", "Identifica el algoritmo del PolicyEngine que evalúa esta política (ej. SCORE_BURO_MINIMO, RCI_BANCARIO)."],
    ["ParametrosJson", "NVARCHAR(500)", "No", "", "Umbrales de la regla en formato JSON (ej. {\"verde\":650,\"amarillo\":580})."],
    ["Orden", "INT", "No", "", "Orden de evaluación y de presentación en el reporte."],
    ["Activo", "BIT", "No", "", "Una política inactiva deja de evaluarse pero conserva su historial."],
  ]),

  h2("3.5 Evaluaciones"),
  p("Encabezado de cada evaluación crediticia realizada, ya sea individual, generada dentro de un lote, o creada a través de la API de integración."),
  dictTable([
    ["EvaluacionId", "INT IDENTITY(100001,1)", "No", "PK", "Número de evaluación (inicia en 100001 para calzar con el formato mostrado en pantalla)."],
    ["ProductoId", "INT", "No", "FK → Productos", "Producto evaluado."],
    ["UsuarioId", "INT", "No", "FK → Usuarios", "Usuario que generó la evaluación (incluye la cuenta de servicio de las APIs)."],
    ["PrimerNombre / SegundoNombre / PrimerApellido / SegundoApellido", "NVARCHAR(100)", "Parcial", "", "Datos del solicitante (segundo nombre y segundo apellido son opcionales)."],
    ["Nit", "NVARCHAR(20)", "No", "", "Número de identificación tributaria del solicitante."],
    ["Dpi", "NVARCHAR(20)", "No", "Índice", "Documento personal de identificación del solicitante."],
    ["FechaNacimiento", "DATE", "No", "", "Fecha de nacimiento, usada para las políticas de edad."],
    ["IngresosMensuales", "DECIMAL(12,2)", "Sí", "", "Ingreso mensual declarado (nulo si el producto no lo requiere, ej. General)."],
    ["TipoEmpleo", "NVARCHAR(150)", "Sí", "", "Tipo de empleo declarado (si el producto lo requiere)."],
    ["AntiguedadLaboralMeses", "INT", "Sí", "", "Antigüedad laboral en meses (si el producto lo requiere)."],
    ["ResultadoGeneral", "NVARCHAR(10)", "No", "CHECK", "VERDE, AMARILLO o ROJO — el peor color entre todas las políticas evaluadas."],
    ["Estado", "NVARCHAR(30)", "No", "", "Estado del registro (por defecto 'Evaluada')."],
    ["SibRawXml", "XML", "Sí", "", "XML crudo devuelto por la SIB (o su simulador) al momento de la evaluación."],
    ["SibScoreBuro / SibNivelEndeudamiento / SibEnListaNegra", "INT / DECIMAL(5,2) / BIT", "Sí", "", "Copia plana de los datos SIB más consultados, para reportes rápidos sin parsear el XML."],
    ["FechaCreacion", "DATETIME2(0)", "No", "Índice", "Fecha de la evaluación (UTC)."],
  ]),

  h2("3.6 EvaluacionPoliticas"),
  p("Detalle: el resultado de cada política individual dentro de una evaluación."),
  dictTable([
    ["EvaluacionPoliticaId", "INT IDENTITY", "No", "PK", "Identificador autonumérico del detalle."],
    ["EvaluacionId", "INT", "No", "FK → Evaluaciones", "Evaluación a la que pertenece este resultado."],
    ["PoliticaId", "INT", "Sí", "FK → Politicas", "Política evaluada. Queda NULL si la política se elimina del catálogo después de haberse usado."],
    ["NombrePolitica", "NVARCHAR(150)", "No", "", "Copia ('foto') del nombre de la política al momento de evaluar, para que el historial no cambie si la política se renombra o se elimina después."],
    ["Resultado", "NVARCHAR(10)", "No", "CHECK", "VERDE, AMARILLO o ROJO para esta política en particular."],
    ["Detalle", "NVARCHAR(400)", "Sí", "", "Explicación legible del resultado (ej. 'Score de 605 está en el rango Amarillo')."],
    ["Orden", "INT", "No", "", "Orden de presentación en el reporte."],
  ]),

  h2("3.7 Lotes"),
  p("Encabezado de cada carga masiva (batch) de evaluaciones vía Excel."),
  dictTable([
    ["LoteId", "INT IDENTITY", "No", "PK", "Identificador autonumérico del lote."],
    ["NombreArchivo", "NVARCHAR(255)", "No", "", "Nombre del archivo Excel subido."],
    ["UsuarioId", "INT", "No", "FK → Usuarios", "Usuario que subió el lote."],
    ["TotalRegistros", "INT", "No", "", "Total de filas del Excel."],
    ["TotalProcesados", "INT", "No", "", "Filas ya procesadas (usado para la barra de progreso mientras el lote está 'Procesando')."],
    ["TotalVerde / TotalAmarillo / TotalRojo / TotalErrores", "INT", "No", "", "Contadores acumulados por resultado."],
    ["Estado", "NVARCHAR(20)", "No", "CHECK", "'Procesando', 'Completado' o 'Error'."],
    ["MensajeError", "NVARCHAR(400)", "Sí", "", "Motivo si el lote terminó en estado 'Error'."],
    ["FechaCreacion", "DATETIME2(0)", "No", "Índice", "Fecha de creación del lote (UTC)."],
  ]),

  h2("3.8 LoteEvaluaciones"),
  p("Detalle: una fila del Excel corresponde a un registro aquí, exista o no una evaluación asociada."),
  dictTable([
    ["LoteEvaluacionId", "INT IDENTITY", "No", "PK", "Identificador autonumérico de la fila."],
    ["LoteId", "INT", "No", "FK → Lotes", "Lote al que pertenece la fila."],
    ["EvaluacionId", "INT", "Sí", "FK → Evaluaciones", "Evaluación generada por esta fila. NULL si la fila falló validación antes de poder evaluarse."],
    ["FilaExcel", "INT", "No", "", "Número de fila dentro del archivo Excel original, para poder ubicarla."],
    ["NombreCompletoOriginal / NitOriginal / DpiOriginal", "NVARCHAR", "Sí", "", "Copia de los datos tal como venían en el Excel (útil incluso cuando la fila falló)."],
    ["Estado", "NVARCHAR(20)", "No", "CHECK", "'Evaluado' o 'Error'."],
    ["MensajeError", "NVARCHAR(400)", "Sí", "", "Motivo del error si el Estado es 'Error' (ej. 'DPI vacío', 'Fecha de nacimiento ilegible')."],
  ]),

  new Paragraph({ children: [new PageBreak()] }),
  h1("4. Procedimientos almacenados"),
  p("Todo acceso a datos pasa por procedimientos almacenados, agrupados por área funcional:"),
  ...bullets([
    "Autenticación y usuarios: sp_Usuarios_ObtenerPorEmail, sp_Usuarios_ObtenerTodosAdmin, sp_Usuarios_ObtenerPorIdAdmin, sp_Usuarios_Crear, sp_Usuarios_Actualizar, sp_Usuarios_ActualizarPassword, sp_Usuarios_CambiarEstado.",
    "Roles y permisos: sp_Roles_ObtenerTodosAdmin, sp_Roles_ObtenerActivos, sp_Roles_ObtenerPorIdAdmin, sp_Roles_Crear, sp_Roles_Actualizar, sp_Roles_ContarUsuariosActivos.",
    "Catálogo (productos y políticas): procedimientos CRUD equivalentes definidos en 03_StoredProcedures.sql y 06_Administracion.sql, más sp_Productos_ObtenerPorCodigo para la carga batch.",
    "Evaluaciones: inserción del encabezado y su detalle de políticas, y las consultas usadas por Resultado.aspx y por las APIs de integración.",
    "Lotes y evaluaciones batch: sp_Lotes_CrearPendiente, sp_Lotes_ActualizarProgreso, sp_Lotes_CambiarEstado, sp_Lotes_ObtenerPorId, sp_Lotes_ObtenerRecientes, sp_LoteEvaluaciones_ObtenerPorLote, además de las inserciones masivas descritas abajo.",
  ]),
  h2("4.1 Inserciones masivas con parámetros de tabla (TVP)"),
  p("Para que la carga de lotes grandes no dependa de un INSERT por fila, se definieron dos tipos de tabla (Table-Valued Parameters) y sus procedimientos: EvaluacionPoliticaTVP con sp_EvaluacionPoliticas_InsertarLote, y LoteEvaluacionTVP con sp_LoteEvaluaciones_InsertarLote. Ambos permiten enviar un bloque completo de filas en una sola llamada a la base de datos, reduciendo drásticamente el número de viajes de ida y vuelta durante el procesamiento en segundo plano de un lote."),

  h1("5. Plan de respaldo y mantenimiento"),
  h2("5.1 Respaldo"),
  p("Dado que el entorno publicado corre sobre un plan de hosting gratuito con cuota de almacenamiento limitada (30 MB), el plan de respaldo se apoya en dos mecanismos complementarios:"),
  ...numbered([
    "Respaldo del esquema y los scripts: los 11 scripts de Database/ son, en sí mismos, la fuente de verdad reproducible de la estructura completa (tablas, procedimientos almacenados, tipos de tabla y datos semilla). Están versionados junto con el resto del código fuente, así que la base de datos siempre puede reconstruirse desde cero ejecutándolos en orden.",
    "Respaldo de datos: mediante SQL Server Management Studio o Azure Data Studio, conectado a la instancia (local o del proveedor de hosting), se puede generar un respaldo lógico exportando los datos con la utilidad 'Generate Scripts' (script de datos) o un backup completo (.bak) cuando el motor y el plan de hosting lo permiten. Se recomienda tomar este respaldo antes de cualquier carga masiva grande y al cierre de cada periodo de pruebas del curso.",
  ]),
  h2("5.2 Mantenimiento y control de espacio"),
  p("La cuota reducida del entorno de hosting hace necesario un mantenimiento activo del espacio en disco, especialmente después de pruebas de carga masiva. Se documentan dos prácticas:"),
  ...bullets([
    "Script de limpieza de datos de prueba (Database/Utilidades/LimpiarDatosEvaluaciones.sql): elimina el contenido de LoteEvaluaciones, EvaluacionPoliticas, Evaluaciones y Lotes, respetando el orden de llaves foráneas, sin tocar Usuarios, Roles, Productos ni Politicas. Incluye el reinicio opcional de los contadores IDENTITY y una llamada a DBCC SHRINKDATABASE para compactar el archivo físico después del borrado (un DELETE libera espacio para reutilización interna, pero no reduce el tamaño del archivo en disco por sí solo).",
    "Recomendación operativa: probar lotes grandes (varios miles de filas) únicamente contra la instancia local de SQL Server Express durante el desarrollo, y reservar el entorno publicado para lotes de tamaño moderado, acordes a su cuota.",
  ]),
  h2("5.3 Índices"),
  p("Se definieron índices no agrupados sobre las columnas más consultadas fuera de las llaves primarias: Evaluaciones.Dpi (búsqueda de un cliente por documento), Evaluaciones.FechaCreacion descendente (listados recientes del dashboard), EvaluacionPoliticas.EvaluacionId (carga del detalle de una evaluación), Lotes.FechaCreacion descendente (listado de cargas recientes) y LoteEvaluaciones.LoteId (carga del detalle de un lote)."),
];

buildAndSave("/home/claude/CreditPulse/Documentacion/Docs/07_Documentacion_Base_de_Datos.docx", { docTitle: "Documentación de la Base de Datos", body })
  .then(() => console.log("OK: 07_Documentacion_Base_de_Datos.docx"));
