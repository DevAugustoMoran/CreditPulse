const { titlePage, h1, h2, p, pb, bullets, numbered, table, cell, buildAndSave } = require("./common");

function costTable() {
  const widths = [2200, 2200, 3800, 1800];
  const header = [
    cell("Categoría", { header: true, width: widths[0] }),
    cell("Concepto", { header: true, width: widths[1] }),
    cell("Detalle técnico / recurso", { header: true, width: widths[2] }),
    cell("Estimación", { header: true, width: widths[3] }),
  ];
  const rows = [
    ["Recursos Humanos", "Desarrollo del sistema", "Un desarrollador (rol de estudiante), cubriendo análisis, programación, pruebas y documentación por fase de proyecto.", "Inversión inicial (tiempo propio)"],
    ["Software y Licencias", "Entorno de base de datos y desarrollo", "SQL Server Express 2022 (sin costo de licencia), Visual Studio Community, PlantUML y herramientas de modelado — todas de uso gratuito.", "Costo Q0.00"],
    ["Infraestructura", "Hosting", "Plan gratuito de hosting ASP.NET + SQL Server (Somee.com), con publicación vía FTP.", "Costo Q0.00 (plan gratuito)"],
    ["Hardware", "Equipo de desarrollo", "Computadora personal del desarrollador, ya disponible.", "Sin costo adicional"],
    ["Mantenimiento", "Soporte y ajustes", "Ajuste de umbrales de políticas, limpieza periódica de datos de prueba para no exceder la cuota del hosting gratuito, y respaldo de scripts.", "Recurrente (tiempo propio)"],
  ];
  const body = rows.map((r) => [
    cell(r[0], { width: widths[0], bold: true }),
    cell(r[1], { width: widths[1] }),
    cell(r[2], { width: widths[2] }),
    cell(r[3], { width: widths[3] }),
  ]);
  return table(widths, [header, ...body]);
}

const body = [
  ...titlePage({
    titulo: "Viabilidad Financiera",
    subtitulo: "Costos, beneficios y dictamen de viabilidad",
    curso: "Análisis de Sistemas II",
    autor: "Augusto Ardón",
    fecha: "Septiembre 2026",
  }),

  h1("Objetivo"),
  p("Esta evaluación busca mostrar que, para el alcance de un proyecto de curso, los beneficios que aporta CreditPulse frente al análisis manual de crédito superan claramente su costo, el cual se mantuvo deliberadamente en Q0.00 mediante decisiones de stack tecnológico y de hosting orientadas a esto."),

  h1("A. Estimación de costos"),
  p("Se dividen los egresos en costos de desarrollo (inversión inicial) y costos recurrentes (operación y mantenimiento)."),
  costTable(),

  h1("B. Análisis de beneficios"),
  h2("Beneficios tangibles"),
  ...numbered([
    "Reducción del tiempo de procesamiento por solicitud: un analista que revisa manualmente cada expediente puede procesar, de forma realista, un número limitado de solicitudes al día; con CreditPulse, la evaluación individual toma segundos, y una carga masiva de miles de solicitantes se procesa en segundo plano sin requerir la atención continua del analista.",
    "Reducción de inconsistencias por criterio humano: al estandarizar la evaluación con políticas configuradas centralmente (mismos umbrales para todos los analistas de un mismo producto), se reduce la variabilidad de decisiones ante perfiles similares, lo que en un entorno real de banca ayuda a mitigar pérdidas por una aprobación de crédito de alto riesgo hecha con criterio inconsistente.",
  ]),
  h2("Beneficios intangibles"),
  ...bullets([
    "Trazabilidad y transparencia: cada evaluación queda respaldada por el detalle de qué política produjo cada resultado, útil frente a revisiones internas.",
    "Escalabilidad sin costo adicional de licenciamiento: el stack elegido (SQL Server Express, hosting gratuito) permite crecer el volumen de evaluaciones de prueba sin incurrir en nuevos costos de software.",
    "Flexibilidad del motor de políticas: ajustar un criterio de aprobación es una operación de datos, no un proyecto de desarrollo nuevo, lo que reduce el costo de mantenimiento a futuro.",
  ]),

  h1("C. Dictamen de viabilidad financiera"),
  p("El proyecto es financieramente viable para su alcance académico, debido a que:"),
  ...numbered([
    "La elección del stack tecnológico (C#, ASP.NET WebForms y SQL Server Express 2022, con hosting gratuito para la publicación) mantiene el costo total de licenciamiento e infraestructura en Q0.00, sin comprometer la funcionalidad requerida.",
    "El motor de políticas configurable evita costos futuros de desarrollo para ajustar criterios de aprobación: un cambio de umbral es una edición desde el panel de administración, no un nuevo ciclo de programación.",
    "El diseño por capas y la abstracción de la consulta a la SIB (ISibService) permiten que, si en el futuro se dispusiera de presupuesto para una integración real con la Superintendencia de Bancos o para un plan de hosting pagado (por ejemplo, para eliminar la cuota de almacenamiento o el anuncio del plan gratuito), el costo de esa migración sea acotado: se reemplaza una implementación puntual sin rediseñar el sistema.",
  ]),
];

buildAndSave("/home/claude/CreditPulse/Documentacion/Docs/04_Viabilidad_Financiera.docx", { docTitle: "Viabilidad Financiera", body })
  .then(() => console.log("OK: 04_Viabilidad_Financiera.docx"));
