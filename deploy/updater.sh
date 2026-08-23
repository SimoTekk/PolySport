#!/usr/bin/env bash
#
# Wird vom systemd-Dienst polysport-update aufgerufen, sobald die Webseite
# eine Aktualisierung anfordert (Datei state/update-request).
#
# Die Webanwendung selbst hat keinen Zugriff auf Docker – sie legt nur die
# Anforderung ab, ausgeführt wird das Update hier auf dem Host.

set -uo pipefail

INSTALL_DIR="${POLYSPORT_DIR:-/opt/polysport}"
STATE_DIR="$INSTALL_DIR/state"
REQUEST="$STATE_DIR/update-request"
STATUS="$STATE_DIR/update-status"

now() { date -u +%Y-%m-%dT%H:%M:%SZ; }

write_status() {
    # write_status <state> <message> [version]
    local state="$1" message="$2" version="${3:-}"
    {
        printf 'state=%s\n' "$state"
        printf 'version=%s\n' "$version"
        printf 'message=%s\n' "$message"
        printf 'updated=%s\n' "$(now)"
    } > "$STATUS.tmp"
    mv "$STATUS.tmp" "$STATUS"
    # Die Anwendung liest die Datei als unprivilegierter Benutzer
    chmod 666 "$STATUS" 2>/dev/null || true
}

[[ -f "$REQUEST" ]] || exit 0

# Wird gesetzt, sobald ein Abschluss geschrieben wurde. Bricht das Skript
# vorher ab – etwa durch das Zeitlimit von systemd – hinterlässt der
# Trap eine Meldung, statt die Oberfläche auf "läuft" stehen zu lassen.
FINAL_WRITTEN=0
trap 'code=$?; if [[ "$FINAL_WRITTEN" -eq 0 ]]; then write_status failed "Vorgang abgebrochen (Code $code) – siehe journalctl -u polysport-update" "${VERSION:-}"; fi' EXIT

VERSION="$(head -n1 "$REQUEST" | tr -d '\r\n[:space:]')"
# Anforderung sofort entfernen, damit sie nicht zweimal läuft
rm -f "$REQUEST"

if [[ -z "$VERSION" ]]; then
    write_status failed "Anforderung enthielt keine Version"
    FINAL_WRITTEN=1
    exit 1
fi

echo "Update auf ${VERSION} angefordert."
write_status running "Sicherung und Aktualisierung laufen" "$VERSION"

# update.sh unbeaufsichtigt ausführen und mitprotokollieren
LOG="$STATE_DIR/update-last.log"
if POLYSPORT_ASSUME_YES=1 POLYSPORT_TARGET="$VERSION" \
        bash "$INSTALL_DIR/deploy/update.sh" >"$LOG" 2>&1; then
    # Meldung des Skripts übernehmen, falls es eine gibt – sie kann einen
    # Hinweis enthalten, etwa dass der Port nicht erreichbar war.
    RESULT="$(grep -m1 '^POLYSPORT_RESULT=' "$LOG" 2>/dev/null | cut -d= -f2- | cut -c1-300)"
    write_status success "${RESULT:-Update auf ${VERSION} abgeschlossen}" "$VERSION"
    FINAL_WRITTEN=1
    echo "Update erfolgreich."
    exit 0
fi

# Fehlerfall: bevorzugt den vom Skript gemeldeten Grund verwenden, sonst
# die letzten Protokollzeilen. Ohne das landete schon mal die Ausgabe von
# "docker compose ps" als Fehlermeldung in der Oberfläche.
DETAIL="$(grep -m1 '^POLYSPORT_RESULT=' "$LOG" 2>/dev/null | cut -d= -f2- | cut -c1-300)"
if [[ -z "$DETAIL" ]]; then
    DETAIL="$(grep -aiE 'error|fehler|failed|cannot|denied|killed' "$LOG" 2>/dev/null \
        | tail -n 2 | tr '\n' ' ' | tr -d '\r' | cut -c1-300)"
fi
if [[ -z "$DETAIL" ]]; then
    DETAIL="$(tail -n 3 "$LOG" 2>/dev/null | tr '\n' ' ' | tr -d '\r' | cut -c1-300)"
fi
write_status failed "Fehlgeschlagen: ${DETAIL:-siehe journalctl -u polysport-update}" "$VERSION"
FINAL_WRITTEN=1
echo "Update fehlgeschlagen, siehe $LOG"
exit 1
