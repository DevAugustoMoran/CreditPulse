const { titlePage, h1, h2, p, pb, bullets, buildAndSave, Paragraph, PageBreak, TextRun, COLOR } = require("./common");

function rf(codigo, titulo, descripcion, puntos, ejemplo) {
  return [
    h2(`${codigo} — ${titulo}`),
    p(descripcion),
    p("Información o funciones principales:"),
    ...bullets(puntos),
    new Paragraph({
      spacing: { after: 240, line: 276 },
      children: [
        new TextRun({ text: "Ejemplo: ", bold: true, italics: true, font: "Calibri", size: 20, color: COLOR.gray }),
        new TextRun({ text: ejemplo, italics: true, font: "Calibri", size: 20, color: COLOR.gray }),
      ],
    }),
  ];
}

function rnf(codigo, titulo, descripcion) {
  return [pb(`${codigo} — ${titulo}: `, descripcion)];
}

const body = [
  ...titlePage({
    titulo: "Requerimientos Funcionales y No Funcionales",
    subtitulo: "Especificación detallada de RF y RNF implementados",
    curso: "Análisis de Sistemas II",
    autor: "Augusto Ardón",
    fecha: "Septiembre 2026",
  }),

  h1("1. Requerimientos Funcionales"),
  p("A continuación se documentan los requerimientos funcionales de CreditPulse, en el orden en que un usuario recorre el sistema: primero se autentica, luego evalúa (individual o masivamente), consulta resultados, y —según su rol— administra el catálogo, los usuarios o integra otros sistemas mediante las APIs."),

  ...rf("RF-01", "Autenticación y control de acceso",
    "El sistema deberá permitir el inicio de sesión de usuarios registrados y restringir el acceso a las pantallas según el rol y los permisos asignados.",
    [
      "Inicio de sesión con correo electrónico y contraseña.",
      "Bloqueo progresivo por intentos fallidos consecutivos.",
      "Expiración de sesión configurable por inactividad.",
      "Restricción de pantallas completas según los permisos del rol del usuario (Evaluar, Administrar Catálogo, Administrar Usuarios).",
      "Redirección a una pantalla de Acceso Denegado si el usuario intenta abrir, incluso por URL directa, una pantalla que su rol no permite.",
    ],
    "Un usuario con rol \"Consulta\" que escribe directamente la URL de Administración de Productos es redirigido a Acceso Denegado, aunque el enlace ni siquiera aparezca en su menú."),

  ...rf("RF-02", "Registro y evaluación individual de un solicitante",
    "El sistema deberá permitir capturar los datos de un solicitante y evaluarlo contra las políticas del producto de crédito elegido.",
    [
      "Datos personales: nombres, apellidos, NIT, DPI, fecha de nacimiento.",
      "Campos adicionales que aparecen o desaparecen según el producto: ingresos mensuales, tipo de empleo, antigüedad laboral.",
      "Selección del producto de crédito a evaluar (Tarjeta de Crédito, Préstamo Hipotecario, Crédito Personal, Crédito Vehicular).",
      "Validación de los datos obligatorios antes de ejecutar la evaluación.",
    ],
    "Un analista captura los datos de Ana López para el producto Tarjeta de Crédito Clásica; el sistema valida que todos los campos requeridos estén completos antes de continuar."),

  ...rf("RF-03", "Consulta de información crediticia (SIB)",
    "El sistema deberá consultar el perfil crediticio del solicitante en la Superintendencia de Bancos (SIB) como parte de toda evaluación.",
    [
      "Score de buró de crédito.",
      "Nivel de endeudamiento reportado.",
      "Historial de moras y días de mora.",
      "Listas de riesgo (local, internacional, OFAC).",
      "Referencias internas (cliente existente, créditos previos).",
    ],
    "Al evaluar a un solicitante, el sistema consulta su perfil por DPI y recibe el score de buró, el nivel de endeudamiento y el historial de moras que se usarán en el motor de políticas."),

  ...rf("RF-04", "Motor de evaluación de riesgo (políticas configurables)",
    "El sistema deberá evaluar al solicitante contra el conjunto de políticas activas del producto elegido y emitir un resultado por semáforo.",
    [
      "Cada política se evalúa de forma independiente y produce Verde, Amarillo o Rojo con una explicación legible.",
      "El resultado general se calcula por jerarquía: un solo Rojo hace Rojo el resultado general; sin Rojo pero con al menos un Amarillo, el resultado es Amarillo; solo es Verde si todas las políticas lo son.",
      "Los umbrales de cada política (ej. score mínimo, endeudamiento máximo, RCI bancario) son configurables por datos, sin cambiar código.",
    ],
    "Si la política de Score de Buró da Verde pero la de RCI Bancario da Amarillo, el resultado general de la evaluación es Amarillo (\"Requiere Revisión\")."),

  ...rf("RF-05", "Reporte de evaluación",
    "El sistema deberá generar un reporte de cada evaluación con el detalle por política y la información de la SIB en un formato legible para un usuario no técnico.",
    [
      "Semáforo general y detalle de cada política evaluada, con su color y explicación.",
      "Tarjeta de score con medidor visual y descripción en palabras (\"Score alto/medio/bajo\").",
      "Secciones de Historial Crediticio, Listas de Riesgo y Referencias Internas, en español y sin XML crudo a la vista.",
      "Sección técnica colapsable con el XML original, disponible para quien lo necesite (soporte, auditoría).",
    ],
    "El reporte de la evaluación 100481 muestra el semáforo Amarillo, el detalle de las 7 políticas de Tarjeta de Crédito Clásica, y una tarjeta de score con la descripción \"Score medio (605 puntos)\"."),

  ...rf("RF-06", "Evaluación masiva por lotes (Batch)",
    "El sistema deberá permitir evaluar a un conjunto grande de solicitantes de una sola vez, a partir de un archivo Excel.",
    [
      "Carga de un archivo Excel (.xlsx) con la plantilla del sistema (nombres, NIT, DPI, fecha de nacimiento).",
      "Procesamiento en segundo plano, sin bloquear al usuario mientras el archivo se evalúa.",
      "Pantalla de progreso en vivo (\"3,400 de 10,000 procesados\") mientras el lote está en curso.",
      "Una fila con datos inválidos no aborta el lote completo: se registra con su motivo de error y el procesamiento continúa con la siguiente fila.",
      "Reporte final del lote con contadores por resultado (Verde/Amarillo/Rojo/Errores) y el detalle de cada fila, con acceso al reporte individual de cada evaluación generada.",
    ],
    "Un analista sube un Excel de 1,000 solicitantes; el sistema crea el lote de inmediato y lo procesa en bloques de 200 filas, mostrando el avance sin que el analista tenga que esperar con la pantalla congelada."),

  ...rf("RF-07", "Administración del catálogo de productos y políticas",
    "El sistema deberá permitir dar de alta, editar y activar/desactivar productos de crédito y sus políticas de evaluación, sin modificar la base de datos directamente.",
    [
      "Alta y edición de un producto: código, nombre, descripción, y qué campos adicionales solicita el formulario de evaluación.",
      "Alta y edición de una política: código, nombre, tipo de regla (de un catálogo fijo) y sus parámetros en JSON, con un ejemplo dinámico según el tipo elegido.",
      "Activar/desactivar en vez de eliminar: un producto o política desactivada deja de evaluarse pero conserva el historial de las evaluaciones ya hechas con ella.",
    ],
    "Un administrador ajusta el umbral \"verde\" de la política de Score de Buró de 650 a 630 puntos editando sus parámetros, sin que nadie tenga que modificar el código fuente."),

  ...rf("RF-08", "Administración de usuarios y roles",
    "El sistema deberá permitir gestionar las cuentas de usuario y los roles del sistema, incluyendo qué puede hacer cada rol.",
    [
      "Alta, edición, cambio de contraseña y activar/desactivar usuarios.",
      "Alta, edición y activar/desactivar roles, marcando de forma independiente tres permisos: Evaluar, Administrar Catálogo, Administrar Usuarios.",
      "Un rol nuevo, creado desde el panel de administración, obtiene acceso funcional real de inmediato a las pantallas correspondientes a sus permisos.",
      "Restricciones de integridad: un usuario no puede desactivarse a sí mismo; un rol no puede desactivarse (ni perder el permiso de Administrar Usuarios) mientras tenga usuarios activos asignados.",
    ],
    "Un administrador crea el rol \"Supervisor\" con los permisos Evaluar y Administrar Catálogo activados; en cuanto se asigna a un usuario, ese usuario ya puede evaluar y administrar el catálogo, sin ningún cambio de código."),

  ...rf("RF-09", "APIs de integración con el ecosistema bancario",
    "El sistema deberá exponer servicios REST para que otros sistemas bancarios soliciten evaluaciones o consulten información de CreditPulse sin intervención humana.",
    [
      "POST /api/v1/evaluaciones: solicita una evaluación crediticia completa y devuelve el semáforo y el detalle por política.",
      "GET /api/v1/evaluaciones/{id}: consulta una evaluación ya realizada (por API, por el formulario individual o por un lote).",
      "GET /api/v1/sib/perfil: consulta los datos más relevantes de la SIB para un cliente, sin crear una evaluación.",
      "GET /api/v1/evaluaciones/{id}/variables-score: expone un modelo de referencia de variables ponderadas para que un sistema externo calcule su propio score.",
      "Autenticación de las tres rutas mediante el header X-Api-Key, validado por un filtro global.",
    ],
    "El sistema de otro banco envía una solicitud POST a /api/v1/evaluaciones con los datos del cliente y su llave, y recibe en la respuesta el número de evaluación, el semáforo general y el detalle de cada política, en formato JSON."),

  new Paragraph({ children: [new PageBreak()] }),
  h1("2. Requerimientos No Funcionales"),
  p("Los requerimientos no funcionales describen cualidades del sistema que no son una función específica, pero que condicionan cómo se construyó cada uno de los requerimientos funcionales anteriores."),

  ...rnf("RNF-01", "Rendimiento",
    "Una evaluación individual debe completarse de forma prácticamente instantánea (validación, consulta a la SIB, motor de políticas y guardado, todo dentro de una sola solicitud HTTP). Para lotes grandes, el procesamiento se ejecuta en segundo plano y por bloques de 200 filas, con inserciones masivas mediante parámetros con valores de tabla (TVP) en vez de una escritura por fila, para mantener un número de operaciones a la base de datos manejable incluso con miles de registros."),
  ...rnf("RNF-02", "Disponibilidad",
    "El sistema debe estar disponible durante el horario de uso esperado para el alcance de un proyecto de curso (pruebas y demostración), publicado en un entorno de hosting real y accesible por URL pública, sin depender de que el equipo de desarrollo mantenga un servidor local encendido."),
  ...rnf("RNF-03", "Arquitectura de datos",
    "Todo acceso a la base de datos se realiza a través de procedimientos almacenados, nunca mediante SQL dinámico embebido en la aplicación, para centralizar la lógica de acceso a datos, mantener consultas predecibles y evitar inyección SQL por construcción."),
  ...rnf("RNF-04", "Seguridad",
    "Las contraseñas se almacenan como hash PBKDF2-HMAC-SHA256 con salt único por usuario (nunca en texto plano ni cifradas de forma reversible); la política de contraseñas exige 10 o más caracteres con letra, número y símbolo; la sesión expira por inactividad y se bloquea tras varios intentos fallidos; las tres APIs de integración exigen una llave (X-Api-Key) validada por un filtro global; y la clave criptográfica de la aplicación (machineKey) es fija, para que las sesiones no se invaliden cada vez que el servidor recicla el proceso por inactividad."),
  ...rnf("RNF-05", "Usabilidad",
    "La interfaz debe ser utilizable por un analista de crédito sin conocimientos técnicos: tema oscuro consistente, tipografía legible, diseño responsivo con panel de navegación deslizable en pantallas angostas, y un reporte de la SIB en español, sin XML ni etiquetas técnicas visibles por defecto."),
  ...rnf("RNF-06", "Mantenibilidad",
    "Agregar una política nueva o ajustar un umbral existente debe ser una operación de datos (un registro en la tabla Politicas), no un cambio de código fuente; de la misma forma, un rol nuevo debe obtener acceso funcional real a partir de sus permisos, sin necesidad de modificar el código de las pantallas restringidas."),
  ...rnf("RNF-07", "Portabilidad de la integración con la SIB",
    "La consulta a la Superintendencia de Bancos debe resolverse detrás de una única abstracción (ISibService), de forma que el resto de la aplicación no distinga si la respuesta viene de un simulador en memoria o de un servicio HTTP externo — y que el día que exista una API oficial, conectarse a ella no requiera cambiar el motor de políticas, el reporte ni el proceso de lotes."),
];

buildAndSave("/home/claude/CreditPulse/Documentacion/Docs/05_Requerimientos_Funcionales_y_No_Funcionales.docx", { docTitle: "Requerimientos Funcionales y No Funcionales", body })
  .then(() => console.log("OK: 05_Requerimientos_Funcionales_y_No_Funcionales.docx"));
