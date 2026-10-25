# Cómo cambiar de versión la app publicada en Somee

Esta guía actualiza **http://creditpulse.somee.com** a la versión CP Analítica
(tercer nombre, reportes, DPI validado, hora corregida, modo claro/oscuro, tokens),
sin romper las APIs que ya consume el servicio externo.

**Regla de oro: primero la base de datos, después el código, y nunca reemplaces el
`Web.config` de Somee** (tiene tu cadena de conexión real).

Tiempo estimado: 20–30 minutos. El sitio estará en mantenimiento 2–5 minutos.

---

## Antes de empezar (respaldos)

1. En el administrador de archivos de Somee (o por FTP), **descarga una copia de
   todo el sitio** (la carpeta `wwwroot`). Es tu versión de regreso.
2. Descarga el `Web.config` actual de Somee y guárdalo aparte
   (`Web.config.somee.bak`). Lo necesitas para la cadena de conexión y la llave de
   las APIs.
3. Respalda la base de datos desde el panel de Somee (opción de backup de la base
   de datos). Si no la ofrece tu plan, exporta al menos los datos de las tablas
   `Evaluaciones`, `Usuarios` y `Lotes`.
4. Anota la llave `ApiKeyIntegraciones` de ese `Web.config`: **no se debe cambiar**
   porque el servicio externo la usa.

---

## Paso 1 — Compilar la nueva versión

En Visual Studio, con la solución abierta:

1. Configuración **Release**.
2. Clic derecho en `CreditPulse.Web` → **Publicar** → destino *Carpeta*
   (por ejemplo `C:\publicar\creditpulse`) → **Publicar**.
3. Verifica que la carpeta contenga `bin\`, `Content\`, `Reportes\`, `Site.Master`,
   `Default.aspx`, etc.

---

## Paso 2 — Base de datos (scripts 12, 13 y 14)

Conéctate a la base de Somee (SSMS con los datos de conexión del panel de Somee, o
el administrador SQL web que ofrece Somee) y ejecuta **en este orden**, usando
las versiones de la carpeta **`Database\Azure\`** (no traen la línea `USE`):

1. `12_TercerNombre.sql`
2. `13_Reportes.sql`
3. `14_AuthTokens.sql`

Son idempotentes: si se corren dos veces no pasa nada. Ninguno borra datos.
Si faltara alguno anterior (01–11), la base de Somee ya los tiene porque la app
está funcionando; no los vuelvas a ejecutar.

Comprobación rápida (debe devolver filas, no error):

```sql
SELECT TOP 1 TercerNombre FROM Evaluaciones;
SELECT COUNT(*) FROM RefreshTokens;
EXEC sp_Lotes_Listar;
```

> Con los scripts ya ejecutados, la versión **vieja** del código sigue
> funcionando igual; por eso este paso va primero.

---

## Paso 3 — Poner el sitio en mantenimiento (opcional pero recomendado)

Sube `Deploy\Somee\app_offline.htm` a la raíz del sitio (`wwwroot`). Mientras
exista ese archivo, IIS muestra la página de mantenimiento y suelta los archivos
bloqueados. Al terminar, bórralo.

---

## Paso 4 — Subir los archivos nuevos

Sube el contenido de la carpeta publicada a `wwwroot`, **sobrescribiendo**, con
estas excepciones:

| No subas / no sobrescribas | Motivo |
|---|---|
| `Web.config` | Tiene tu cadena de conexión de Somee y la llave de las APIs |
| `app_offline.htm` (de la carpeta publicada, si apareciera) | Solo lo controlas tú |

Asegúrate de que se actualicen:

- `bin\` completo (incluye `CreditPulse.Web.dll`; si subes solo parte, el sitio
  falla con errores de compilación).
- `Content\` (CSS, `cp-theme.js`, `img\` con el logo y favicons,
  `Plantillas\PlantillaEvaluacionBatch.xlsx`).
- La carpeta nueva `Reportes\`.
- `Content\img\logo-icono.jpg` (es el logo que se imprime en los PDF; si falta, el PDF sale igual pero sin logo).
- Todos los `.aspx`, `Site.Master` y `Global.asax` que cambiaron.

No borres nada de lo que ya estaba. No hace falta subir `Database\`, `Docker\` ni
los proyectos `CreditPulse.SibApi` / `CreditPulse.SibSimulador`.

---

## Paso 5 — Agregar los appSettings nuevos al Web.config de Somee

1. Abre el `Web.config` **de Somee** (el que respaldaste).
2. Abre `Deploy\Somee\appSettings-nuevos.xml`.
3. Pega esas líneas dentro de `<appSettings> ... </appSettings>`.
4. **Cambia `JwtSecret`** por un secreto propio de al menos 32 caracteres (el
   archivo trae el comando para generarlo).
5. Verifica que **sigan igual**: `ConnectionStrings\CreditPulseDB` (la de Somee),
   `ApiKeyIntegraciones`, `UsarSibMock` (déjalo en `true` en Somee, ver nota
   abajo), `machineKey`.
6. Guarda y sube ese `Web.config` (al guardarse, el sitio se reinicia solo).

> Sin los appSettings nuevos la app arranca igual (la zona horaria y el DPI tienen
> valores por defecto), pero **los tokens no funcionarán** hasta que exista
> `JwtSecret`.

---

## Paso 6 — Quitar el mantenimiento y probar

Borra `app_offline.htm` de `wwwroot` y comprueba, en este orden:

1. **Carga el sitio** y entra con `admin@creditpulse.com`. Debe verse el logo nuevo
   y el botón de modo claro/oscuro.
2. **Hora**: abre una evaluación reciente; la fecha debe coincidir con la hora de
   Guatemala.
3. **Tres nombres**: Nueva Evaluación con *Primer*, *Segundo* y *Tercer Nombre*.
   Usa un DPI válido, por ejemplo `3478953870101`. Con un DPI mal formado
   (por ejemplo `1234567890123`) debe rechazarlo con un mensaje.
4. **Reportes**: menú *Reportes* → *Evaluaciones por mes* (elige el mes actual) y
   *Clientes por batch* (elige un lote).
5. **Batch**: descarga la plantilla nueva y sube un archivo corto.
6. **APIs (lo más importante)**, con la misma llave de siempre:

   ```
   GET http://creditpulse.somee.com/api/v1/sib/perfil?dpi=3478953870101&nit=1234567&nombre=Prueba
   X-Api-Key: <tu ApiKeyIntegraciones>
   ```

   Debe responder `200` con el mismo formato de antes. Sin la llave debe dar `401`.
7. **Descargas**: en un reporte, prueba *Descargar Excel* y *Descargar PDF*. Los
   archivos se generan en el servidor sin instalar nada extra.
8. **Desbloqueo**: en *Usuarios y Roles*, un usuario bloqueado muestra "Bloqueado" y
   el enlace "Desbloquear".
9. **Token (nuevo)**:

   ```
   POST http://creditpulse.somee.com/api/v1/auth/token
   Content-Type: application/json
   {"email":"analista@creditpulse.com","password":"<su contraseña>"}
   ```

   Debe devolver access token y refresh token.

Pídele al responsable del servicio externo que haga una llamada de prueba.

---

## Si algo sale mal (volver atrás)

1. Sube `app_offline.htm`.
2. Restaura la copia de `wwwroot` del paso de respaldos (incluido el
   `Web.config` original).
3. No hace falta deshacer los scripts 12–14: son aditivos y la versión vieja los
   ignora.
4. Quita `app_offline.htm`.

Errores típicos:

| Síntoma | Causa probable |
|---|---|
| Pantalla amarilla de error de compilación | `bin\` incompleto o subiste un `Web.config` sin los ensamblados; vuelve a subir `bin\` completo |
| Todos tienen que iniciar sesión de nuevo | Cambió la `machineKey`; no la modifiques |
| Reporte muestra "ejecuta el script 13" | Falta el script 13 en la base de Somee |
| `/api/v1/auth/token` responde 500 | Falta `JwtSecret` (o tiene menos de 32 caracteres) o falta el script 14 |
| Los íconos o el logo no se ven | No se subió `Content\img\` |
| Quedé bloqueado y soy el único administrador | Ejecuta en la base: `UPDATE dbo.Usuarios SET IntentosFallidos = 0 WHERE Email = 'tu@correo';` |

---

## Notas sobre el plan gratuito de Somee

- **No ocultes ni quites** el banner publicitario que Somee agrega al sitio.
- La base tiene un límite de tamaño (30 MB en tu plan). Los reportes y el batch
  no crean tablas grandes, pero si llevas miles de evaluaciones revisa el espacio
  desde el panel.
- **La API de la SIB en Docker** (`CreditPulse.SibApi`) es para tu equipo local. En
  Somee gratuito no se recomienda montar un segundo sitio solo para eso; deja
  `UsarSibMock=true` y la SIB se simula dentro de la misma app (es lo que
  funciona hoy).
- El primer acceso después de un rato sin uso puede tardar porque Somee descarga
  el sitio de memoria por inactividad.
