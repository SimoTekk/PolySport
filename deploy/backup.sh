#!/usr/bin/env bash
#
# PolySport – Sicherung der Datenbank in den Ordner ./backups.
#
# Aufruf:
#   bash /opt/polysport/deploy/backup.sh
#
# Die Sicherung wird im Datenbank-Container erstellt und danach auf den
# Host kopiert, damit keine Rechteprobleme mit gemounteten Ordnern entstehen.

set -euo pipefail

INSTALL_DIR="${POLYSPORT_DIR:-/opt/polysport}"
KEEP="${POLYSPORT_KEEP_BACKUPS:-10}"

cd "$INSTALL_DIR"
[[ -f .env ]] || { echo "Die Datei .env fehlt." >&2; exit 1; }

# shellcheck disable=SC1091
set -a; . ./.env; set +a

DB_NAME="${DB_NAME:-PolySport}"
STAMP="$(date +%Y%m%d-%H%M%S)"
FILE="${DB_NAME}-${STAMP}.bak"

mkdir -p backups

# Der Pfad der sqlcmd-Tools hängt von der Image-Version ab, darum
# wird er im Container selbst bestimmt.
docker compose exec -T db bash -lc "
    mkdir -p /var/opt/mssql/backup
    SQLCMD=\$( [ -x /opt/mssql-tools18/bin/sqlcmd ] && echo '/opt/mssql-tools18/bin/sqlcmd -C' || echo '/opt/mssql-tools/bin/sqlcmd' )
    \$SQLCMD -S localhost -U sa -P \"\$MSSQL_SA_PASSWORD\" -Q \"BACKUP DATABASE [${DB_NAME}] TO DISK = N'/var/opt/mssql/backup/${FILE}' WITH INIT, COMPRESSION, STATS = 25\"
"

docker compose cp "db:/var/opt/mssql/backup/${FILE}" "backups/${FILE}"
docker compose exec -T db rm -f "/var/opt/mssql/backup/${FILE}"

echo "Sicherung: ${INSTALL_DIR}/backups/${FILE}"

# Alte Sicherungen aufräumen, die neuesten $KEEP behalten
COUNT="$(ls -1 backups/*.bak 2>/dev/null | wc -l)"
if (( COUNT > KEEP )); then
    ls -1t backups/*.bak | tail -n +$((KEEP + 1)) | xargs -r rm -f
    echo "Alte Sicherungen entfernt, ${KEEP} behalten."
fi
