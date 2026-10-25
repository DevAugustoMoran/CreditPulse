# CreditPulse — Núcleo de la aplicación (Fase 1 + Fase 2: Batch y Administración)

Motor de análisis de riesgo crediticio: evalúa el perfil de un solicitante
contra las políticas configuradas para el producto solicitado y emite un
veredicto por semáforo (Verde / Amarillo / Rojo), respaldado por la
información que "devuelve" la Superintendencia de Bancos (simulada por
ahora, ver más abajo).

Construido en el stack exigido por el curso: **ASP.NET WebForms (C#) +
SQL Server Express 2022**.

> **Importante:** este proyecto se generó en un entorno Linux, que no puede
> compilar ni ejecutar ASP.NET WebForms (requiere .NET Framework + IIS en
> Windows). El código está completo y listo para abrir en Visual Studio,
> pero no fue posible ejecutarlo/compilarlo aquí para verlo funcionando.
> Revísalo con calma al abrirlo la primera vez en Visual Studio — la
> sección "Solución de problemas" al final cubre los tropiezos más
> comunes al abrir un proyecto así por primera vez.

---

## 1. Qué incluye esta entrega (alcance de Fase 1)

Se acordó abordar el proyecto por fases, empezando por el núcleo:

- ✅ Login básico (usuario = correo, contraseña con hash seguro, control de
  intentos fallidos, sesión con expiración configurable).
- ✅ Formulario de **Nueva Evaluación**: datos personales + campos
  adicionales que aparecen/desaparecen según el producto elegido
  (ingresos, tipo de empleo, antigüedad laboral).
- ✅ **Motor de políticas** configurable en base de datos (no hardcodeado):
  cada producto tiene un set de políticas; cada política se evalúa y
  produce Verde/Amarillo/Rojo con una explicación legible.
- ✅ **Jerarquía de semáforo**: si alguna política da Rojo, el resultado
  general es Rojo; si no hay Rojo pero hay Amarillo, el resultado general
  es Amarillo; solo es Verde si todas las políticas son Verde.
- ✅ **Simulador de la SIB** (`SibMockService`): genera un XML plausible
  (score de buró, endeudamiento, historial de moras, listas de riesgo,
  etc.) de forma determinística por DPI, para poder hacer pruebas
  repetibles mientras no haya acceso a la API oficial.
- ✅ **Reporte de evaluación** (`Evaluacion/Resultado.aspx`): semáforo
  general, tabla con cada política y su resultado, y una pestaña aparte
  con la información cruda devuelta por la SIB.
- ✅ Dashboard con contadores (Verde/Amarillo/Rojo), listado de
  evaluaciones recientes y búsqueda por número de evaluación.

## 1.1 Fase 2 (primer entregable): Evaluaciones Batch

Ya construida en este mismo paquete, siguiendo el orden acordado
(Batch → Administración → Roles):

- ✅ **`Evaluacion/Batch.aspx`**: subes un Excel (**solo .xlsx**, formato
  moderno — se decidió no soportar el `.xls` binario legacy porque
  requiere una dependencia extra y frágil, `System.Text.Encoding.CodePages`,
  solo para ese formato en desuso), se procesa fila por fila y redirige
  al reporte del lote.
- ✅ **Producto "General"** (código `GENERAL`, ver `Database/05_Batch.sql`):
  para que el Excel de carga masiva tenga siempre las mismas columnas sin
  importar qué tan distintos sean los demás productos, se creó un
  producto dedicado a precalificación batch cuyas políticas solo dependen
  de datos básicos (edad, listas de riesgo, score de buró, endeudamiento,
  moras) — no pide ingresos, tipo de empleo ni antigüedad laboral. Cada
  fila del Excel se evalúa contra este producto, reutilizando el mismo
  `EvaluationService`/`PolicyEngine` que usa la evaluación individual.
- ✅ **Plantilla descargable** (`Content/Plantillas/PlantillaEvaluacionBatch.xlsx`,
  botón en la misma pantalla de Batch): columnas Primer Nombre, Segundo
  Nombre, Primer Apellido, Segundo Apellido, NIT, DPI, Fecha de
  Nacimiento.
- ✅ **`Evaluacion/LoteResultado.aspx`**: contadores del lote (total,
  Verde/Amarillo/Rojo, filas con error) y una tabla con cada cliente del
  archivo, su resultado y un link al detalle individual (mismo
  `Resultado.aspx` que usa la evaluación uno-por-uno).
- ✅ Una fila con datos inválidos (DPI vacío, fecha ilegible, etc.) **no
  aborta el lote completo**: se registra con su mensaje de error y el
  procesamiento sigue con la siguiente fila, así el reporte final
  muestra todas las filas del archivo.
- ✅ El dashboard ahora también muestra las cargas Batch recientes.

## 1.2 Fase 2 (segundo entregable): Administración de Productos y Políticas

Ya construida en este mismo paquete (siguiente paso del orden acordado
Batch → **Administración** → Roles):

- ✅ **`Administracion/Productos.aspx`**: listado de todos los productos
  (activos e inactivos), con el total de políticas de cada uno, botón
  para activar/desactivar (no se borra nada físicamente, se conserva el
  historial) y accesos a "Editar" y "Políticas".
- ✅ **`Administracion/ProductoForm.aspx`**: alta y edición de un
  producto — código, nombre, descripción, qué campos adicionales pide el
  formulario de Nueva Evaluación (ingresos, tipo de empleo, antigüedad
  laboral) y si está activo.
- ✅ **`Administracion/Politicas.aspx`**: listado de las políticas de un
  producto (orden, código, nombre, tipo de regla en texto legible,
  estado), con activar/desactivar y acceso a "Editar".
- ✅ **`Administracion/PoliticaForm.aspx`**: alta y edición de una
  política — código, nombre, descripción, orden, y el **tipo de regla**
  elegido de un catálogo fijo (`Models/Enums.cs → TiposDeRegla.Catalogo`)
  que además muestra un **ejemplo de `ParametrosJson`** para el tipo
  seleccionado (se actualiza sin recargar la página, vía `UpdatePanel`).
- ✅ **`BLL/AdminCatalogoService.cs`**: valida los datos antes de guardar
  (campos requeridos, que el tipo de regla exista en el catálogo, que
  `ParametrosJson` sea JSON válido) y traduce el error de SQL Server por
  código duplicado (`UQ_Productos_Codigo` / `UQ_Politicas_Producto_Codigo`)
  a un mensaje en español en vez de mostrar la excepción cruda.
- ✅ Activar/desactivar es un cambio de estado, no un `DELETE`: un
  producto o política desactivada deja de evaluarse (`PolicyEngine`) y
  deja de aparecer en Nueva Evaluación / Batch, pero las evaluaciones ya
  hechas con ella conservan su historial intacto.
- ✅ Nuevo grupo "Administración" en el menú lateral (`Site.Master`).

## 1.3 Fase 2 (tercer y último entregable): Roles y Permisos

Ya construido en este mismo paquete — cierra el orden acordado
Batch → Administración → **Roles**, con la opción elegida de
**restringir pantallas completas por rol** (no botones sueltos):

- ✅ **`BasePage.RolesPermitidos`**: cada página que necesita
  restricción sobreescribe esta propiedad con la lista de roles que
  pueden abrirla; `BasePage.OnInit` la valida contra el rol del usuario
  en sesión *antes* de ejecutar el `Page_Load` de la página, y redirige
  a `AccesoDenegado.aspx` si el rol no está autorizado. Una página sin
  restricción (el valor por defecto, `null`) sigue abierta para
  cualquier usuario autenticado, igual que en Fase 1/2 anteriores.
- ✅ **Tres roles** (`Models/RolesDeSistema.cs`, ya sembrados desde
  `04_SeedData.sql`):

  | Rol | Puede ver |
  |---|---|
  | **Administrador** | Todo: Dashboard, Nueva Evaluación, Batch, reportes **y** Administración de Productos/Políticas. |
  | **Analista de Crédito** | Dashboard, Nueva Evaluación, Evaluaciones Batch y reportes. Sin acceso a Administración. |
  | **Consulta** | Solo Dashboard y reportes (`Resultado.aspx` / `LoteResultado.aspx`) en modo lectura. No puede crear evaluaciones ni entrar a Administración. |

- ✅ Si alguien intenta abrir una pantalla que no le corresponde
  (incluso escribiendo la URL directamente), llega a
  **`AccesoDenegado.aspx`** con un mensaje claro y un botón para volver
  al Dashboard, en vez de un error genérico o una pantalla a medio
  cargar.
- ✅ El menú lateral (`Site.Master`) y los botones "+ Nueva Evaluación"
  / "+ Nueva Carga Batch" (Dashboard y reporte de lote) se **ocultan**
  según el rol — es una ayuda de navegación además de la restricción
  real en `BasePage`, así que aunque el enlace no se vea, la pantalla
  sigue protegida si se accede por URL directa.
- ✅ **Usuarios de prueba** para los tres roles (`Database/07_Roles.sql`,
  ver tabla en la sección 3).

## 1.4 Rediseño visual (tema oscuro, tipografía y responsividad)

Última pasada, ahora que la funcionalidad de las tres fases está cerrada
(se dejó explícitamente para el final, como se acordó). Todo el cambio
vive en `Content/site.css` y `Site.Master` — ninguna página cambió su
estructura de datos ni su lógica, así que aplica automáticamente a las
~20 pantallas del sistema:

- ✅ **Tema oscuro/nocturno** en toda la aplicación (antes solo el
  sidebar y el hero del login eran oscuros; el resto — tarjetas, tablas,
  formularios, login, pantalla de error — era claro). Los colores del
  semáforo (verde/amarillo/rojo) se reajustaron para verse bien y seguir
  siendo legibles sobre fondo oscuro.
- ✅ **Tipografía más grande**: tamaño base de 14.5px a 16.5px (se
  refleja en toda la interfaz porque el resto de tamaños está en `rem`),
  encabezados más grandes (`h1` de 1.7rem a ~2rem) y más espaciado.
- ✅ **Diseño más adaptativo/responsivo**: el menú lateral, que antes
  simplemente desaparecía en pantallas angostas (sin forma de navegar
  salvo volver atrás), ahora es un **panel deslizable ("drawer")** que
  se abre con un botón de hamburguesa en la barra superior y se cierra
  tocando el fondo oscuro, la tecla Escape, o el enlace de navegación
  (lógica en JavaScript simple, sin dependencias, al final de
  `Site.Master`). Tarjetas, formularios y tablas también se reacomodan
  en columnas según el ancho de pantalla.
- ✅ Detalles de acabado: transiciones suaves en botones/enlaces,
  contorno de foco visible para navegación por teclado, y una barra de
  desplazamiento (scrollbar) con estilo oscuro a juego.

## 1.5 Fase 3: Usuarios/Roles administrables, reporte SIB legible, Batch a gran escala y API SIB independiente

Cuarta tanda de trabajo, construida en el orden acordado explícitamente
con el usuario (Usuarios/Roles → Reporte SIB → Batch → API SIB), a
partir de cuatro preguntas concretas: *"¿puedo crear usuarios y roles
desde un panel?"*, *"¿el reporte de la SIB puede ser legible, no JSON/XML
crudo?"*, *"¿qué pasa si subo un Excel de 10,000 clientes?"* y *"¿se
puede consultar la SIB directamente, o armamos una API propia?"*.

### 1.5.1 Panel de administración de Usuarios y Roles

- ✅ **`Administracion/Usuarios.aspx`** y **`UsuarioForm.aspx`**: alta,
  edición, cambio de contraseña y activar/desactivar usuarios desde la
  interfaz — ya no hace falta tocar la base de datos para dar de alta a
  alguien. La contraseña se valida con la misma política y se guarda con
  el mismo hash PBKDF2 que el login (`Security/PasswordHasher.cs`).
- ✅ **`Administracion/Roles.aspx`** y **`RolForm.aspx`**: alta, edición y
  activar/desactivar roles, marcando **tres permisos independientes**
  (Evaluar, Administrar Catálogo, Administrar Usuarios) con checkboxes.
- ✅ **Rediseño del modelo de permisos** (necesario para que "crear
  roles" sirva de algo): en las fases anteriores, cada pantalla
  restringida comparaba el **nombre** del rol del usuario contra una
  lista fija en código (`BasePage.RolesPermitidos` vs.
  `RolesDeSistema.Administrador/AnalistaCredito/Consulta`) — si alguien
  creaba un rol nuevo desde un panel de administración, ese rol nuevo no
  tendría acceso a nada, porque el código no lo reconocía por nombre. Se
  reemplazó por un modelo de **permisos como banderas** en la tabla
  `Roles` (`PuedeEvaluar`, `PuedeAdministrarCatalogo`,
  `PuedeAdministrarUsuarios`, columnas nuevas vía
  `Database/08_UsuariosRoles.sql`) y `BasePage.PermisoRequerido` ahora
  pide un permiso (`Models.Permiso.Evaluar` /
  `AdministrarCatalogo` / `AdministrarUsuarios`), no un nombre de rol.
  Así, un rol creado desde `RolForm.aspx` con el permiso correcto
  marcado funciona de inmediato en las pantallas protegidas, sin tocar
  código. Los tres roles sembrados (Administrador / Analista de Crédito
  / Consulta) siguen funcionando igual que antes; solo cambió *cómo* se
  valida el acceso, no el resultado.
- ✅ **Candados de sentido común**: un usuario no puede desactivarse a sí
  mismo, y no se puede desactivar (ni quitarle el permiso
  AdministrarUsuarios a) un rol mientras todavía tenga usuarios activos
  asignados — para no dejar la aplicación sin ningún administrador que
  pueda revertir el cambio.

### 1.5.2 Reporte de la SIB legible para un usuario no técnico

Antes, la pestaña "Información SIB" de `Evaluacion/Resultado.aspx`
mostraba el XML crudo tal cual lo "devuelve" la SIB — útil para
depuración, pero no para un analista de crédito sin trasfondo técnico.

- ✅ **`BLL/SibXmlParser.cs`** reconstruye un objeto `SibPerfil`
  estructurado a partir del mismo XML que ya se guardaba en
  `Evaluaciones.SibRawXml` (no se agregó ninguna columna nueva a la
  base de datos).
- ✅ La pestaña "Información SIB" ahora muestra: una **tarjeta de score**
  con medidor visual y descripción en palabras ("Score alto/medio/bajo"),
  **Historial Crediticio** (créditos activos/cancelados, moras,
  días de mora), **Listas de Riesgo** (local/internacional/OFAC, con
  íconos Sí/No en vez de `true`/`false`), **Referencias Internas**
  (cliente existente, créditos previos) y **Metadatos de la consulta**
  (fecha, fuente, tiempo de respuesta) — todo en español, sin una sola
  llave ni etiqueta XML a la vista.
- ✅ El XML técnico **no se eliminó**: queda disponible en una sección
  colapsable ("Ver XML técnico") debajo del reporte legible, para quien
  sí lo necesite (soporte, auditoría).
- ✅ Si el XML guardado es de un formato antiguo o no se puede
  interpretar, la pantalla cae de vuelta a las columnas planas que ya
  existían (`SibScoreBuro`, `SibNivelEndeudamiento`, `SibEnListaNegra`)
  en vez de mostrar un error.

### 1.5.3 Evaluaciones Batch a gran escala (miles de filas)

**El problema real, diagnosticado antes de tocar código:** con la
implementación de Fase 2, subir un Excel de 10,000 clientes causaba
tres problemas serios:
1. **~90,000 viajes de ida y vuelta a SQL Server** en una sola
   petición HTTP (una consulta a la SIB + varios `INSERT` por cada una
   de las 10,000 filas), todo dentro del tiempo de espera de un
   `Page_Load` — con altísima probabilidad de **timeout** del
   navegador/IIS antes de terminar.
2. **Datos huérfanos si el proceso se caía a la mitad**: como todo el
   lote se guardaba al final (o fila por fila sin marcar progreso), un
   error en la fila 6,000 podía dejar la base de datos en un estado
   ambiguo, sin forma clara de saber cuáles de las primeras 6,000 sí
   quedaron bien guardadas.
3. Sin ninguna indicación de progreso, el usuario solo veía una página
   "cargando" sin saber si el proceso sigue vivo o se trabó.

**La solución implementada (arquitectura completa en segundo plano,
la opción más a fondo, según lo decidido):**

- ✅ **Procesamiento en segundo plano**: `btnProcesar_Click` ya no
  procesa el archivo dentro de la petición HTTP. Valida y lee el Excel
  a memoria, crea el registro del lote con estado `Procesando`, y
  encola el trabajo pesado con
  `HostingEnvironment.QueueBackgroundWorkItem` (`BLL/BatchEvaluationService.cs`)
  — la página redirige de inmediato al reporte del lote, sin esperar a
  que las 10,000 filas terminen de evaluarse.
- ✅ **Progreso incremental y reanudable ante fallas**: el lote se
  procesa en bloques de 200 filas; después de cada bloque se guarda en
  la base de datos y se actualiza `Lotes.TotalProcesados` — así, si el
  proceso se interrumpe a la mitad, las filas ya procesadas quedan
  correctamente guardadas y contadas (se corrige el problema de datos
  huérfanos), y el lote queda marcado `Error` con el motivo, no
  simplemente "colgado" para siempre.
- ✅ **Inserciones masivas con parámetros con valores de tabla (TVP)**:
  en vez de un `INSERT` por cada fila y por cada política evaluada, se
  crearon tipos de tabla en SQL Server (`EvaluacionPoliticaTVP`,
  `LoteEvaluacionTVP`, `Database/09_BatchAsincrono.sql`) que permiten
  enviar un bloque completo de filas en **una sola llamada** a la base
  de datos (`sp_EvaluacionPoliticas_InsertarLote`,
  `sp_LoteEvaluaciones_InsertarLote`), reduciendo drásticamente el
  número de viajes de ida y vuelta.
- ✅ **Producto y políticas se leen una sola vez por lote**, no una vez
  por fila (`EvaluationService.EvaluarSinGuardar` reutiliza la misma
  lógica de negocio que la evaluación individual, sin repetir lecturas
  redundantes a la base de datos).
- ✅ **Pantalla de progreso**: mientras el lote tiene estado
  `Procesando`, `Evaluacion/LoteResultado.aspx` muestra una barra de
  progreso ("3,400 de 10,000 procesados") y se **autorrefresca cada 3
  segundos** (`<meta http-equiv="refresh">`) hasta que el lote termina
  — una solución deliberadamente simple (sin SignalR ni JavaScript de
  sondeo/polling), suficiente para el alcance del curso y documentada
  aquí como decisión consciente.
- ✅ Compatible con lotes ya existentes de Fase 2: la columna `Estado`
  nueva tiene `DEFAULT 'Completado'` para que los lotes históricos
  sigan mostrándose como terminados.

### 1.5.4 La SIB como servicio HTTP independiente (API propia)

Sobre la pregunta de si se puede consultar directamente a la SIB: la
Superintendencia de Bancos de Guatemala solo ofrece autoconsulta
personal del historial crediticio (portal de banca en línea del propio
banco, o trámite presencial) — **no** una API pública para consumo
automatizado de terceros. Por eso se mantiene un simulador, pero se
llevó un paso más allá de lo que pedía Fase 1: en vez de una clase C#
interna (`SibMockService`, que corre dentro del mismo proceso de
CreditPulse.Web), ahora también existe una versión que corre como
**servicio HTTP separado**, para que la integración se sienta como
sería integrarse con un sistema externo real.

- ✅ **`CreditPulse.SibSimulador`**: segundo proyecto en la misma
  solución (`CreditPulse.sln`), un ASP.NET Web API 2 independiente que
  expone `GET api/perfil?dpi=...&nit=...&nombre=...` y responde el
  mismo XML que ya generaba `SibMockService` — se puede publicar y
  ejecutar por separado, en su propio proceso/puerto (`44399` por
  defecto en IIS Express), tal como estaría desplegado un sistema de
  otra institución.
- ✅ **`BLL/SibApiClientService.cs`** (en `CreditPulse.Web`): nueva
  implementación de `ISibService` que llama a ese servicio por HTTP
  (`HttpClient`) y reutiliza `BLL/SibXmlParser.cs` para interpretar la
  respuesta — el resto de la aplicación (motor de políticas, reporte,
  batch) no distingue si la SIB fue consultada en memoria o por HTTP.
- ✅ **Interruptor por configuración**: el appSetting `UsarSibMock` en
  `CreditPulse.Web/Web.config` decide cuál se usa
  (`BLL/EvaluationService.CrearSibService()`):
  - `true` (por defecto): simulador en memoria (`SibMockService`), para
    poder correr y probar `CreditPulse.Web` sin depender de tener el
    segundo proyecto corriendo también.
  - `false`: llama por HTTP a la URL de `SibApiBaseUrl` (apuntando a
    `CreditPulse.SibSimulador`).
- ✅ Si el servicio HTTP no responde (apagado, URL mal configurada,
  timeout), se muestra un mensaje claro indicando qué revisar, en vez
  de una excepción técnica cruda.

## 1.6 Fase 5: APIs de integración con el ecosistema bancario, y política RCI

CreditPulse está pensado como el **eje** que recolecta y evalúa la
información crediticia; el resto del ecosistema bancario (otros
sistemas del banco, no personas con navegador) necesita poder pedirle
esa información. Se construyeron 3 APIs REST dentro del mismo proyecto
`CreditPulse.Web` (carpeta `Api/`), conviviendo con las pantallas
WebForms — **no** un proyecto aparte como `CreditPulse.SibSimulador`,
porque a diferencia de la SIB (que simula un sistema *externo*), estas
APIs son del sistema CreditPulse mismo.

### 1.6.1 Las 3 APIs

Las tres devuelven **JSON o XML según el header `Accept`** del
llamador (`application/json` o `application/xml`; si no se envía
`Accept`, responden JSON) — Web API 2 lo resuelve automáticamente
(content negotiation), sin código adicional. Las tres requieren el
header `X-Api-Key` (ver 1.6.3).

**1. `POST /api/v1/evaluaciones`** — evaluación crediticia completa.
Recibe los mismos datos que pide `Evaluacion/Nueva.aspx` (producto,
datos del cliente, ingresos, etc.), ejecuta la evaluación real (SIB +
`PolicyEngine`), la guarda, y devuelve el número de evaluación, el
color general, y el detalle de cada política evaluada — el mismo
resultado que vería un analista en `Resultado.aspx`, en un formato que
otro sistema puede consumir directamente.

```json
// Request
POST /api/v1/evaluaciones
X-Api-Key: <tu llave>
Content-Type: application/json

{
  "productoCodigo": "TARJETA_CREDITO",
  "primerNombre": "Ana", "primerApellido": "López",
  "nit": "1234-5", "dpi": "3478953810101",
  "fechaNacimiento": "1990-03-14",
  "ingresosMensuales": 6500, "tipoEmpleo": "Asalariado", "antiguedadLaboralMeses": 30
}

// Response 201 Created
{
  "evaluacionId": 481,
  "productoCodigo": "TARJETA_CREDITO", "productoNombre": "Tarjeta de Crédito Clásica",
  "clienteNombreCompleto": "Ana López", "nit": "1234-5", "dpi": "3478953810101",
  "resultadoGeneral": "AMARILLO", "resultadoGeneralEtiqueta": "Requiere Revisión",
  "politicas": [
    { "politicaId": 21, "nombre": "Edad Mínima", "resultado": "VERDE", "detalle": "...", "orden": 1 },
    { "politicaId": 27, "nombre": "RCI Bancario (Relación Cuota/Ingreso)", "resultado": "AMARILLO", "detalle": "...", "orden": 7 }
  ],
  "sib": { "scoreBuro": 605, "nivelEndeudamiento": 52.30, "enListaNegra": false },
  "fechaEvaluacion": "2026-09-24T22:40:00"
}
```

También existe **`GET /api/v1/evaluaciones/{id}`** (el mismo formato
de respuesta, para una evaluación ya hecha — creada por API, por
`Nueva.aspx` o por un lote Batch).

**2. `GET /api/v1/sib/perfil?dpi=...&nit=...&nombre=...`** — los datos
más importantes que devuelve la SIB para un cliente (score,
endeudamiento, historial de moras, listas de riesgo, referencias
internas), sin necesidad de crear una evaluación. Usa el mismo
interruptor `UsarSibMock` que el resto de la aplicación: si está en
`false`, esta API llama por HTTP a `CreditPulse.SibSimulador`, igual
que lo hace `Evaluacion/Nueva.aspx`.

**3. `GET /api/v1/evaluaciones/{id}/variables-score`** — ver 1.6.2.

### 1.6.2 API de variables ponderadas para el score (la más particular)

El pedido original era: *"no sé cómo el sistema calcula el score, pero
normalmente se pondera entre 7 y 8 variables; que la API devuelva esas
variables para que otros sistemas calculen el score a su manera."*
Vale la pena ser explícito sobre esto: **CreditPulse no calcula ni
guarda un score numérico propio**. El "score" que se ve en todas
partes (`SibScoreBuro`) es el que reporta la SIB tal cual; lo que sí
construye CreditPulse es el semáforo Verde/Amarillo/Rojo del
`PolicyEngine`, que es un veredicto por política, no un número
ponderado.

Por eso, `GET /api/v1/evaluaciones/{id}/variables-score` no expone "el
cálculo interno de un score" (no existe tal cosa) — expone un
**modelo de referencia de 8 variables y pesos sugeridos**, construido
para esta entrega, documentado aquí para que se pueda ajustar con
datos reales más adelante:

| Variable | Peso | De dónde sale |
|---|---|---|
| Score de Buró | 25% | SIB (`ScoreBuro`) |
| Nivel de Endeudamiento | 15% | SIB (`NivelEndeudamiento`) |
| RCI Bancario | 15% | Calculado (ver 1.6.4) |
| Moras últimos 12 meses | 15% | SIB (`MorasUltimos12Meses`) |
| Días de Mora Máximo | 5% | SIB (`DiasMoraMaximo`) |
| Ingresos Mensuales declarados | 10% | Datos de la solicitud |
| Antigüedad Laboral | 10% | Datos de la solicitud |
| Edad | 5% | Calculada de la fecha de nacimiento |

Cada variable se devuelve con su **valor crudo** (`valor`, en su
unidad real — puntos, %, Q, meses) y, además, ese mismo valor llevado
a una **escala 0-100** (`valorNormalizado0a100`, donde 100 es "mejor
para el cliente"), para que sea comparable entre variables con
unidades distintas. El sistema consumidor puede usar cualquiera de los
dos: el valor crudo con su propia fórmula, o el normalizado con sus
propios pesos. La respuesta incluye también `scoreSugerido0a100` (la
suma ponderada de los normalizados, como *un* ejemplo de cómo
combinarlas) y una `nota` aclarando que no es "el score oficial" de
nada. Si la evaluación es de un cliente sin ingreso declarado (posible
en el producto "General" de Batch), las variables que dependen del
ingreso (RCI, Ingresos) se marcan con una nota en vez de inventar un
valor.

### 1.6.3 Autenticación: API key compartida (simplificación consciente)

Las 3 rutas exigen el header `X-Api-Key` con el valor del appSetting
`ApiKeyIntegraciones` (`CreditPulse.Web/Web.config`) — se valida en
`Api/Filters/ApiKeyAuthorizeAttribute.cs`, registrado como filtro
**global** (`WebApiConfig.Register`) para que sea imposible agregar un
endpoint nuevo bajo `/api/v1/` y olvidarlo proteger. Sin el header, o
con un valor incorrecto: `401 Unauthorized`.

Es deliberadamente simple —una sola llave compartida entre todos los
sistemas consumidores, en vez de una llave distinta por sistema o un
esquema OAuth2 client-credentials con expiración/rotación— para no
sobrecargar el alcance del curso. **Antes de exponer esto fuera de un
entorno de pruebas**, un ecosistema bancario real necesitaría llaves
por sistema consumidor (para poder revocar la de uno sin afectar a los
demás) y, probablemente, mTLS o OAuth2 en vez de una llave fija en un
header — se documenta aquí como el siguiente paso, no como algo ya
resuelto. La llave que trae este entregable
(`ApiKeyIntegraciones` en `Web.config`) es solo un ejemplo generado
para poder probar; cámbiala antes de usar esto en serio.

Las evaluaciones creadas por `POST /api/v1/evaluaciones` quedan
atribuidas a una cuenta de servicio (`integracion@creditpulse.com`,
rol "Integración API", ver `Database/11_ApiIntegraciones.sql`) en vez
de a un analista humano, porque `Evaluaciones.UsuarioId` es obligatorio
para trazabilidad y aquí no hay ninguna persona que inició sesión. Esa
cuenta nunca inicia sesión por el formulario de Login — la
autenticación real de las APIs es la API key, no esa contraseña.

### 1.6.4 Política nueva: RCI Bancario (Tarjeta de Crédito)

RCI = Relación Cuota/Ingreso: qué tan posible es que el cliente pague
un crédito, dividiendo sus deudas entre sus ingresos. Se evaluó
agregar un campo de "monto solicitado" para calcular una cuota real,
pero se descartó (decisión del usuario): en vez de eso, la deuda
mensual del cliente se **estima** a partir de lo que ya reporta la SIB
(`NivelEndeudamiento%` aplicado sobre los `IngresosReportados` al
buró), y esa deuda estimada se compara contra el ingreso que el
cliente declaró en la solicitud **actual**:

```
RCI% = (NivelEndeudamiento_SIB / 100 × IngresosReportados_SIB) / IngresosMensuales_declarados × 100
```

Es deliberadamente distinta de la política "Endeudamiento" que ya
existía desde Fase 1 (esa solo compara el % crudo del buró contra un
umbral fijo, sin cruzarlo con nada más): si el ingreso que el cliente
declaró ahora coincide con el que tiene reportado en el buró, el RCI
da el mismo resultado que Endeudamiento; si declaró un ingreso
distinto (mayor o menor), el RCI se separa y refleja mejor su
capacidad de pago real con el ingreso que está declarando en este
momento — que es precisamente la señal que un RCI busca detectar.

- ✅ Nuevo `TipoRegla` `RCI_BANCARIO` en `BLL/PolicyEngine.cs`
  (`EvaluarRciBancario`), con umbrales configurables vía
  `ParametrosJson` igual que cualquier otra política (`{"verde":35,
  "amarillo":45}` por defecto).
- ✅ Agregada como política #7 de **Tarjeta de Crédito Clásica**
  (`Database/10_RciBancario.sql`); no se tocó ningún otro producto.
- ✅ Si la evaluación no tiene ingreso mensual declarado, la política
  no falla ni asume un valor: da Amarillo con el mensaje "No se puede
  calcular el RCI sin un ingreso mensual declarado".

**Queda para una futura iteración** (fuera del alcance acordado para
este curso):

- Auditoría detallada de cambios de estado (RF-08 del levantamiento de
  requerimientos).
- Cifrado a nivel de columna para NIT/DPI (mencionado en Viabilidad
  Operativa). Ver la nota de seguridad más abajo.
- Conexión a la API real de la SIB en lugar del simulador (no existe
  API pública oficial disponible, ver sección 1.5.4).
- Llaves de API por sistema consumidor y/o OAuth2/mTLS en vez de una
  sola llave compartida (ver 1.6.3).

---

## 1.7 Versión CP Analítica (octubre 2026)

Resumen de los cambios de esta versión. El detalle completo de cómo funciona
todo está en [`memory.md`](memory.md).

- **Marca:** la aplicación se llama **CP Analítica (CreditPulse Analítica)**, con
  el logo nuevo en el menú, el login, la pantalla de error y como favicon.
- **Tres nombres:** Nueva Evaluación y Batch aceptan un *Tercer Nombre* opcional
  (script `12_TercerNombre.sql`; la plantilla de Excel trae la columna nueva).
- **DPI validado:** además de los 13 dígitos se valida el departamento (01–22), el
  municipio de ese departamento y el dígito verificador (`BLL/DpiValidator.cs`).
  Aplica en pantalla y Batch; **no** en las APIs `/api/v1`.
- **Hora corregida:** la base guarda UTC y se muestra siempre en hora de
  Guatemala (`BLL/ZonaHoraria.cs`, appSetting `ZonaHorariaId`).
- **Reportes** (menú *Reportes*, script `13_Reportes.sql`): evaluaciones por mes
  (color, cliente, score, fecha) y clientes por número de batch (con la
  información de la SIB). Exportan a CSV.
- **Descargas de reportes:** *Descargar Excel*, *PDF* y *CSV* (todas las filas del
  filtro, no solo la página en pantalla). Sin librerías externas.
- **Desbloquear usuarios:** en *Usuarios y Roles* aparece "Desbloquear" en las
  cuentas bloqueadas por 5 intentos fallidos.
- **Login:** se quitó la nota con el usuario de prueba.
- **Modo claro / oscuro** con botón en la barra superior (se recuerda por
  navegador) y revisión de responsividad de 320 px a 2560 px.
- **Tokens:** `POST /api/v1/auth/token | refresh | revoke` (JWT de 15 min +
  refresh token rotativo de 7 días; script `14_AuthTokens.sql`). Es **adicional**:
  `X-Api-Key` sigue funcionando igual.
- **Docker:** `docker compose up -d` levanta SQL Server 2022 Express con todos los
  scripts cargados y la API de la SIB (`CreditPulse.SibApi`, puerto 5080).
- **SIB por API:** `UsarSibMock=true` (por defecto) simula la SIB dentro de la
  app; con `false` se consulta por HTTP a `SibApiBaseUrl` (la API Docker en
  `http://localhost:5080/`, o `CreditPulse.SibSimulador`).
- **APIs existentes sin cambios.** El único toque dentro de `Api/` es una línea
  aditiva en `WebApiConfig.cs` para registrar el handler de tokens.
- **Publicar en Somee:** ver [`Deploy/Somee/GUIA_ACTUALIZAR_SOMEE.md`](Deploy/Somee/GUIA_ACTUALIZAR_SOMEE.md).

### Base de datos local con Docker

```
docker compose up -d            # SQL Server + carga de scripts 01-14 + API de la SIB
docker compose logs db-init     # confirmar que los scripts corrieron
docker compose down             # apagar (los datos se conservan en el volumen)
docker compose down -v          # apagar y borrar los datos
```

Requisitos: Docker Desktop. `Web.config` ya apunta a `localhost,1433` con el usuario
`cp_app`. Para cambiar contraseñas o puertos, copia `.env.example` a `.env`.
Para volver a cargar desde cero: `docker compose down -v` y `docker compose up -d`.

---

## 2. Estructura del proyecto

```
CreditPulse/
├── CreditPulse.sln
├── CreditPulse.Web/              Proyecto ASP.NET Web Application (WebForms) — la aplicación principal
│   ├── Web.config                Connection string, Forms Auth, sesión, políticas, UsarSibMock/SibApiBaseUrl
│   ├── Global.asax(.cs)
│   ├── Site.Master(.cs)          Layout general (sidebar + topbar)
│   ├── Login.aspx(.cs)
│   ├── Default.aspx(.cs)         Dashboard
│   ├── AccesoDenegado.aspx(.cs)  Pantalla mostrada cuando el permiso no alcanza
│   ├── Evaluacion/
│   │   ├── Nueva.aspx(.cs)          Formulario de captura + evaluación individual
│   │   ├── Resultado.aspx(.cs)      Reporte de una evaluación (con "Información SIB" legible)
│   │   ├── Batch.aspx(.cs)          Carga del Excel para Evaluaciones Batch (procesa en 2do plano)
│   │   └── LoteResultado.aspx(.cs)  Reporte de un lote (progreso en vivo + detalle por cliente)
│   ├── Administracion/
│   │   ├── Productos.aspx(.cs)      Listado de productos (activar/desactivar)
│   │   ├── ProductoForm.aspx(.cs)   Alta/edición de un producto
│   │   ├── Politicas.aspx(.cs)      Listado de políticas de un producto
│   │   ├── PoliticaForm.aspx(.cs)   Alta/edición de una política (con ejemplo de JSON dinámico)
│   │   ├── Usuarios.aspx(.cs)       Listado de usuarios (activar/desactivar)
│   │   ├── UsuarioForm.aspx(.cs)    Alta/edición de un usuario (rol, contraseña)
│   │   ├── Roles.aspx(.cs)          Listado de roles (activar/desactivar)
│   │   └── RolForm.aspx(.cs)        Alta/edición de un rol (3 permisos por checkbox)
│   ├── Api/                      APIs REST de integración (/api/v1/..., ver sección 1.6)
│   │   ├── App_Start/WebApiConfig.cs      Rutas + filtro global de API key
│   │   ├── Filters/ApiKeyAuthorizeAttribute.cs
│   │   ├── Controllers/
│   │   │   ├── EvaluacionesApiController.cs   POST/GET evaluaciones, GET variables-score
│   │   │   └── SibApiController.cs            GET sib/perfil
│   │   └── Models/                EvaluacionApiModels, SibApiModels, ScoreApiModels (VariablesScoreCalculator)
│   ├── Models/                   Clases de datos (POCOs) + enums (incluye Permiso.cs, Rol.cs)
│   ├── DAL/                      Acceso a datos (ADO.NET + stored procedures)
│   ├── BLL/                      Lógica de negocio: PolicyEngine, EvaluationService,
│   │                              BatchEvaluationService (2do plano), SibMockService /
│   │                              SibApiClientService (ISibService), SibXmlParser,
│   │                              AdminCatalogoService, AdminUsuariosService
│   ├── Security/                 Hash de contraseñas, helper de sesión
│   └── Content/
│       ├── site.css              Estilos
│       └── Plantillas/PlantillaEvaluacionBatch.xlsx
├── CreditPulse.SibSimulador/     Proyecto ASP.NET Web API 2 — simulador de la SIB como
│   │                              servicio HTTP independiente (ver sección 1.5.4)
│   ├── Web.config
│   ├── Global.asax(.cs)
│   ├── App_Start/WebApiConfig.cs
│   ├── Controllers/PerfilController.cs   GET api/perfil?dpi=...&nit=...&nombre=...
│   └── Models/
│       ├── SibPerfilSimulado.cs          Mismo perfil que Models/SibPerfil.cs de CreditPulse.Web
│       └── SibPerfilGenerator.cs         Mismo algoritmo determinístico + serializador XML
└── Database/
    ├── 01_CreateDatabase.sql
    ├── 02_CreateTables.sql
    ├── 03_StoredProcedures.sql
    ├── 04_SeedData.sql
    ├── 05_Batch.sql               Producto "General" + tablas/SPs de Lotes
    ├── 06_Administracion.sql      Stored procedures CRUD de Productos y Políticas
    ├── 07_Roles.sql               Usuarios de prueba para Analista de Crédito y Consulta
    ├── 08_UsuariosRoles.sql       Permisos por rol (banderas) + CRUD de Usuarios/Roles
    ├── 09_BatchAsincrono.sql      Estado/progreso de Lotes + tipos de tabla (TVP) para inserción masiva
    ├── 10_RciBancario.sql         Política RCI Bancario para Tarjeta de Crédito (ver 1.6.4)
    └── 11_ApiIntegraciones.sql    Rol y usuario de servicio para las APIs (ver 1.6.3)
```

### Arquitectura por capas

`Página .aspx.cs` → `BLL` (reglas de negocio) → `DAL` (ADO.NET, siempre
vía stored procedures) → `SQL Server`. El motor de políticas
(`BLL/PolicyEngine.cs`) es el corazón del sistema: interpreta cada fila de
la tabla `Politicas` (tipo de regla + parámetros en JSON) y decide el
color. Agregar o ajustar una política es un `INSERT`/`UPDATE` en la base
de datos, no un cambio de código.

`CreditPulse.SibSimulador` es un proyecto aparte y **no** es referenciado
por `CreditPulse.Web` a nivel de código (no comparten ensamblado): se
comunican solo por HTTP/XML, igual que se comunicaría `CreditPulse.Web`
con la SIB real el día que exista una API oficial. Es opcional: si no lo
levantas, `CreditPulse.Web` sigue funcionando normal con `UsarSibMock=true`.

---

## 3. Cómo ejecutarlo en Visual Studio

1. **Base de datos:** en SQL Server Management Studio (o Azure Data
   Studio), conectado a tu instancia de SQL Server Express 2022, ejecuta
   en orden:
   `Database/01_CreateDatabase.sql` → `02_CreateTables.sql` →
   `03_StoredProcedures.sql` → `04_SeedData.sql` → `05_Batch.sql` →
   `06_Administracion.sql` → `07_Roles.sql` → `08_UsuariosRoles.sql` →
   `09_BatchAsincrono.sql` → `10_RciBancario.sql` →
   `11_ApiIntegraciones.sql` → `12_TercerNombre.sql` → `13_Reportes.sql` →
   `14_AuthTokens.sql`. **O bien**, con Docker (recomendado): `docker compose up -d`
   y se cargan solos (ver 1.7).
   (Si ya tenías una entrega anterior corriendo, solo te falta ejecutar
   los scripts que te falten en orden; todos son incrementales y no
   borran nada de lo anterior.)
2. **Connection string:** `Web.config` ya viene apuntando a la base de Docker
   (`localhost,1433`, usuario `cp_app`). Si usas SQL Server Express instalado en
   Windows, usa la línea comentada de ahí. Si no, ajusta
   `Data Source=localhost\SQLEXPRESS` si tu instancia tiene otro nombre.
3. **Abrir el proyecto:** abre `CreditPulse.sln` con Visual Studio 2019 o
   2022 (con la carga de trabajo "Desarrollo web y ASP.NET" instalada).
   La solución trae **dos proyectos**: `CreditPulse.Web` (la aplicación)
   y `CreditPulse.SibSimulador` (opcional, ver paso 6).
4. **Paquetes NuGet:** `CreditPulse.Web` usa `Newtonsoft.Json`,
   `ExcelDataReader` y `ExcelDataReader.DataSet` (declarados en
   `packages.config`, usados por Evaluaciones Batch para leer el Excel —
   solo se soporta `.xlsx`, ver nota abajo); `CreditPulse.SibSimulador`
   usa `Microsoft.AspNet.WebApi.Core` y `Microsoft.AspNet.WebApi.WebHost`.
   Al abrir la solución, Visual Studio debería restaurarlos
   automáticamente; si no, clic derecho en la solución → "Restaurar
   paquetes NuGet".
5. **Ejecutar:** clic derecho en `CreditPulse.Web` → "Establecer como
   proyecto de inicio" (si no lo está ya) → F5. Debería abrir
   `Login.aspx` con el servidor de desarrollo de Visual Studio (IIS
   Express). Con `UsarSibMock=true` (el valor por defecto en
   `Web.config`) esto es todo lo que hace falta — no necesitas levantar
   `CreditPulse.SibSimulador` para nada de lo demás.
6. **(Opcional) Probar la SIB como servicio HTTP separado:** para ver el
   simulador corriendo como su propio servicio en vez de en memoria —
   ver sección 1.5.4 —, clic derecho en la solución → "Propiedades" →
   "Varios proyectos de inicio" → poner `CreditPulse.Web` y
   `CreditPulse.SibSimulador` ambos en "Iniciar", F5, y confirmar en la
   consola/URL de `CreditPulse.SibSimulador` en qué puerto quedó
   (`https://localhost:44399/` por defecto). Luego, en
   `CreditPulse.Web/Web.config`, pon `UsarSibMock` en `false` y ajusta
   `SibApiBaseUrl` a esa URL si cambió. Puedes probar el servicio
   directo desde el navegador:
   `https://localhost:44399/api/perfil?dpi=1234567890101&nit=123456-7&nombre=Prueba` —
   debería devolver el XML del perfil simulado.
7. **(Opcional) Probar las APIs de integración** (ver sección 1.6): con
   `CreditPulse.Web` corriendo (F5, sin necesitar `CreditPulse.SibSimulador`
   si `UsarSibMock=true`), prueba por ejemplo desde Postman o `curl`:
   ```
   GET https://localhost:<puerto>/api/v1/sib/perfil?dpi=1234567890101&nit=123456-7&nombre=Prueba
   Header: X-Api-Key: gemIysCWqXkClJ4BcCChAJgO8eZcD7tRltqsmL4NblxMQaIM
   ```
   (la llave de ejemplo es la que viene en `Web.config` →
   `ApiKeyIntegraciones`; cámbiala si la editas ahí). Sin el header
   `X-Api-Key`, la respuesta debería ser `401 Unauthorized`.

### Usuarios de prueba (uno por rol)

| Rol | Correo | Contraseña |
|---|---|---|
| Administrador | `admin@creditpulse.com` | `Admin#2026!` |
| Analista de Crédito | `analista@creditpulse.com` | `Analista#2026!` |
| Consulta | `consulta@creditpulse.com` | `Consulta#2026!` |

Inicia sesión con cada uno para ver la diferencia: el menú lateral y los
botones de "crear" cambian según el rol, y si fuerzas la URL de una
pantalla no permitida (por ejemplo, `Administracion/Productos.aspx` con
el usuario de Consulta), te manda a `AccesoDenegado.aspx`.

---

## 4. Decisiones de diseño que vale la pena explicar

- **Contraseña "encriptada" → hash, no cifrado reversible.** El
  levantamiento de requerimientos pide contraseñas encriptadas. Se
  implementó **hash PBKDF2-HMAC-SHA256 con salt por usuario**
  (`Security/PasswordHasher.cs`) en vez de una encriptación simétrica
  reversible. Es la práctica estándar de la industria para credenciales:
  un hash con salt no se puede "desencriptar" ni siquiera por quien
  diseñó el sistema, así que una filtración de la base de datos no
  expone las contraseñas reales. Se documenta aquí para que quede clara
  la decisión frente al enunciado original.
- **Motor de políticas configurable, no si/else fijos.** Cada política
  vive en la tabla `Politicas` con un `TipoRegla` (ej.
  `SCORE_BURO_MINIMO`) y un `ParametrosJson` (ej.
  `{"verde":650,"amarillo":580}`). `PolicyEngine.cs` interpreta ese tipo
  y esos parámetros. Ajustar un umbral o agregar una política a un
  producto nuevo no requiere tocar el código fuente.
- **Simulador de SIB determinístico por DPI.** Mismo DPI → mismo perfil
  simulado siempre, para que las pruebas de un mismo cliente sean
  repetibles (útil para probar los tres colores del semáforo a
  propósito). Cuando haya acceso a la API real, basta con implementar
  `ISibService` contra el webservice oficial de la SIB y registrarla en
  `EvaluationService.CrearSibService()` (ver sección 1.5.4) — el resto
  de la aplicación no cambia.
- **Permisos como banderas en vez de nombres de rol fijos en código.**
  Se detectó que el modelo de Fase 2 ("Roles y Permisos") no dejaba que
  un rol creado desde un panel de administración tuviera acceso
  funcional a nada, porque las pantallas comparaban el *nombre* del rol
  contra una lista fija en `RolesDeSistema`. Se rediseñó a permisos
  (`PuedeEvaluar`/`PuedeAdministrarCatalogo`/`PuedeAdministrarUsuarios`)
  en la tabla `Roles`, para que "crear roles" (sección 1.5.1) sea
  funcionalmente real. Ver el detalle completo en 1.5.1.
- **`QueueBackgroundWorkItem` en vez de un servicio de Windows o cola
  externa para el Batch.** Para procesar Lotes grandes en segundo plano
  sin bloquear la petición HTTP, se usó la API que ya trae ASP.NET
  (`System.Web.Hosting.HostingEnvironment.QueueBackgroundWorkItem`) en
  vez de introducir infraestructura adicional (un servicio de Windows,
  una cola de mensajes, un job scheduler) — suficiente para el volumen y
  el alcance de este curso, y documentado como la primera pieza a
  cambiar si el proyecto creciera a producción con volúmenes aún
  mayores o con varios servidores balanceando carga (ahí sí haría falta
  una cola real, porque el trabajo en `QueueBackgroundWorkItem` vive
  solo en el proceso de IIS que lo encoló).
- **Stored procedures para todo acceso a datos.** El levantamiento de
  requerimientos (RNF03) pide procedimientos almacenados eficientes en
  vez de SQL dinámico embebido; así se implementó en `DAL/`.
- **Cifrado de NIT/DPI a nivel de columna:** la Viabilidad Operativa
  menciona que esos identificadores deben quedar encriptados en las
  tablas maestras. Se documenta como pendiente de Fase 2 (junto con
  roles avanzados) para no bloquear la entrega del núcleo funcional;
  técnicamente se resolvería con Always Encrypted o cifrado a nivel de
  columna de SQL Server sin cambiar el resto de la arquitectura.

---

## 5. Catálogo de productos y políticas por defecto

Como no se contaba con reglas de negocio específicas, se diseñó un set de
políticas razonable por producto (ver `Database/04_SeedData.sql`),
editable luego desde `Administracion/Politicas.aspx` (o directamente en
la base de datos):

| Producto | Políticas |
|---|---|
| Tarjeta de Crédito Clásica | Edad mínima 18, listas de riesgo SIB, score de buró (≥620 verde / ≥560 amarillo), endeudamiento (≤45% / ≤65%), ingreso mínimo Q3,000, moras últimos 12 meses |
| Préstamo Hipotecario | Edad mínima 21, edad máxima 70, listas de riesgo SIB, score de buró (≥680/≥620), endeudamiento (≤35%/≤50%), ingreso mínimo Q8,000, antigüedad laboral ≥24 meses, moras |
| Crédito Personal | Edad mínima 18, listas de riesgo SIB, score de buró (≥600/≥540), endeudamiento (≤50%/≤70%), ingreso mínimo Q2,500, moras |
| Crédito Vehicular | Edad mínima 21, listas de riesgo SIB, score de buró (≥640/≥580), endeudamiento (≤40%/≤60%), ingreso mínimo Q4,500, antigüedad laboral ≥12 meses |

---

## 6. Solución de problemas al abrir en Visual Studio

- **"No se encuentra Newtonsoft.Json"**: clic derecho en la solución →
  "Restaurar paquetes NuGet". Si persiste, instálalo manualmente desde
  el Administrador de paquetes NuGet (`Newtonsoft.Json`, versión 13.0.3).
- **Error de conexión a SQL Server**: revisa el `connectionString` en
  `Web.config` contra el nombre real de tu instancia (`.\SQLEXPRESS`,
  `localhost`, etc.) y que el motor tenga habilitada la autenticación de
  Windows (o cambia a autenticación SQL si prefieres).
- **La página de Login no aparece / redirige en bucle**: confirma que
  ejecutaste `04_SeedData.sql` (crea el usuario administrador de
  prueba).
- **Los archivos `.designer.cs` muestran advertencias al abrir el
  diseñador por primera vez**: es normal; Visual Studio los
  regenera/sincroniza automáticamente al guardar cada página desde el
  diseñador. Ya se incluyeron manualmente para que el proyecto compile
  sin necesidad de abrir cada página en el diseñador primero.
- **"No se encuentra Microsoft.AspNet.WebApi.Core" (o `.WebHost`) al
  compilar `CreditPulse.SibSimulador`**: igual que con Newtonsoft.Json,
  clic derecho en la solución → "Restaurar paquetes NuGet". Solo hace
  falta si vas a probar la SIB como servicio HTTP separado (paso 6 de
  la sección 3); si te quedas con `UsarSibMock=true`, ni siquiera
  necesitas abrir/compilar este proyecto.
- **`CreditPulse.SibSimulador` no arranca en el puerto esperado**: el
  puerto (`44399`) es solo el sugerido por el proyecto; Visual Studio
  puede asignar otro si está ocupado. Revisa la URL real que muestra
  al iniciar y actualiza `SibApiBaseUrl` en
  `CreditPulse.Web/Web.config` con esa URL.

---

## 7. Estado del proyecto

Con Batch, Administración de Productos/Políticas, Roles y Permisos, el
rediseño visual (tema oscuro, tipografía más grande, sidebar responsivo),
y ahora también el panel de Usuarios/Roles, el reporte de la SIB legible,
el Batch optimizado para archivos grandes (procesamiento en segundo
plano + TVP) y la SIB como servicio HTTP independiente, se completó todo
lo acordado en las cuatro fases de trabajo. Lo que queda (auditoría
detallada, cifrado de columna para NIT/DPI y la conexión a una API
oficial de la SIB, que no existe públicamente hoy) está listado en la
sección 1.5.4/1 como pendiente para una futura iteración, fuera del
alcance actual del curso.
