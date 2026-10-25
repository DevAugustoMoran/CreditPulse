# memory.md — CP Analítica (CreditPulse Analítica)

Documento de referencia: qué hace la aplicación y cómo funciona por dentro.
Proyecto del curso *Análisis de Sistemas II* (UMG), autor: Augusto Ardón.
Última actualización: octubre 2026.

---

## 1. Qué es

**CP Analítica** (nombre técnico interno: *CreditPulse*) es un motor de análisis
de riesgo crediticio para una institución financiera. Dado un solicitante y un
producto de crédito, hace tres cosas:

1. Consulta el perfil crediticio del solicitante en la **SIB** (Superintendencia
   de Bancos de Guatemala). La SIB real no tiene API pública, así que su
   información se **simula** de forma determinística (el mismo DPI siempre da el
   mismo perfil).
2. Aplica las **políticas** del producto (edad mínima, score de buró,
   endeudamiento, ingreso mínimo, moras, listas de riesgo, RCI bancario, etc.).
3. Devuelve un **semáforo**: **Verde** (aprobado), **Amarillo** (revisión manual)
   o **Rojo** (rechazado), con el detalle de cada política y una explicación en
   español.

Se usa de tres maneras: por **pantalla** (analistas), por **archivo Excel**
(evaluaciones Batch) y por **APIs REST** (otros sistemas del banco).

---

## 2. Tecnología

| Pieza | Detalle |
|---|---|
| Aplicación | ASP.NET WebForms, C#, .NET Framework 4.8 (`CreditPulse.Web`) |
| Acceso a datos | ADO.NET con **stored procedures** (capa `DAL`) |
| Base de datos | SQL Server Express 2022 (local con **Docker**; en producción, SQL de Somee) |
| APIs REST | ASP.NET Web API 2 dentro de `CreditPulse.Web` (carpeta `Api/`) |
| SIB simulada (opcional, como API) | `CreditPulse.SibSimulador` (Web API 2, Windows/IIS) y `CreditPulse.SibApi` (.NET 8, corre en Docker/Linux). Los dos usan el mismo generador. |
| Hosting publicado | Somee.com (plan gratuito): http://creditpulse.somee.com |
| Excel (Batch) | ExcelDataReader (solo `.xlsx`) |
| JSON | Newtonsoft.Json |

Arquitectura por capas: `Página .aspx.cs` → `BLL` (reglas) → `DAL` (stored
procedures) → SQL Server. Las políticas viven en la base de datos (tabla
`Politicas`, regla + parámetros JSON): ajustar una política es un `UPDATE`, no
un cambio de código.

---

## 3. Mapa de carpetas

```
CreditPulse/
├── CreditPulse.Web/            Aplicación principal
│   ├── Default.aspx            Dashboard
│   ├── Login.aspx, Error.aspx, AccesoDenegado.aspx
│   ├── Evaluacion/             Nueva, Resultado, Batch, LoteResultado
│   ├── Reportes/               ReporteEvaluaciones, ReporteBatch      (nuevo)
│   ├── Administracion/         Productos, Políticas, Usuarios, Roles
│   ├── Api/                    APIs /api/v1  (CONGELADAS, ver §9)
│   ├── BLL/                    PolicyEngine, EvaluationService, BatchEvaluationService,
│   │                           ReporteService, TokenService, DpiValidator, ZonaHoraria,
│   │                           SibMockService / SibApiClientService / SibXmlParser, Admin*Service
│   ├── DAL/                    Repositorios ADO.NET
│   ├── Models/                 Clases de datos
│   ├── Security/               PasswordHasher, SessionHelper, JwtHelper
│   └── Content/                site.css, cp-theme.js, img/ (logo), Plantillas/ (Excel de Batch)
├── CreditPulse.SibSimulador/   SIB como API (Web API 2, Windows)
├── CreditPulse.SibApi/         SIB como API (.NET 8, Docker/Linux)
├── Database/                   Scripts 01–14 (+ Azure/ para Somee, MySQL/, Utilidades/)
├── Docker/                     db-init.sh y script del usuario de la app
├── docker-compose.yml, .env.example
├── Deploy/Somee/               Guía de actualización y appSettings nuevos
├── Documentacion/              Documentos del curso (Word) y capturas
└── memory.md                   Este archivo
```

---

## 4. Roles y permisos

Tres permisos independientes por rol: **Evaluar**, **Administrar catálogo**,
**Administrar usuarios**. Roles de fábrica:

| Rol | Correo de prueba | Puede |
|---|---|---|
| Administrador | admin@creditpulse.com | Todo |
| Analista de Crédito | analista@creditpulse.com | Evaluar (individual y batch), consultar y ver reportes |
| Consulta | consulta@creditpulse.com | Solo consultar y ver reportes |

(Contraseñas de prueba: ver README.) Si alguien abre por URL una pantalla que su
rol no permite, va a `AccesoDenegado.aspx`. Un usuario no puede desactivarse a sí
mismo y un rol no puede quedarse sin permiso de administración mientras tenga
usuarios activos.

**Reportes:** son de solo lectura y los puede ver cualquier usuario autenticado.

### 4.1 Usuarios bloqueados y cómo desbloquearlos

Un usuario se **bloquea** al fallar la contraseña 5 veces seguidas
(`IntentosFallidos >= 5`); el login responde "La cuenta fue bloqueada...". El contador
se pone en 0 con un inicio de sesión correcto. Para desbloquear, un Administrador
(permiso *Administrar usuarios*) entra a **Administración → Usuarios y Roles**: la fila
del usuario muestra el estado **Bloqueado** y el enlace **Desbloquear**, que limpia el
contador sin cambiar la contraseña. También desbloquea: *Editar* el usuario y escribirle
una contraseña nueva, o *Desactivar* y luego *Activar*.

Si el bloqueado es el **único administrador** (nadie más puede entrar a desbloquearlo),
se hace directo en la base de datos:

```sql
UPDATE dbo.Usuarios SET IntentosFallidos = 0 WHERE Email = 'correo@del.usuario';
```

No hace falta ningún script nuevo: la acción reutiliza `sp_Usuarios_CambiarEstado`.

---

## 5. Pantallas

- **Login**: correo y contraseña; botón de tema. Tras **5 intentos fallidos** la cuenta
  queda bloqueada (ver §4.1).
- **Dashboard**: contadores (total, verde, amarillo, rojo), buscador por número de
  evaluación, lotes y evaluaciones recientes.
- **Nueva evaluación**: producto → campos dinámicos según el producto → datos
  personales (**hasta tres nombres** y dos apellidos, NIT, DPI, fecha de
  nacimiento) → resultado.
- **Resultado**: pestaña *Políticas evaluadas* (semáforo y detalle) y pestaña
  *Información SIB* (score con medidor, endeudamiento, ingresos, historial,
  listas de riesgo).
- **Evaluaciones Batch**: sube un Excel (plantilla incluida, con columna opcional
  *Tercer Nombre*); se procesa en segundo plano en bloques de 200 filas con barra
  de progreso. Cada carga recibe un **número de lote (batch)**.
- **Reportes** (nuevo):
  - *Evaluaciones por mes*: filtro de mes, año y color; muestra color, cliente,
    score crediticio y fecha; totales por color; paginado; exportación a CSV e
    impresión.
  - *Clientes por batch*: se elige o escribe el número de lote; muestra datos del
    cliente, información recolectada de la SIB, número de batch y fecha de
    evaluación.
  - **Descargas** (los dos reportes): botones *Descargar Excel* (.xlsx),
    *Descargar PDF* y *Descargar CSV*, más *Imprimir*. Las descargas incluyen **todas**
    las filas del filtro (no solo la página que se ve). El Excel y el CSV del batch
    llevan las 26 columnas (todo lo recolectado de la SIB); el PDF (A4 horizontal,
    con logo, totales, encabezado repetido y "Página X de Y") lleva las 15 principales.
    Se generan en el servidor sin librerías externas (`BLL/ExportadorXlsx.cs`,
    `BLL/ExportadorPdf.cs`, `BLL/TablaReporte.cs`; entrega en `BLL/ExportacionReporte.cs`).
- **Administración**: productos y políticas; usuarios y roles.
- **Modo claro / oscuro**: botón sol/luna en la barra superior (y en el login);
  se recuerda en el navegador (`localStorage`, clave `cp-theme`). El menú lateral
  se mantiene oscuro en ambos modos.

Interfaz **responsiva** (probada de 320 px a 2560 px, claro y oscuro): menú tipo
cajón en pantallas angostas, tablas con desplazamiento horizontal propio, tarjetas
que se reacomodan.

---

## 6. Cómo funciona una evaluación

1. Se validan los datos. En pantalla y en Batch el **DPI** pasa por `DpiValidator`
   (ver §7).
2. `EvaluationService` consulta la SIB mediante `ISibService`:
   - `SibMockService`: genera el perfil en el mismo proceso (valor por defecto,
     `UsarSibMock=true`).
   - `SibApiClientService`: llama por HTTP a `SibApiBaseUrl`
     (`GET api/perfil?dpi&nit&nombre`, responde XML) cuando `UsarSibMock=false`.
     Si hay appSetting `SibApiKey`, envía `X-Api-Key`.
   - `SibXmlParser` convierte el XML en un `SibPerfil`.
3. `PolicyEngine` evalúa una a una las políticas activas del producto. El color
   general es el **peor** de los resultados individuales.
4. Se guarda la evaluación (encabezado + detalle + XML crudo de la SIB) con los
   stored procedures.

**¿La SIB viene de una API?** Por defecto **no**: se genera dentro de la misma
aplicación. Pero la aplicación ya puede consultarla por HTTP: apunta
`SibApiBaseUrl` a `CreditPulse.SibSimulador` (Windows) o a `CreditPulse.SibApi`
(Docker, puerto 5080) y pon `UsarSibMock=false`.

---

## 7. Validación del DPI (CUI)

`BLL/DpiValidator.cs` (y su réplica en JavaScript en `Nueva.aspx`, para avisar antes
de enviar). Estructura de 13 dígitos: `NNNNNNNN V DD MM`.

- 8 dígitos de número de registro.
- 1 dígito verificador: Σ dígito[i] × (i+2), i = 0..7, módulo 11 debe ser igual al
  dígito 9. (Configurable con `DpiValidarDigitoVerificador`; por defecto activo.)
- 2 dígitos de departamento (01–22).
- 2 dígitos de municipio (de 01 hasta los municipios que tiene ese departamento).

Acepta espacios y guiones ("3478-95387-0101"). Es validación de **formato**, no
consulta al RENAP. **No** se aplica en las APIs `/api/v1` (para no cambiar su
comportamiento). En Batch, una fila con DPI inválido queda como error y no se
envía a la SIB.

DPIs de ejemplo válidos: `3478953870101`, `2589471600101`, `1023456780101`,
`2045678150101`, `3156789250101`, `4012345680101`, `5123456780101`, `6134567850101`.

---

## 8. Fechas y zona horaria

La base **guarda en UTC** (`SYSUTCDATETIME()`). Antes, el detalle usaba la zona del
servidor (en Somee no es Guatemala) y el dashboard mostraba el UTC sin convertir:
de ahí la hora incorrecta. Ahora todo lo que se **muestra** pasa por
`BLL/ZonaHoraria.cs`, que convierte a Guatemala (UTC-6, sin horario de verano),
sin importar dónde corra el servidor. Los filtros mensuales de los reportes
también cuentan el mes en hora de Guatemala. Las APIs siguen devolviendo UTC.

---

## 9. APIs REST (`/api/v1`) — CONGELADAS

Las consume un servicio externo, por eso **no se modifican**.

| Método y ruta | Qué hace |
|---|---|
| `POST /api/v1/evaluaciones` | Evaluación completa (SIB + políticas) |
| `GET /api/v1/evaluaciones/{id}` | Detalle de una evaluación |
| `GET /api/v1/evaluaciones/{id}/variables-score` | Variables ponderadas del score |
| `GET /api/v1/sib/perfil?dpi&nit&nombre` | Perfil de la SIB |

Autenticación existente: header `X-Api-Key` (filtro global
`ApiKeyAuthorizeAttribute`, llave en `ApiKeyIntegraciones`). Responden JSON o XML
según `Accept`.

**Único cambio dentro de `Api/`:** una línea **aditiva** en `WebApiConfig.cs`
(`config.MessageHandlers.Add(new TokenAuthHandler())`) más dos archivos nuevos
(`Handlers/TokenAuthHandler.cs`, `Models/AuthApiModels.cs`). Controladores, filtro y
modelos existentes quedaron intactos (se verificó con hashes SHA-256).

---

## 10. Token y refresh token (nuevo, adicional)

Antes **no existía**: la web usaba cookie de Forms Authentication y las APIs la
llave `X-Api-Key`. Ahora hay además:

| Endpoint | Cuerpo | Respuesta |
|---|---|---|
| `POST /api/v1/auth/token` | `{ "email", "password" }` | access token + refresh token |
| `POST /api/v1/auth/refresh` | `{ "refreshToken" }` | par nuevo (rotación) |
| `POST /api/v1/auth/revoke` | `{ "refreshToken" }` | cierra la sesión de API |

- **Access token**: JWT HS256, dura 15 minutos (`JwtAccessMinutes`). Se envía como
  `Authorization: Bearer <token>` en las APIs existentes **como alternativa** a
  `X-Api-Key`. Crear una evaluación exige el permiso *Evaluar* del rol del usuario.
- **Refresh token**: 64 bytes aleatorios, dura 7 días (`JwtRefreshDays`). En la base
  solo se guarda su hash SHA-256 (tabla `RefreshTokens`). Es de **un solo uso**:
  cada renovación entrega uno nuevo; si se reutiliza uno ya usado, se revocan todos
  los tokens de ese usuario.
- Si la petición trae `X-Api-Key`, el handler no hace nada: el servicio externo
  funciona exactamente igual que antes.
- La interfaz web **sigue con cookie** (sesión deslizante de 20 minutos).
- `JwtSecret` (mínimo 32 caracteres) debe ser distinto y secreto en cada ambiente.

---

## 11. Base de datos

SQL Server, scripts incrementales e idempotentes en `Database/`:

| Script | Contenido |
|---|---|
| 01–04 | Base, tablas, stored procedures, datos semilla |
| 05 | Producto "General" y lotes (Batch) |
| 06–08 | Administración, roles, usuarios y permisos |
| 09 | Batch asíncrono (progreso, tipos de tabla) |
| 10 | Política RCI Bancario |
| 11 | Rol y usuario de servicio para las APIs |
| **12** | `Evaluaciones.TercerNombre` (opcional) |
| **13** | Procedimientos de reportes (`sp_Reporte_*`, `sp_Lotes_Listar`) |
| **14** | `RefreshTokens` y procedimientos de tokens |

`Database/Azure/` son las mismas versiones sin la línea `USE` (para bases de
hosting como Somee). `Database/MySQL/` es una conversión para generar el diagrama
entidad-relación en MySQL Workbench.

**Compatibilidad hacia atrás:** el código solo envía `@TercerNombre` si hay valor y
detecta si la columna existe, así que una base sin el script 12 sigue funcionando.
Aun así, **ejecuta los scripts antes de subir el código nuevo**.

---

## 12. Docker (base local)

`docker-compose.yml` (proyecto `cp-analitica`):

- `db`: SQL Server 2022 Express, puerto 1433, volumen `cp_sqldata` (los datos
  sobreviven a reinicios).
- `db-init`: espera a que `db` esté sano y ejecuta `Database/[0-9][0-9]_*.sql` en
  orden con `sqlcmd`, y crea el usuario `cp_app`. Si la base ya existe no la
  recrea (salvo `FORCE_INIT=1`).
- `sib-api`: la API de la SIB (.NET 8) en el puerto 5080.

```
docker compose up -d                 # levanta todo
docker compose logs db-init          # ver la carga de scripts
docker compose down                  # apaga (conserva datos)
docker compose down -v               # apaga y BORRA los datos
```

La aplicación .NET Framework corre en Windows (IIS Express desde Visual Studio) y
se conecta a `localhost,1433` con el usuario `cp_app` (cadena ya puesta en
`Web.config`). Contraseñas y puertos: copiar `.env.example` a `.env`.

---

## 13. Configuración (`Web.config` → appSettings)

| Clave | Para qué |
|---|---|
| `SessionTimeoutMinutes` | Inactividad antes de cerrar sesión (20) |
| `UsarSibMock` / `SibApiBaseUrl` / `SibApiTimeoutMs` / `SibApiKey` | SIB en memoria o por HTTP |
| `ApiKeyIntegraciones` | Llave `X-Api-Key` de las APIs |
| `ApiUsuarioIntegracionEmail` | Usuario de servicio que firma evaluaciones por API |
| `JwtSecret`, `JwtIssuer`, `JwtAudience`, `JwtAccessMinutes`, `JwtRefreshDays` | Tokens |
| `ZonaHorariaId` | Zona para mostrar fechas (`Central America Standard Time`) |
| `DpiValidarDigitoVerificador` | Verificar o no el dígito verificador del DPI |

`machineKey` es fija (necesario en hostings que reciclan el proceso, para no
invalidar las cookies de sesión).

---

## 14. Marca

Logo **CP ANALÍTICA — Motor de riesgos** en `Content/img/` (logo completo, icono
cuadrado, favicons). Aparece en el menú lateral, en el login, en el error y como
favicon.

---

## 15. Decisiones importantes para recordar

- Las **APIs existentes no se tocan**; todo lo nuevo es aditivo.
- La base guarda UTC; la hora de Guatemala se aplica solo al mostrar.
- El DPI se valida en UI y Batch, no en API.
- La SIB es simulada; consultarla por API es opcional por configuración.
- Los reportes son de lectura. Excel guarda los textos como texto (no se ejecutan
  como fórmula) y el CSV neutraliza celdas que empiecen con `=`, `+`, `-`, `@`.
- La pantalla de login ya no muestra un usuario de prueba.
- En Somee gratuito hay un banner publicitario obligatorio que no debe ocultarse.

---

## 16. Pendiente o limitaciones conocidas

- No se probó la ejecución de los scripts 12–14 ni de las páginas `.aspx` contra
  SQL Server/IIS reales en el entorno donde se desarrolló (sí se compiló todo el
  código C#, se probaron DPI, JWT y las API de SIB, y se revisó el diseño
  responsivo con un navegador).
- Alojar la API de la SIB como segundo sitio en Somee gratuito puede no estar
  permitido; por eso en producción la SIB queda en modo interno (`UsarSibMock=true`).
- Sin auditoría detallada ni cifrado de columnas NIT/DPI.
