const { titlePage, h1, h2, p, pb, bullets, numbered, buildAndSave, Paragraph, PageBreak } = require("./common");

const body = [
  ...titlePage({
    titulo: "Levantamiento de Requerimientos",
    subtitulo: "Identificación de la necesidad y definición del alcance",
    curso: "Análisis de Sistemas II",
    autor: "Augusto Ardón",
    fecha: "Septiembre 2026",
  }),

  h1("1. Identificación de la Necesidad"),
  p("El análisis de solicitudes de crédito de forma manual obliga a un analista a revisar, uno por uno, distintos factores del solicitante —edad, historial crediticio, nivel de endeudamiento, ingresos, moras previas— y a compararlos mentalmente contra los criterios de aprobación de la institución. Este proceso es lento, depende del criterio individual de quien evalúa (dos analistas pueden llegar a conclusiones distintas ante el mismo perfil) y se vuelve prácticamente inviable cuando hay que evaluar no a un solicitante sino a cientos o miles al mismo tiempo, como ocurre en campañas de precalificación masiva."),
  pb("La necesidad principal es: ", "contar con un sistema que automatice la evaluación de riesgo crediticio aplicando criterios de aprobación configurables y consistentes, que informe de: la Superintendencia de Bancos (SIB) para conocer el historial del solicitante, que produzca un veredicto claro (semáforo Verde, Amarillo o Rojo) explicado política por política, y que además permita evaluar un lote completo de solicitantes desde un archivo Excel, sin sacrificar la trazabilidad de qué se evaluó, cómo, y quién lo hizo."),
  p("Esta necesidad se identificó específicamente en el contexto de un curso de Análisis de Sistemas, donde el interés es demostrar el ciclo completo de análisis, diseño e implementación de un sistema de este tipo con un alcance acotado y verificable, priorizando un motor de decisión configurable y auditable por sobre modelos predictivos que requerirían datos históricos reales, entrenamiento y validación estadística —fuera del alcance de una entrega académica—."),

  h1("2. Levantamiento de Requerimientos"),
  p("Se levantaron los requerimientos mediante reuniones de trabajo iterativas con el usuario dueño del proyecto, en las que se definió el alcance en fases incrementales: primero el núcleo de evaluación individual, luego la evaluación por lotes, después la administración del catálogo, y finalmente los roles y permisos, la administración de usuarios, el reporte legible de la SIB, la optimización del procesamiento de lotes grandes y las APIs de integración con el resto del ecosistema bancario. El detalle completo de los requerimientos funcionales y no funcionales resultantes se documenta en el archivo separado \"Requerimientos Funcionales y No Funcionales\"; a continuación se resume su naturaleza:"),
  h2("2.1 Requerimientos funcionales (resumen)"),
  ...bullets([
    "Autenticación y control de acceso por rol, con permisos independientes (evaluar, administrar catálogo, administrar usuarios).",
    "Registro y evaluación individual de un solicitante contra las políticas del producto elegido.",
    "Consulta de la información crediticia del solicitante ante la Superintendencia de Bancos (SIB).",
    "Motor de evaluación de riesgo configurable por producto, con resultado por semáforo (Verde/Amarillo/Rojo).",
    "Evaluación masiva de solicitantes mediante la carga de un archivo Excel (evaluación por lotes).",
    "Administración del catálogo de productos y políticas de crédito.",
    "Administración de usuarios y roles del sistema.",
    "Exposición de APIs REST para que otros sistemas del ecosistema bancario soliciten evaluaciones o consulten información de la SIB.",
  ]),
  h2("2.2 Requerimientos no funcionales (resumen)"),
  ...bullets([
    "Rendimiento adecuado para evaluaciones individuales instantáneas y procesamiento en segundo plano para lotes de gran volumen.",
    "Seguridad: contraseñas con hash irreversible, sesión con expiración y bloqueo por intentos fallidos, comunicación de las APIs protegida por llave.",
    "Todo acceso a datos a través de procedimientos almacenados, para un rendimiento predecible y consultas centralizadas.",
    "Interfaz simple y consistente, utilizable por un analista sin conocimientos técnicos.",
    "Motor de reglas de negocio ajustable por datos, sin requerir cambios de código fuente para modificar un umbral o agregar una política.",
  ]),

  h1("3. Definición del Alcance del Sistema"),
  h2("3.1 Dentro del alcance (in-scope)"),
  ...numbered([
    "Inicio de sesión con correo electrónico y contraseña, con control de intentos fallidos y expiración de sesión.",
    "Formulario de evaluación individual, con campos adicionales que aparecen o desaparecen según el producto solicitado.",
    "Consulta del perfil crediticio del solicitante ante un servicio que representa a la Superintendencia de Bancos.",
    "Motor de políticas configurable en base de datos: cada producto tiene un conjunto de políticas, y cada política se evalúa de forma independiente antes de calcular el resultado general por jerarquía de semáforo.",
    "Reporte de evaluación con el detalle de cada política y una vista legible (no técnica) de la información de la SIB.",
    "Carga masiva de evaluaciones desde un archivo Excel, con procesamiento en segundo plano, barra de progreso y reporte de resultados por lote.",
    "Administración de productos, políticas, usuarios y roles desde paneles dedicados, sin necesidad de modificar la base de datos directamente.",
    "Modelo de permisos por rol (evaluar, administrar catálogo, administrar usuarios) que restringe pantallas completas según el perfil del usuario autenticado.",
    "Tres APIs REST de integración (evaluación completa, consulta de datos de la SIB, y variables ponderadas de referencia para el cálculo de un score) protegidas por llave de API, para que otros sistemas bancarios consuman los servicios de CreditPulse.",
  ]),
  h2("3.2 Fuera del alcance (out-of-scope)"),
  p("Los siguientes puntos se definieron explícitamente como fuera del alcance de este sistema, ya sea porque pertenecen a otro sistema del ecosistema bancario, porque requieren information o infraestructura que no está disponible, o porque exceden el objetivo académico de la entrega:"),
  ...bullets([
    "Gestión documental (carga, validación y almacenamiento de documentos de soporte como DPI escaneado, constancias de ingresos o estados de cuenta): CreditPulse evalúa con los datos que el solicitante declara en el formulario, no gestiona expedientes documentales.",
    "Desembolso y ciclo de vida posterior del crédito (desembolso de fondos, cobro de cuotas, gestión de mora): CreditPulse emite el veredicto de riesgo; la disposición de fondos y el resto del ciclo de vida del crédito pertenecen al core bancario de la institución, un sistema distinto.",
    "Cálculo de un score numérico propio mediante un modelo predictivo o de aprendizaje automático: la decisión de diseño fue construir un motor de reglas de negocio configurable (políticas con umbrales) en lugar de un modelo estadístico, por ser más simple de auditar, ajustar y explicar a un analista no técnico — cada resultado tiene una razón textual clara, en vez de ser la salida de una caja negra.",
    "Conexión directa a una API oficial de la Superintendencia de Bancos: dicha API pública no existe hoy para consumo automatizado de terceros (la SIB solo ofrece autoconsulta personal); el sistema se apoya en un simulador propio, diseñado para que su reemplazo futuro por un servicio real no requiera cambiar el resto de la aplicación (ver Manual Técnico, sección de arquitectura de la integración con la SIB).",
    "Auditoría detallada de cambios de estado y cifrado a nivel de columna de los identificadores NIT/DPI: quedan documentados como la siguiente iteración natural del sistema (ver Viabilidad Operativa y Manual Técnico), sin bloquear la entrega del núcleo funcional evaluado en este curso.",
  ]),
];

buildAndSave("/home/claude/CreditPulse/Documentacion/Docs/01_Levantamiento_de_Requerimientos.docx", { docTitle: "Levantamiento de Requerimientos", body })
  .then(() => console.log("OK: 01_Levantamiento_de_Requerimientos.docx"));
