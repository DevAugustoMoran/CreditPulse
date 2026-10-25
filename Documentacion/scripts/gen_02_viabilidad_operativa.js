const { titlePage, h1, h2, p, pb, bullets, buildAndSave } = require("./common");

const body = [
  ...titlePage({
    titulo: "Viabilidad Operativa",
    subtitulo: "Aceptación, curva de aprendizaje, alineación con el negocio y seguridad",
    curso: "Análisis de Sistemas II",
    autor: "Augusto Ardón",
    fecha: "Septiembre 2026",
  }),

  h1("1. Definición del problema"),
  p("El análisis de solicitudes de crédito de forma manual obliga a un analista a revisar, expediente por expediente, el historial crediticio, el nivel de endeudamiento, los ingresos y el historial de moras de cada solicitante, aplicando de memoria los criterios de aprobación de la institución. Este proceso es lento, propenso a inconsistencias entre analistas distintos y difícil de escalar cuando hay que evaluar a muchos solicitantes en poco tiempo (por ejemplo, una campaña de precalificación). CreditPulse automatiza la consulta y evaluación de estos datos, reduciendo el tiempo de análisis de un solicitante a segundos y aplicando siempre el mismo criterio configurado para el producto correspondiente."),

  h1("2. Aceptación del sistema"),
  ...bullets([
    ["Para los analistas de crédito: el sistema es bien recibido porque libera la carga operativa de recopilar y comparar manualmente los datos del solicitante — el analista pasa de \"recolector de datos\" a revisor del resultado, con la libertad de enfocarse en los casos que el semáforo marca como Amarillo (revisión) en vez de repetir el mismo cálculo en cada solicitud."],
    ["Para administradores: la aceptación es alta porque el motor de políticas configurable estandariza los criterios de aprobación entre todos los analistas — ya no depende de quién evalúe, sino de las políticas activas para ese producto —, y porque el panel de administración permite ajustar esos criterios sin depender de un cambio de código ni de intervención técnica."],
  ]),

  h1("3. Curva de aprendizaje"),
  p("Se proyecta una curva de aprendizaje mínima. El motor de CreditPulse realiza todo el trabajo pesado en el backend (consulta a la SIB, cálculo del semáforo por política); la interfaz se diseñó bajo el principio de simplicidad, con formularios que solo piden los campos que el producto elegido realmente necesita. Un analista solo necesita capacitarse en: completar el formulario de evaluación (o preparar el Excel de carga masiva con la plantilla provista), y leer el resultado (semáforo general y detalle por política). Ambos flujos se cubren en detalle en el Manual de Usuario."),

  h1("4. Alineación con el negocio"),
  p("CreditPulse está alineado con el objetivo de agilizar la colocación de crédito sin sacrificar el control de riesgo: al reducir el tiempo de evaluación de minutos u horas a segundos, un analista puede atender más solicitudes en la misma jornada sin que eso implique contratar más personal, y la evaluación por lotes permite procesar campañas de precalificación de cientos o miles de clientes potenciales de una sola vez. La estandarización de criterios (motor de políticas configurable) también mejora la transparencia frente a auditorías internas, porque cada resultado queda respaldado por el detalle de qué política lo produjo y por qué."),

  h1("5. Cumplimiento operativo y seguridad de la información"),
  p("Operativamente, el sistema incorpora varias prácticas de seguridad de la información desde su diseño inicial:"),
  ...bullets([
    "Contraseñas con hash irreversible (PBKDF2-HMAC-SHA256 con salt por usuario), en vez de una encriptación simétrica reversible: ni siquiera un administrador con acceso a la base de datos puede recuperar la contraseña original de un usuario, lo cual es la práctica estándar de la industria para credenciales.",
    "Control de sesión con expiración por inactividad y bloqueo de la cuenta tras varios intentos fallidos de inicio de sesión consecutivos.",
    "Restricción de pantallas completas según el rol y los permisos del usuario autenticado, con redirección a una pantalla de Acceso Denegado ante cualquier intento de acceso directo por URL a una pantalla no autorizada.",
    "Autenticación por llave (X-Api-Key) para las tres APIs de integración, de forma que solo sistemas autorizados del ecosistema bancario puedan solicitar evaluaciones o consultar datos de CreditPulse.",
  ]),
  pb("Próxima iteración de seguridad: ", "el cifrado a nivel de columna de los identificadores NIT y DPI en las tablas maestras (por ejemplo, mediante Always Encrypted o cifrado de columna de SQL Server) queda documentado como el siguiente paso natural de este módulo de seguridad, junto con una auditoría más detallada de cambios de estado por evaluación — ambos se pueden incorporar sin alterar el resto de la arquitectura del sistema, y se documentan aquí para que queden explícitamente contemplados en la hoja de ruta del proyecto."),
];

buildAndSave("/home/claude/CreditPulse/Documentacion/Docs/02_Viabilidad_Operativa.docx", { docTitle: "Viabilidad Operativa", body })
  .then(() => console.log("OK: 02_Viabilidad_Operativa.docx"));
