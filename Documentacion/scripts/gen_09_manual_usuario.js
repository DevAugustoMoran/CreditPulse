const { titlePage, h1, h2, h3, p, pb, bullets, numbered, image, caption, buildAndSave, Paragraph, PageBreak, TextRun, COLOR, table, cell } = require("./common");

const CAP = "/home/claude/CreditPulse/Documentacion/Capturas/";
const IW = 480, IH = 337; // 800x562 -> escalado proporcional

function roleTable() {
  const widths = [2600, 3200, 3900];
  const header = [cell("Rol", { header: true, width: widths[0] }), cell("Correo de prueba", { header: true, width: widths[1] }), cell("Puede hacer", { header: true, width: widths[2] })];
  const rows = [
    ["Administrador", "admin@creditpulse.com", "Todo: evaluar, administrar catálogo y administrar usuarios/roles."],
    ["Analista de Crédito", "analista@creditpulse.com", "Evaluar individualmente y por lotes, y consultar resultados. Sin acceso a Administración."],
    ["Usuario de Consulta", "consulta@creditpulse.com", "Solo consultar evaluaciones y lotes ya existentes (modo lectura)."],
  ].map((r) => [cell(r[0], { width: widths[0], bold: true }), cell(r[1], { width: widths[1] }), cell(r[2], { width: widths[2] })]);
  return table(widths, [header, ...rows]);
}

const body = [
  ...titlePage({
    titulo: "Manual de Usuario",
    subtitulo: "Guía de uso por pantalla y por rol, con capturas del sitio publicado",
    curso: "Análisis de Sistemas II",
    autor: "Augusto Ardón",
    fecha: "Septiembre 2026",
  }),

  h1("1. Introducción"),
  p("Este manual explica cómo usar CreditPulse desde la perspectiva de cada uno de sus tres roles, con capturas de pantalla tomadas directamente del sitio publicado (creditpulse.somee.com). El sistema restringe pantallas completas según el rol del usuario autenticado, por lo que el menú lateral y las opciones disponibles cambian de una cuenta a otra."),
  roleTable(),

  h1("2. Inicio de sesión"),
  p("Para entrar al sistema, ingresa tu correo electrónico y contraseña en la pantalla de inicio. Si olvidas tu contraseña o tu cuenta se bloquea por varios intentos fallidos, un Administrador puede restablecerla desde Administración de Usuarios (sección 4.4)."),

  new Paragraph({ children: [new PageBreak()] }),
  h1("3. Trabajando como Administrador"),
  p("El Administrador ve el menú completo: Dashboard, Nueva Evaluación, Evaluaciones Batch, y el grupo Administración con Productos y Políticas, y Usuarios y Roles."),

  h2("3.1 Dashboard"),
  p("Al iniciar sesión, el Dashboard muestra cuatro contadores (total de evaluaciones, Aprobadas/Verde, Revisión manual/Amarillo, Rechazadas/Rojo), un buscador para ir directo al detalle de una evaluación por su número, las cargas Batch recientes y las evaluaciones recientes registradas en el sistema."),
  image(CAP + "01_Dashboard_Admin.png", IW, IH),
  caption("Figura 1. Dashboard con el menú completo del rol Administrador."),

  h2("3.2 Nueva Evaluación"),
  p("Desde el menú, Nueva Evaluación abre el formulario para evaluar a un solicitante. Primero se elige el producto de crédito:"),
  image(CAP + "02_NuevaEvaluacion_vacio.png", IW, IH),
  caption("Figura 2. Formulario de Nueva Evaluación, con el producto aún sin seleccionar."),
  p("Al elegir un producto (por ejemplo, Tarjeta de Crédito Clásica), aparece automáticamente la sección \"Datos Adicionales\" con los campos que ese producto necesita —en este caso, ingresos mensuales y tipo de empleo—. Un producto como Precalificación General, en cambio, no muestra estos campos porque no los requiere."),
  image(CAP + "03_NuevaEvaluacion_camposDinamicos.png", IW, IH),
  caption("Figura 3. Los campos adicionales aparecen según el producto elegido."),
  p("Al completar los datos personales y presionar \"Evaluar Cliente\", el sistema consulta la SIB, ejecuta el motor de políticas y redirige de inmediato al reporte de resultado (sección 3.3)."),

  h2("3.3 Consultar el resultado de una evaluación"),
  p("El reporte de resultado tiene dos pestañas. \"Políticas Evaluadas\" muestra el semáforo general (en la esquina superior derecha de la tarjeta del cliente) y el detalle de cada política, con su resultado individual y una explicación en español:"),
  image(CAP + "04_Resultado_Politicas.png", IW, IH),
  caption("Figura 4. Pestaña «Políticas Evaluadas» del reporte de una evaluación."),
  p("La pestaña \"Información SIB\" traduce el XML técnico devuelto por la Superintendencia de Bancos a un formato legible: score de buró con medidor visual, nivel de endeudamiento, ingresos reportados a la SIB, historial crediticio (créditos activos/cancelados, moras, días de mora máximo) y listas de riesgo."),
  image(CAP + "05_Resultado_InformacionSIB.png", IW, IH),
  caption("Figura 5. Pestaña «Información SIB» del reporte de una evaluación."),

  new Paragraph({ children: [new PageBreak()] }),
  h2("3.4 Evaluaciones Batch (carga masiva)"),
  p("Evaluaciones Batch permite evaluar a muchos solicitantes de una sola vez. La pantalla tiene dos pasos: descargar la plantilla de Excel (con las columnas Primer Nombre, Segundo Nombre, Primer Apellido, Segundo Apellido, NIT, DPI y Fecha de Nacimiento) y subir el archivo ya lleno con los datos de los solicitantes:"),
  image(CAP + "06_Batch_Upload.png", IW, IH),
  caption("Figura 6. Pantalla de carga de un archivo Excel para Evaluaciones Batch."),
  pb("Qué sucede después de subir el archivo: ", "el sistema crea el lote de inmediato y lo procesa en segundo plano, en bloques de 200 filas, sin que el analista tenga que esperar con la pantalla congelada. La pantalla de reporte del lote (LoteResultado.aspx) muestra, mientras el lote está en curso, una barra de progreso (\"3,400 de 10,000 procesados\") que se autoactualiza cada pocos segundos; al terminar, muestra los contadores finales por resultado (Verde/Amarillo/Rojo/Errores) y una tabla con cada fila del archivo, su resultado y un enlace al detalle individual de cada evaluación generada (el mismo reporte de la sección 3.3). Una fila con datos inválidos (por ejemplo, un DPI vacío o una fecha ilegible) no detiene el resto del lote: se marca con su motivo de error y el procesamiento continúa con la siguiente fila."),

  h2("3.5 Administración: Productos y Políticas"),
  p("Productos y Políticas lista los productos de crédito configurados, con el total de políticas de cada uno y su estado (activo/inactivo):"),
  image(CAP + "07_Admin_Productos.png", IW, IH),
  caption("Figura 7. Listado de productos de crédito configurados."),
  p("Al entrar a \"Políticas\" de un producto, se ve el detalle de sus reglas de evaluación, en el orden en que se aplican:"),
  image(CAP + "08_Admin_Politicas.png", IW, IH),
  caption("Figura 8. Políticas configuradas para el producto Tarjeta de Crédito Clásica."),
  p("Desde \"+ Nuevo Producto\" o \"Editar\" se da de alta o se modifica un producto (código, nombre, y qué campos adicionales pide su formulario). Desde \"+ Nueva Política\" o \"Editar\" se hace lo mismo con una política, eligiendo su tipo de regla de un catálogo fijo —el formulario muestra un ejemplo del formato de parámetros esperado según el tipo elegido—. \"Desactivar\" no borra el producto o la política: solo deja de usarse en evaluaciones nuevas, conservando el historial de las que ya se hicieron con ella."),

  h2("3.6 Administración: Usuarios y Roles"),
  p("Usuarios y Roles lista las cuentas del sistema, su rol asignado, estado y último acceso:"),
  image(CAP + "09_Admin_Usuarios.png", IW, IH),
  caption("Figura 9. Listado de usuarios del sistema."),
  p("Desde \"Ver Roles\" se administra el catálogo de roles, cada uno con sus tres permisos independientes (Evaluar, Administrar Catálogo, Administrar Usuarios) y el número de usuarios activos que lo tienen asignado:"),
  image(CAP + "10_Admin_Roles.png", IW, IH),
  caption("Figura 10. Roles configurados, con sus permisos y usuarios activos."),
  pb("Candados de sentido común: ", "un usuario no puede desactivarse a sí mismo, y un rol no puede desactivarse (ni perder el permiso de Administrar Usuarios) mientras todavía tenga usuarios activos asignados — esto evita dejar la aplicación sin ningún administrador que pueda revertir el cambio."),

  new Paragraph({ children: [new PageBreak()] }),
  h1("4. Trabajando como Analista de Crédito"),
  p("El Analista de Crédito ve un menú reducido: Dashboard, Nueva Evaluación y Evaluaciones Batch, sin el grupo Administración."),
  image(CAP + "11_Dashboard_Analista.png", IW, IH),
  caption("Figura 11. Dashboard del rol Analista de Crédito (sin el grupo Administración en el menú)."),
  p("Si un Analista de Crédito intenta abrir una pantalla de Administración escribiendo la URL directamente —incluso sin que el enlace aparezca en su menú—, el sistema lo redirige a Acceso Denegado:"),
  image(CAP + "12_AccesoDenegado.png", IW, IH),
  caption("Figura 12. Pantalla de Acceso Denegado al intentar abrir una pantalla no autorizada."),

  h1("5. Trabajando como Usuario de Consulta"),
  p("El Usuario de Consulta solo ve Dashboard en el menú, y su Dashboard no muestra los botones \"+ Nueva Evaluación\" ni \"+ Nueva Carga Batch\": puede buscar y revisar el detalle de evaluaciones y lotes ya existentes (los mismos reportes de las secciones 3.3 y 3.4), pero no puede crear ninguno nuevo."),
  image(CAP + "13_Dashboard_Consulta.png", IW, IH),
  caption("Figura 13. Dashboard de solo lectura del rol Usuario de Consulta."),

  h1("6. Cerrar sesión"),
  p("El enlace \"Salir\", visible en la esquina superior derecha en cualquier pantalla, cierra la sesión y regresa a la pantalla de inicio de sesión."),
];

buildAndSave("/home/claude/CreditPulse/Documentacion/Docs/09_Manual_de_Usuario.docx", { docTitle: "Manual de Usuario", body })
  .then(() => console.log("OK: 09_Manual_de_Usuario.docx"));
