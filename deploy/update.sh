#!/usr/bin/env bash
#
# PolySport – auf das neueste Release aktualisieren.
#
# Aufruf:
#   bash /opt/polysport/deploy/update.sh
#
# Holt die Tags von GitHub, wechselt auf das neueste Release, baut neu
# und startet die Container. Die Datenbank wird vorher gesichert; die
# Anwendung wendet fehlende Migrationen beim Start selbst an.
#
# Umgebungsvariablen:
#   POLYSPORT_ASSUME_YES=1   keine Rückfragen (für den Aufruf aus der Weboberfläche)
#   POLYSPORT_TARGET=v1.2.3  bestimmte Version statt der neuesten

set -euo pipefail

INSTALL_DIR="${POLYSPORT_DIR:-/opt/polysport}"

BOLD=$'\033[1m'; GREEN=$'\033[32m'; YELLOW=$'\033[33m'; RED=$'\033[31m'; RESET=$'\033[0m'
step()  { printf '\n%s==> %s%s\n' "$BOLD" "$*" "$RESET"; }
ok()    { printf '%s  ok%s  %s\n' "$GREEN" "$RESET" "$*"; }
warn()  { printf '%s  !%s   %s\n' "$YELLOW" "$RESET" "$*"; }
die()   { printf '\n%sFehler:%s %s\n' "$RED" "$RESET" "$*" >&2; exit 1; }

[[ "$(id -u)" -eq 0 ]] || die "Bitte als root ausführen (oder mit sudo)."
[[ -d "$INSTALL_DIR/.git" ]] || die "Keine Installation in $INSTALL_DIR gefunden."

cd "$INSTALL_DIR"
[[ -f .env ]] || die "Die Datei .env fehlt – bitte zuerst deploy/install.sh ausführen."

step "Stand prüfen"

CURRENT="$(git describe --tags --exact-match 2>/dev/null || git rev-parse --short HEAD)"
printf '  aktuell installiert: %s\n' "$CURRENT"

git fetch --all --tags --prune --quiet

if [[ -n "${POLYSPORT_TARGET:-}" ]]; then
    TARGET="$POLYSPORT_TARGET"
    printf '  angefordert:         %s\n' "$TARGET"
    git rev-parse --verify --quiet "$TARGET" >/dev/null \
        || die "Version $TARGET ist im Repository nicht vorhanden."
else
    LATEST_TAG="$(git tag -l --sort=-v:refname | head -n1 || true)"
    if [[ -z "$LATEST_TAG" ]]; then
        warn "Keine Release-Tags gefunden – es wird auf den Hauptzweig aktualisiert."
        TARGET="origin/$(git remote show origin | sed -n '/HEAD branch/s/.*: //p')"
    else
        printf '  neuestes Release:    %s\n' "$LATEST_TAG"
        TARGET="$LATEST_TAG"
        if [[ "$CURRENT" == "$LATEST_TAG" ]]; then
            ok "Bereits aktuell. Nichts zu tun."
            exit 0
        fi
    fi
fi

step "Datenbank sichern"

if bash "$INSTALL_DIR/deploy/backup.sh"; then
    ok "Sicherung erstellt"
else
    warn "Sicherung fehlgeschlagen (läuft die Datenbank?)."
    if [[ "${POLYSPORT_ASSUME_YES:-0}" == "1" ]]; then
        warn "Es wird ohne Sicherung weitergemacht (unbeaufsichtigter Lauf)."
    else
        printf '  Weiter ohne Sicherung? [j/N]: '
        read -r answer < /dev/tty || true
        [[ "$answer" =~ ^([jJ]|[yY])$ ]] || die "Abgebrochen."
    fi
fi

step "Auf $TARGET wechseln"

git -c advice.detachedHead=false checkout --quiet "$TARGET"
ok "Quellcode aktualisiert"

# Die laufende Version bekommt die Anwendung als Umgebungsvariable, damit
# sie in der Weboberfläche gegen GitHub verglichen werden kann.
NEW_VERSION="$(git describe --tags --exact-match 2>/dev/null || git rev-parse --short HEAD)"
if grep -q '^POLYSPORT_VERSION=' .env; then
    sed -i "s/^POLYSPORT_VERSION=.*/POLYSPORT_VERSION=${NEW_VERSION}/" .env
else
    printf 'POLYSPORT_VERSION=%s\n' "$NEW_VERSION" >> .env
fi
ok "Version $NEW_VERSION eingetragen"

step "Neu bauen und starten"

docker compose up -d --build

step "Aufräumen"
docker image prune -f >/dev/null 2>&1 || true
ok "Alte Images entfernt"

step "Status"
docker compose ps

APP_PORT="$(grep -E '^APP_PORT=' .env | cut -d= -f2-)"
APP_PORT="${APP_PORT:-8080}"

# Auf welcher Adresse ist der Port überhaupt veröffentlicht? Ist er an eine
# bestimmte Adresse gebunden, antwortet 127.0.0.1 nicht – dann würde die
# Prüfung fehlschlagen, obwohl die Anwendung längst läuft.
BIND_ADDRESS="$(grep -E '^BIND_ADDRESS=' .env | cut -d= -f2-)"
case "${BIND_ADDRESS:-0.0.0.0}" in
    0.0.0.0|::|"") CHECK_HOST="127.0.0.1" ;;
    *)             CHECK_HOST="$BIND_ADDRESS" ;;
esac

for _ in $(seq 1 60); do
    if curl -fsS -o /dev/null "http://${CHECK_HOST}:${APP_PORT}/" 2>/dev/null; then
        printf '\n%sAktualisierung abgeschlossen – die Webseite antwortet.%s\n' "$GREEN" "$RESET"
        exit 0
    fi
    sleep 5
done

# Maschinenlesbarer Grund, damit die Weboberfläche eine brauchbare
# Meldung zeigt statt der letzten Zeilen einer Tabelle.
printf 'POLYSPORT_RESULT=Neue Version läuft, aber http://%s:%s antwortete innerhalb von 5 Minuten nicht\n' \
    "$CHECK_HOST" "$APP_PORT"
warn "Die Webseite antwortet noch nicht. Protokoll: cd $INSTALL_DIR && docker compose logs -f"
exit 1
