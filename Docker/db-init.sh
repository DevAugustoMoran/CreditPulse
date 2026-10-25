#!/bin/bash
# Inicializa la base de datos CreditPulseDB dentro de SQL Server (contenedor "db").
#   - Si la base ya existe, NO vuelve a ejecutar los scripts (02, 03 y 06 no son re-ejecutables).
#   - FORCE_INIT=1 vuelve a ejecutarlos todos (útil solo con una base vacía/recién borrada).
#   - Siempre deja creado el usuario de la aplicación (cp_app).
set -euo pipefail

HOST="${DB_HOST:-db}"
SQLCMD_BIN="${SQLCMD_BIN:-/opt/mssql-tools18/bin/sqlcmd}"
SCRIPTS_DIR="${SCRIPTS_DIR:-/scripts}"
INIT_DIR="${INIT_DIR:-/init}"

# -C confía en el certificado del servidor, -b aborta ante errores, -I activa QUOTED_IDENTIFIER
# (requerido por columnas XML e índices), -f 65001 lee los .sql como UTF-8 (tildes y "ñ").
run_sql() {
    "$SQLCMD_BIN" -C -S "$HOST" -U sa -P "$MSSQL_SA_PASSWORD" -b -I -f 65001 "$@"
}

echo "[db-init] Esperando a que SQL Server acepte conexiones en '$HOST'..."
for intento in $(seq 1 60); do
    if run_sql -Q "SELECT 1" > /dev/null 2>&1; then break; fi
    sleep 2
done

existe=$(run_sql -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = N'CreditPulseDB'" | tr -d '[:space:]')

if [ "$existe" != "0" ] && [ "${FORCE_INIT:-0}" != "1" ]; then
    echo "[db-init] La base CreditPulseDB ya existe: se omiten los scripts de creación."
else
    shopt -s nullglob
    archivos=("$SCRIPTS_DIR"/[0-9][0-9]_*.sql)
    if [ "${#archivos[@]}" -eq 0 ]; then
        echo "[db-init] ERROR: no hay scripts .sql en $SCRIPTS_DIR" >&2
        exit 1
    fi
    for archivo in "${archivos[@]}"; do
        echo "[db-init] Ejecutando $(basename "$archivo")"
        run_sql -i "$archivo"
    done
fi

echo "[db-init] Asegurando el usuario de la aplicación (cp_app)"
run_sql -v APP_PASSWORD="$CP_APP_PASSWORD" -i "$INIT_DIR/99_usuario_app.sql"

echo "[db-init] Listo. Cadena de conexión local: Data Source=localhost,1433;Initial Catalog=CreditPulseDB;User ID=cp_app"
