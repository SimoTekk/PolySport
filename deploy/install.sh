#!/usr/bin/env bash
#
# PolySport – geführte Installation auf einem Debian/Ubuntu-System (z.B. LXC).
#
# Aufruf:
#   bash -c "$(curl -fsSL https://raw.githubusercontent.com/SimoTekk/PolySport/master/deploy/install.sh)"
#
# Das Skript installiert Docker (falls nötig), holt den Quellcode,
# fragt die Einstellungen ab und startet Webanwendung samt Datenbank.

set -euo pipefail

REPO_URL="${POLYSPORT_REPO:-https://github.com/SimoTekk/PolySport.git}"
INSTALL_DIR="${POLYSPORT_DIR:-/opt/polysport}"

# Eingaben immer vom Terminal lesen, damit die Abfragen auch dann
# funktionieren, wenn das Skript über eine Pipe kommt.
if [[ -r /dev/tty ]]; then
    exec 3</dev/tty
else
    exec 3<&0
fi

BOLD=$'\033[1m'; GREEN=$'\033[32m'; YELLOW=$'\033[33m'; RED=$'\033[31m'; RESET=$'\033[0m'

info()  { printf '%s\n' "$*"; }
step()  { printf '\n%s==> %s%s\n' "$BOLD" "$*" "$RESET"; }
ok()    { printf '%s  ok%s  %s\n' "$GREEN" "$RESET" "$*"; }
warn()  { printf '%s  !%s   %s\n' "$YELLOW" "$RESET" "$*"; }
die()   { printf '\n%sFehler:%s %s\n' "$RED" "$RESET" "$*" >&2; exit 1; }

ask() {
    # ask <Frage> <Standardwert> -> Antwort auf stdout
    local prompt="$1" default="${2:-}" answer=""
    if [[ -n "$default" ]]; then
        printf '%s [%s]: ' "$prompt" "$default" >&2
    else
        printf '%s: ' "$prompt" >&2
    fi
    read -r answer <&3 || true
    printf '%s' "${answer:-$default}"
}

ask_secret() {
    # ask_secret <Frage> <Standardwert> -> Antwort auf stdout (Eingabe verdeckt)
    local prompt="$1" default="${2:-}" answer=""
    printf '%s [Enter = generiertes Passwort]: ' "$prompt" >&2
    read -rs answer <&3 || true
    printf '\n' >&2
    printf '%s' "${answer:-$default}"
}

confirm() {
    local answer
    answer="$(ask "$1 (j/n)" "${2:-n}")"
    [[ "$answer" =~ ^([jJ]|[yY])$ ]]
}

generate_password() {
    # SQL Server verlangt Komplexität: Buchstaben, Ziffern, Sonderzeichen.
    if command -v openssl >/dev/null 2>&1; then
        printf 'Ps%s_7!' "$(openssl rand -base64 18 | tr -dc 'A-Za-z0-9' | cut -c1-16)"
    else
        printf 'Ps%s_7!' "$(head -c 32 /dev/urandom | od -An -tx1 | tr -d ' \n' | cut -c1-16)"
    fi
}

printf '%s\n' "-----------------------------------------------"
printf '%s\n' " PolySport – Installation"
printf '%s\n' "-----------------------------------------------"

# ---------------------------------------------------------------
step "Systemprüfung"

[[ "$(id -u)" -eq 0 ]] || die "Bitte als root ausführen (oder mit sudo)."

command -v apt-get >/dev/null 2>&1 || die "Dieses Skript ist für Debian/Ubuntu gedacht (apt-get fehlt)."
ok "Debian/Ubuntu erkannt"

ARCH="$(uname -m)"
if [[ "$ARCH" != "x86_64" ]]; then
    die "Microsoft SQL Server läuft nur auf x86_64, gefunden: $ARCH."
fi
ok "Architektur $ARCH"

TOTAL_MB="$(awk '/MemTotal/ {printf "%d", $2/1024}' /proc/meminfo)"
if (( TOTAL_MB < 2600 )); then
    warn "Nur ${TOTAL_MB} MB RAM. SQL Server braucht rund 2 GB, empfohlen sind 3 GB."
    confirm "Trotzdem weitermachen?" "n" || die "Abgebrochen. Bitte dem Container mehr RAM geben."
else
    ok "${TOTAL_MB} MB RAM"
fi

if [[ -f /proc/1/environ ]] && grep -qa container=lxc /proc/1/environ 2>/dev/null; then
    warn "LXC erkannt. Der Container braucht die Optionen nesting=1 und keyctl=1,"
    warn "sonst startet Docker nicht. Bei Proxmox: Optionen > Features."
fi

# ---------------------------------------------------------------
step "Grundwerkzeuge"

# Minimale Debian-Vorlagen bringen weder curl noch git mit. Beides wird
# später gebraucht: curl für die Docker-Installation und die Prüfung am
# Ende, git zum Holen des Quellcodes.
MISSING=()
for tool in curl git ca-certificates; do
    case "$tool" in
        ca-certificates) dpkg -s ca-certificates >/dev/null 2>&1 || MISSING+=("$tool") ;;
        *) command -v "$tool" >/dev/null 2>&1 || MISSING+=("$tool") ;;
    esac
done

if (( ${#MISSING[@]} > 0 )); then
    info "Es fehlen: ${MISSING[*]} – werden installiert."
    apt-get update -qq
    apt-get install -y -qq "${MISSING[@]}" >/dev/null || die "Installation von ${MISSING[*]} fehlgeschlagen."
    ok "${MISSING[*]} installiert"
else
    ok "curl und git sind vorhanden"
fi

# ---------------------------------------------------------------
step "Docker"

if command -v docker >/dev/null 2>&1 && docker compose version >/dev/null 2>&1; then
    ok "Docker mit Compose-Plugin ist vorhanden"
else
    info "Docker wird installiert (offizielles Installationsskript)."
    curl -fsSL https://get.docker.com -o /tmp/get-docker.sh
    sh /tmp/get-docker.sh
    rm -f /tmp/get-docker.sh
    systemctl enable --now docker >/dev/null 2>&1 || true
    docker compose version >/dev/null 2>&1 || die "Docker Compose fehlt weiterhin."
    ok "Docker installiert"
fi

# ---------------------------------------------------------------
step "Quellcode"

if [[ -d "$INSTALL_DIR/.git" ]]; then
    if [[ -f "$INSTALL_DIR/.env" ]]; then
        # Schon einmal eingerichtet – hier ist Vorsicht angebracht.
        warn "In $INSTALL_DIR ist bereits eine Installation vorhanden."
        info "Für eine Aktualisierung: bash $INSTALL_DIR/deploy/update.sh"
        confirm "Vorhandene Installation weiterverwenden und neu einrichten?" "j" \
            || die "Abgebrochen."
    else
        # Der Quellcode wurde von Hand geklont, aber noch nichts eingerichtet.
        ok "Quellcode liegt bereits in $INSTALL_DIR"
    fi
    git -C "$INSTALL_DIR" fetch --all --tags --prune
else
    mkdir -p "$(dirname "$INSTALL_DIR")"
    git clone --quiet "$REPO_URL" "$INSTALL_DIR"
    ok "Nach $INSTALL_DIR geklont"
fi

cd "$INSTALL_DIR"

# Auf das neueste Release stellen, falls es Tags gibt
LATEST_TAG="$(git tag -l --sort=-v:refname | head -n1 || true)"
if [[ -n "$LATEST_TAG" ]]; then
    git -c advice.detachedHead=false checkout --quiet "$LATEST_TAG"
    ok "Release $LATEST_TAG ausgecheckt"
else
    warn "Keine Release-Tags gefunden, es wird der Hauptzweig verwendet."
fi

# ---------------------------------------------------------------
step "Einstellungen"

if [[ -f .env ]]; then
    warn "Es gibt bereits eine .env."
    if confirm "Bestehende Einstellungen behalten?" "j"; then
        KEEP_ENV=1
    else
        cp .env ".env.backup.$(date +%Y%m%d%H%M%S)"
        KEEP_ENV=0
    fi
else
    KEEP_ENV=0
fi

if [[ "$KEEP_ENV" -eq 0 ]]; then
    info "Leere Eingabe übernimmt jeweils den Wert in Klammern."
    info ""

    APP_PORT="$(ask 'Port für die Webseite' '8080')"

    info ""
    info "Soll die Webseite direkt im Netz erreichbar sein (0.0.0.0),"
    info "oder nur lokal (127.0.0.1), weil ein Reverse Proxy davorkommt?"
    BIND_ADDRESS="$(ask 'Adresse' '0.0.0.0')"

    info ""
    ADMIN_EMAIL="$(ask 'E-Mail des ersten Admin-Kontos' 'admin@admin.com')"

    GENERATED_ADMIN="$(generate_password)"
    ADMIN_PASSWORD="$(ask_secret 'Passwort des Admin-Kontos' "$GENERATED_ADMIN")"

    GENERATED_SA="$(generate_password)"
    info ""
    info "Passwort für das Datenbank-Konto. Das braucht man im Alltag nicht,"
    info "ein generiertes ist hier die bessere Wahl."
    MSSQL_SA_PASSWORD="$(ask_secret 'Datenbank-Passwort (sa)' "$GENERATED_SA")"

    if (( ${#ADMIN_PASSWORD} < 8 )) || (( ${#MSSQL_SA_PASSWORD} < 8 )); then
        die "Passwörter müssen mindestens 8 Zeichen haben (Vorgabe von SQL Server)."
    fi

    umask 077
    cat > .env <<EOF
# Von deploy/install.sh erzeugt am $(date -Iseconds)
MSSQL_SA_PASSWORD=${MSSQL_SA_PASSWORD}
ADMIN_EMAIL=${ADMIN_EMAIL}
ADMIN_PASSWORD=${ADMIN_PASSWORD}
APP_PORT=${APP_PORT}
BIND_ADDRESS=${BIND_ADDRESS}
DB_NAME=PolySport
MSSQL_PID=Express
POLYSPORT_VERSION=${LATEST_TAG:-master}
UPDATE_REPOSITORY=SimoTekk/PolySport
EOF
    chmod 600 .env
    ok ".env geschrieben (nur für root lesbar)"

    if [[ "$ADMIN_PASSWORD" == "$GENERATED_ADMIN" ]]; then
        printf '\n%sBitte notieren:%s Admin-Passwort lautet %s%s%s\n' \
            "$BOLD" "$RESET" "$BOLD" "$ADMIN_PASSWORD" "$RESET"
    fi
fi

# ---------------------------------------------------------------
step "Update über die Weboberfläche einrichten"

# Über diesen Ordner meldet die Weboberfläche einen Update-Wunsch an.
# Der Container läuft als unprivilegierter Benutzer, darum darf jeder
# schreiben – es liegen nur zwei Statusdateien darin.
mkdir -p "$INSTALL_DIR/state"
chmod 777 "$INSTALL_DIR/state"

install -m 644 "$INSTALL_DIR/deploy/systemd/polysport-update.service" /etc/systemd/system/
install -m 644 "$INSTALL_DIR/deploy/systemd/polysport-update.path" /etc/systemd/system/

# Pfade anpassen, falls nicht im Standardordner installiert
if [[ "$INSTALL_DIR" != "/opt/polysport" ]]; then
    sed -i "s#/opt/polysport#${INSTALL_DIR}#g" \
        /etc/systemd/system/polysport-update.service \
        /etc/systemd/system/polysport-update.path
fi

systemctl daemon-reload
systemctl enable --now polysport-update.path >/dev/null 2>&1
ok "Wächter aktiv – Updates lassen sich in der Weboberfläche auslösen"

# ---------------------------------------------------------------
step "Bauen und starten"

info "Das erste Bauen lädt die .NET- und SQL-Server-Images herunter."
info "Je nach Verbindung dauert das einige Minuten."
docker compose up -d --build

# ---------------------------------------------------------------
step "Warten bis die Webseite antwortet"

# Werte aus der .env lesen, auch wenn sie behalten wurde
APP_PORT="$(grep -E '^APP_PORT=' .env | cut -d= -f2-)"
APP_PORT="${APP_PORT:-8080}"

READY=0
for _ in $(seq 1 90); do
    if curl -fsS -o /dev/null "http://127.0.0.1:${APP_PORT}/" 2>/dev/null; then
        READY=1
        break
    fi
    sleep 5
done

if [[ "$READY" -ne 1 ]]; then
    warn "Die Webseite hat noch nicht geantwortet."
    info "Protokoll ansehen mit:  docker compose -f $INSTALL_DIR/docker-compose.yml logs -f"
    exit 1
fi

IP_ADDRESS="$(hostname -I 2>/dev/null | awk '{print $1}')"
ADMIN_EMAIL="$(grep -E '^ADMIN_EMAIL=' .env | cut -d= -f2-)"

printf '\n%s%s%s\n' "$GREEN" "Fertig – PolySport läuft." "$RESET"
printf '\n'
printf '  Webseite:   http://%s:%s\n' "${IP_ADDRESS:-<IP-des-Containers>}" "$APP_PORT"
printf '  Anmeldung:  %s\n' "$ADMIN_EMAIL"
printf '  Passwort:   wie beim Setup angegeben\n'
printf '\n'
printf '  Aktualisieren:   in der Weboberfläche unter "Update" (nur Admin)\n'
printf '                   oder bash %s/deploy/update.sh\n' "$INSTALL_DIR"
printf '  Sicherung:       bash %s/deploy/backup.sh\n' "$INSTALL_DIR"
printf '  Protokoll:       cd %s && docker compose logs -f\n' "$INSTALL_DIR"
printf '  Stoppen:         cd %s && docker compose down\n' "$INSTALL_DIR"
printf '\n'
printf '  Für Zugriff von aussen mit HTTPS einen Reverse Proxy davorsetzen,\n'
printf '  Beispiel für nginx steht in der README unter "Reverse Proxy".\n'
printf '\n'
