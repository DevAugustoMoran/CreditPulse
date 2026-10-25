const { titlePage, h1, h2, p, pb, bullets, image, caption, buildAndSave, PageBreak, Paragraph } = require("./common");

const DIAG = "/home/claude/CreditPulse/Documentacion/Diagramas/";

const body = [
  ...titlePage({
    titulo: "Diagramas UML",
    subtitulo: "Modelado del sistema: casos de uso, secuencia, actividades, clases y componentes",
    curso: "Análisis de Sistemas II",
    autor: "Augusto Ardón",
    fecha: "Septiembre 2026",
  }),

  h1("1. Introducción"),
  p("Este documento reúne los cinco diagramas UML que modelan CreditPulse: casos de uso, secuencia, actividades, clases y componentes. Cada diagrama se acompaña de una explicación de qué representa y cómo se relaciona con la implementación real del sistema, de modo que sirva tanto como referencia de análisis como de mapa para ubicarse en el código."),
  p("Los diagramas fueron elaborados con PlantUML a partir de la arquitectura efectivamente construida: el motor de políticas configurable, la abstracción de servicio para la Superintendencia de Bancos (SIB), el modelo de permisos por rol, el procesamiento de lotes en segundo plano y las APIs de integración."),

  h1("2. Diagrama de Casos de Uso"),
  p("Representa las funciones del sistema desde la perspectiva de cada tipo de actor. CreditPulse reconoce cuatro actores:"),
  ...bullets([
    "Administrador: además de evaluar, gestiona el catálogo de productos y políticas, y administra usuarios y roles.",
    "Analista de Crédito: crea evaluaciones individuales, carga lotes de evaluaciones masivas y consulta resultados.",
    "Usuario de Consulta: solo puede consultar evaluaciones y lotes ya existentes, sin crear nada nuevo (rol de solo lectura).",
    "Sistema Externo: cualquier sistema del ecosistema bancario que consume las APIs REST de integración autenticándose con una llave (X-Api-Key), sin necesidad de iniciar sesión como un usuario humano.",
  ]),
  p("Las relaciones «include» muestran dependencias funcionales reales: crear una evaluación individual siempre incluye consultar el perfil de la SIB, y cargar un lote incluye —fila por fila— el mismo caso de uso de evaluación individual, porque el motor de negocio (EvaluationService) es compartido entre ambos flujos."),
  image(DIAG + "CasosDeUso.png", 560, 830),
  caption("Figura 1. Diagrama de casos de uso de CreditPulse."),

  new Paragraph({ children: [new PageBreak()] }),
  h1("3. Diagrama de Secuencia"),
  p("Modela el flujo de mensajes de punta a punta para el caso de uso más representativo del sistema: crear una evaluación crediticia individual. Se ilustran los participantes reales del código (Nueva.aspx, EvaluationService, ISibService, SibXmlParser, PolicyEngine y SQL Server) y los dos caminos alternos de error contemplados explícitamente: datos de entrada inválidos y falta de respuesta de la SIB."),
  p("Un punto clave del diagrama es el bucle («loop») sobre las políticas activas del producto: el PolicyEngine evalúa cada política de forma independiente y el resultado general de la evaluación se calcula después, tomando el peor color entre todas ellas (jerarquía de semáforo: un solo Rojo hace Rojo el resultado general; sin Rojo pero con al menos un Amarillo, el resultado es Amarillo; solo es Verde si todas las políticas lo son)."),
  image(DIAG + "Secuencia.png", 620, 660),
  caption("Figura 2. Diagrama de secuencia: evaluación crediticia individual."),

  new Paragraph({ children: [new PageBreak()] }),
  h1("4. Diagrama de Actividades"),
  p("Describe el flujo de trabajo completo desde la perspectiva del Analista de Crédito, cubriendo las dos modalidades de evaluación que ofrece el sistema y bifurcando el flujo según cuál elija:"),
  ...bullets([
    "Evaluación individual: completar el formulario, validar datos, consultar la SIB, ejecutar el motor de políticas y ver el resultado — con salidas tempranas si los datos son inválidos o la SIB no responde.",
    "Carga masiva (Batch): subir un Excel, crear el lote de inmediato y procesar cada fila en segundo plano (representado con un fork/fork again, ya que las filas se procesan por bloques mientras el analista ya puede ver el progreso), hasta consultar el reporte final del lote.",
  ]),
  image(DIAG + "Actividades.png", 480, 640),
  caption("Figura 3. Diagrama de actividades: evaluación individual vs. carga masiva."),

  new Paragraph({ children: [new PageBreak()] }),
  h1("5. Diagrama de Clases"),
  p("Muestra la estructura interna de las tres capas de lógica del sistema (Modelos, BLL y Api) y cómo colaboran entre sí. Los elementos más relevantes del diseño:"),
  ...bullets([
    "La interfaz ISibService con dos implementaciones (SibMockService y SibApiClientService) permite que EvaluationService consulte a la SIB sin saber si la respuesta viene de un simulador en memoria o de un servicio HTTP externo — es el patrón de diseño que hace posible cambiar de uno a otro con una sola línea de configuración.",
    "PolicyEngine concentra toda la lógica de reglas de negocio (score mínimo, endeudamiento máximo, RCI bancario, historial de moras), interpretando el TipoRegla y ParametrosJson de cada Politica sin que agregar una regla nueva requiera cambiar el modelo de datos.",
    "BatchEvaluationService reutiliza EvaluationService fila por fila, evitando duplicar la lógica de evaluación entre el flujo individual y el flujo masivo.",
    "Los controladores de la Api (EvaluacionesApiController, SibApiController) están marcados como protegidos por ApiKeyAuthorizeAttribute, el filtro global de autenticación por llave.",
  ]),
  image(DIAG + "Clases.png", 620, 745),
  caption("Figura 4. Diagrama de clases: modelos, lógica de negocio y capa de integración."),

  new Paragraph({ children: [new PageBreak()] }),
  h1("6. Diagrama de Componentes"),
  p("Presenta la vista de más alto nivel: cómo se despliegan y comunican las piezas del sistema. CreditPulse.Web concentra la interfaz WebForms, la capa de negocio (BLL), el acceso a datos (DAL) y las APIs de integración en un mismo proceso IIS; CreditPulse.SibSimulador es un proyecto ASP.NET Web API 2 independiente, sin referencia de código compartida, que simula a la Superintendencia de Bancos como si fuera un sistema externo real."),
  pb("Nota de diseño: ", "el interruptor de configuración UsarSibMock decide en tiempo de ejecución si la consulta a la SIB se resuelve en el mismo proceso (SibMockService) o por HTTP contra CreditPulse.SibSimulador (SibApiClientService), sin que el resto de la aplicación —ni el reporte, ni el motor de políticas, ni el Batch— necesite saber cuál de los dos está activo."),
  image(DIAG + "Componentes.png", 560, 500),
  caption("Figura 5. Diagrama de componentes: despliegue y comunicación entre proyectos."),
];

buildAndSave("/home/claude/CreditPulse/Documentacion/Docs/06_Diagramas_UML.docx", { docTitle: "Diagramas UML", body })
  .then(() => console.log("OK: 06_Diagramas_UML.docx"));
