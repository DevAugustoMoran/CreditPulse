const { titlePage, h1, h2, p, pb, bullets, buildAndSave } = require("./common");

const body = [
  ...titlePage({
    titulo: "Viabilidad Técnica",
    subtitulo: "Stack tecnológico, principios de arquitectura, seguridad e infraestructura",
    curso: "Análisis de Sistemas II",
    autor: "Augusto Ardón",
    fecha: "Septiembre 2026",
  }),

  h1("1. Evaluación del stack"),
  p("El proyecto es técnicamente viable sobre un stack maduro y estable, alineado con el exigido por el curso:"),
  ...bullets([
    ["Frontend (interfaz de usuario): ASP.NET WebForms sobre .NET Framework 4.8, para las pantallas, formularios y reportes (Login, Nueva Evaluación, Resultado, Batch, Dashboard, Administración)."],
    ["Backend (lógica de negocio): C#, organizado en capas BLL (reglas de negocio) y DAL (acceso a datos), más un segundo módulo de Web API 2 para las integraciones REST."],
    ["Base de datos: Microsoft SQL Server (SQL Server Express 2022 en el entorno de desarrollo local), con procedimientos almacenados para toda operación de lectura y escritura, índices sobre las columnas más consultadas, y tipos de tabla (TVP) para inserciones masivas."],
  ]),

  h1("2. Principios de arquitectura y diseño"),
  p("El código se organiza en capas (Página .aspx.cs → BLL → DAL → SQL Server) para evitar un diseño monolítico y facilitar el soporte futuro. Se aplican explícitamente los siguientes principios:"),
  ...bullets([
    ["Programación orientada a objetos: modularidad, abstracción y encapsulamiento en cada componente (modelos, servicios de negocio, repositorios de datos)."],
    ["Bajo acoplamiento y alta cohesión: la consulta a la Superintendencia de Bancos se resuelve detrás de la interfaz ISibService, con dos implementaciones intercambiables (un simulador en memoria y un cliente HTTP) — el motor de políticas, el reporte y el proceso de lotes no dependen de cuál de las dos está activa."],
    ["Simplicidad (principio KISS): el motor de decisión es un intérprete de reglas configurables por datos (tipo de regla + parámetros JSON), no un modelo predictivo; el procesamiento en segundo plano de los lotes usa la capacidad que ya trae ASP.NET (QueueBackgroundWorkItem) en vez de introducir infraestructura adicional (cola de mensajes, servicio de Windows) que el volumen del proyecto no justifica todavía — y se documenta como el primer punto a cambiar si el sistema creciera a un volumen de producción mucho mayor o a varios servidores balanceando carga."],
  ]),

  h1("3. Módulo de seguridad y control de acceso"),
  ...bullets([
    "Autenticación y credenciales: el nombre de usuario es el correo electrónico; las contraseñas se almacenan como hash PBKDF2-HMAC-SHA256 con salt único por usuario, nunca en texto plano ni cifradas de forma reversible.",
    "Políticas de contraseña: mínimo 10 caracteres, con letra, número y símbolo obligatorios, validados tanto en el alta como en el cambio de contraseña.",
    "Control de sesión: expiración configurable por inactividad y bloqueo de la cuenta tras varios intentos fallidos consecutivos.",
    "Control de roles y perfiles: permisos independientes por rol (Evaluar, Administrar Catálogo, Administrar Usuarios) que restringen pantallas completas, validados antes de ejecutar cualquier página protegida.",
    "Autenticación de las APIs de integración mediante llave compartida (X-Api-Key), validada por un filtro global registrado para todas las rutas bajo /api/v1/, de forma que ningún endpoint nuevo pueda quedar protegido por olvido.",
  ]),
  pb("Nota sobre el alcance de la API key: ", "una sola llave compartida entre todos los sistemas consumidores es una simplificación consciente, adecuada para el alcance de este curso. Antes de exponer las APIs fuera de un entorno de pruebas, un ecosistema bancario real necesitaría llaves distintas por sistema consumidor (para poder revocar la de uno sin afectar a los demás) y, probablemente, un esquema más robusto como OAuth2 client-credentials o mTLS — se documenta aquí como el siguiente paso de este módulo, no como un requisito ya resuelto."),

  h1("4. Infraestructura y procesos de datos"),
  p("A nivel de base de datos y despliegue, el proyecto documenta su propio diagrama entidad-relación, diccionario de datos y plan de mantenimiento (ver \"Documentación de la Base de Datos\"). La base de datos se construye mediante 11 scripts SQL incrementales y versionados junto con el código fuente, lo que permite reconstruir el esquema completo desde cero en cualquier instancia de SQL Server."),

  h1("5. Infraestructura física y de hosting"),
  p("Para el alcance de un proyecto de curso, se optó deliberadamente por una infraestructura simple y de costo cero, coherente con el principio KISS aplicado también al despliegue:"),
  ...bullets([
    "Entorno de desarrollo: SQL Server Express 2022 y Visual Studio (con la carga de trabajo de desarrollo web y ASP.NET) en la máquina del desarrollador, usando IIS Express para ejecutar y depurar la aplicación localmente.",
    "Entorno publicado: hosting ASP.NET gratuito sobre IIS compartido (Somee.com), con base de datos SQL Server incluida en el mismo plan, publicado mediante FTP. Es una infraestructura suficiente para el volumen de pruebas y demostración de un proyecto de curso, accesible por URL pública sin requerir que el equipo mantenga un servidor propio encendido.",
    "El proyecto CreditPulse.SibSimulador puede ejecutarse como un segundo servicio HTTP independiente (en un puerto propio) para simular con mayor fidelidad la comunicación con un sistema externo real, o desactivarse por configuración (UsarSibMock=true) para correr todo en un solo proceso durante el desarrollo.",
  ]),
  pb("Nota de diseño: ", "se evaluó deliberadamente no introducir contenerización (Docker) ni una capa de red perimetral dedicada (por ejemplo, un proxy de borde) para este entregable: el volumen y la naturaleza del proyecto —una demostración académica de un motor de evaluación crediticia, no una plataforma bancaria multi-región— no lo justifican, y añadir esa infraestructura habría consumido tiempo de desarrollo sin aportar valor al objetivo del curso. Se documenta como una decisión técnica consciente, no como una limitación no evaluada."),
];

buildAndSave("/home/claude/CreditPulse/Documentacion/Docs/03_Viabilidad_Tecnica.docx", { docTitle: "Viabilidad Técnica", body })
  .then(() => console.log("OK: 03_Viabilidad_Tecnica.docx"));
