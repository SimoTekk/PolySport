# PolySport

Webanwendung zur Verwaltung von Matches, Toren und Statistiken einer Mannschaft.
ASP.NET Core 8 (MVC) mit Identity und SQL Server.

## Was die Anwendung kann

- **Matches** anlegen, Kader zusammenstellen, Spiel starten und beenden
- **Spieluhr** auf dem Server: Drittel starten und beenden, Tore werden
  automatisch mit Drittel und Spielzeit erfasst
- **Tore und Assists** inklusive Gegentore, mit Torfolge und Zwischenstand
- **Statistik**: Top-Scorer, Auswertung nach Drittel, Saisonblatt mit Toren
  und Assists pro Spieler und Match (druckbar im Querformat)
- **Dashboard** mit Bilanz, Torverhältnis, offenem Match und letztem Resultat
- **Saisons** anlegen, bearbeiten, aktiv setzen und löschen
- **Spieler** anlegen und aktiv/inaktiv schalten (nur aktive stehen für neue
  Matches zur Auswahl, die Historie bleibt erhalten)
- **Benutzerverwaltung**: neue Registrierungen müssen von einem Admin
  freigegeben werden, bevor eine Anmeldung möglich ist
- Hell/Dunkel-Umschaltung

---

## Installation auf einem LXC oder Server

Vier kurze Zeilen, danach läuft alles geführt ab:

```bash
apt update
apt install -y git
git clone https://github.com/SimoTekk/PolySport /opt/polysport
bash /opt/polysport/deploy/install.sh
```

Diese Variante ist die verlässlichste: kurze Zeilen ohne Anführungszeichen,
die beim Kopieren in ein Terminal nicht kaputtgehen. Minimale Debian-Vorlagen
bringen weder `git` noch `curl` mit – die zweite Zeile deckt das ab, alles
Weitere installiert das Skript selbst.

Wer es in einem Aufruf mag und `curl` schon hat:

```bash
bash -c "$(curl -fsSL https://raw.githubusercontent.com/SimoTekk/PolySport/master/deploy/install.sh)"
```

> Achtung beim Kopieren aus einer Anleitung: Zeilen mit `$(...)` dürfen nicht
> umbrechen. Bricht die Adresse mitten im Namen um, schlägt der Aufruf fehl.
> Im Zweifel die Variante mit den vier Zeilen nehmen.

Das Skript
1. prüft System, Architektur und Arbeitsspeicher,
2. installiert Docker samt Compose-Plugin, falls nicht vorhanden,
3. klont das Projekt nach `/opt/polysport` und wechselt auf das neueste Release,
4. fragt Port, Adresse, Admin-Zugang und Datenbank-Passwort ab
   (Passwörter können generiert werden),
5. baut die Anwendung und startet sie mit der Datenbank,
6. wartet, bis die Webseite antwortet, und zeigt die Adresse an.

Danach ist die Seite unter `http://<IP-des-Containers>:8080` erreichbar. Die
Anmeldung erfolgt mit dem im Setup angegebenen Admin-Konto.

### Voraussetzungen

| Punkt | Anforderung |
|---|---|
| Betriebssystem | Debian oder Ubuntu (apt) |
| Architektur | x86_64 – SQL Server läuft nicht auf ARM |
| Arbeitsspeicher | mindestens 2 GB, empfohlen 3 GB (SQL Server allein braucht rund 2 GB) |
| Speicherplatz | rund 6 GB für Images und Datenbank |

**Bei einem LXC-Container** (z.B. unter Proxmox) muss der Container die Optionen
`nesting=1` und `keyctl=1` haben, sonst startet Docker nicht. In der Proxmox-
Oberfläche unter *Optionen > Features*, oder auf dem Host:

```bash
pct set <VMID> --features nesting=1,keyctl=1
```

---

## Aktualisieren

### Über die Weboberfläche

Die Anwendung prüft im Hintergrund alle sechs Stunden, ob auf GitHub ein
neueres Versions-Tag vorliegt. Ist das der Fall, sieht ein angemeldeter Admin
einen Hinweis über jeder Seite und einen Menüpunkt **Update**. Dort genügt ein
Klick auf *Update installieren*: die Datenbank wird gesichert, der neue Stand
geholt, neu gebaut und gestartet. Die Seite zeigt den Fortschritt und lädt sich
nach dem Neustart selbst neu.

Wie das ohne Docker-Rechte im Container funktioniert: die Weboberfläche legt
lediglich eine Datei unter `state/update-request` ab. Ein systemd-Wächter auf
dem Host (`polysport-update.path`) sieht sie und führt `deploy/update.sh` aus.
Der Anwendungscontainer hat **keinen** Zugriff auf den Docker-Socket.

Schlägt der Bau fehl, läuft die bisherige Version unverändert weiter und die
Seite meldet den Fehler.

### Auf der Kommandozeile

```bash
bash /opt/polysport/deploy/update.sh
```

Das Skript holt die Tags, sichert vorher die Datenbank, wechselt auf das
neueste Release, baut neu und startet die Container. Fehlende
Datenbank-Migrationen wendet die Anwendung beim Start selbst an. Ist bereits
die neueste Version installiert, bricht das Skript ab, ohne etwas zu tun.

### Releases veröffentlichen

Das Update-Skript orientiert sich an Git-Tags. Ein neues Release erstellt man
mit einem Tag nach dem Muster `v1.2.3`:

```bash
git tag -a v1.1.0 -m "Saisonblatt und Spieluhr"
git push origin v1.1.0
```

Ohne Tags aktualisiert das Skript auf den Hauptzweig.

---

## Sicherung

```bash
bash /opt/polysport/deploy/backup.sh
```

Legt eine Sicherung unter `/opt/polysport/backups/` ab und behält die
zehn neuesten. Vor jedem Update läuft das automatisch mit.

Wiederherstellen einer Sicherung:

```bash
cd /opt/polysport
docker compose cp backups/PolySport-20260823-201500.bak db:/var/opt/mssql/backup/restore.bak
docker compose exec db /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" \
  -Q "RESTORE DATABASE [PolySport] FROM DISK = N'/var/opt/mssql/backup/restore.bak' WITH REPLACE"
```

---

## Reverse Proxy mit HTTPS

Für den Zugriff von aussen empfiehlt sich nginx mit einem Zertifikat von
Let's Encrypt. Damit die Anwendung nicht doppelt im Netz hängt, in der `.env`
die Bindung auf lokal umstellen und neu starten:

```bash
sed -i 's/^BIND_ADDRESS=.*/BIND_ADDRESS=127.0.0.1/' /opt/polysport/.env
cd /opt/polysport && docker compose up -d
```

nginx-Konfiguration (`/etc/nginx/sites-available/polysport`):

```nginx
server {
    listen 80;
    server_name polysport.example.ch;

    location / {
        proxy_pass         http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_set_header   Upgrade           $http_upgrade;
        proxy_set_header   Connection        keep-alive;
    }
}
```

Aktivieren und Zertifikat holen:

```bash
ln -s /etc/nginx/sites-available/polysport /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx
apt install certbot python3-certbot-nginx
certbot --nginx -d polysport.example.ch
```

Die Anwendung wertet `X-Forwarded-Proto` aus
(`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` ist in der Compose-Datei gesetzt),
Anmeldung und Weiterleitungen funktionieren hinter dem Proxy also korrekt.

---

## Betrieb

Alle Befehle im Ordner `/opt/polysport`:

| Zweck | Befehl |
|---|---|
| Status | `docker compose ps` |
| Protokoll | `docker compose logs -f app` |
| Neu starten | `docker compose restart app` |
| Stoppen | `docker compose down` |
| Starten | `docker compose up -d` |
| Einstellungen ändern | `.env` bearbeiten, dann `docker compose up -d` |

Die Datenbank liegt im Docker-Volume `polysport_db-data` und bleibt bei
`docker compose down` erhalten. Erst `docker compose down -v` löscht sie.

---

## Entwicklung auf dem eigenen Rechner

Voraussetzungen: .NET 8 SDK und SQL Server LocalDB (kommt mit Visual Studio).

```bash
git clone https://github.com/SimoTekk/PolySport.git
cd PolySport
dotnet run --project PolySport --launch-profile https
```

`appsettings.Development.json` zeigt auf LocalDB (`PolySport-Dev`), es sind
also keine Zugangsdaten nötig. Migrationen wendet die Anwendung beim Start
selbst an.

Für die Entwicklung wird ein Admin-Konto `admin@admin.com` / `Admin123!`
angelegt. **Ausserhalb der Entwicklung gibt es kein Standardpasswort**: ohne
`Seed__AdminPassword` wird kein Konto erstellt und die Anwendung schreibt einen
Fehler ins Protokoll. Die Docker-Installation setzt den Wert automatisch aus
der `.env`.

EF-Werkzeuge und Migrationen:

```bash
dotnet tool restore
dotnet ef migrations add MeineAenderung --project PolySport --output-dir Data/Migrations
```

---

## Konfiguration

Alle Werte lassen sich als Umgebungsvariable setzen; in Docker geschieht das
über die `.env` (Vorlage: `.env.example`).

| Variable | Bedeutung |
|---|---|
| `ConnectionStrings__DefaultConnection` | Verbindung zur Datenbank |
| `Seed__AdminEmail` | E-Mail des ersten Admin-Kontos |
| `Seed__AdminPassword` | Passwort des ersten Admin-Kontos |
| `APP_PORT` | Port der Webseite (nur Docker) |
| `BIND_ADDRESS` | `0.0.0.0` für Netzzugriff, `127.0.0.1` hinter einem Proxy |
| `MSSQL_SA_PASSWORD` | Passwort des Datenbank-Kontos (nur Docker) |
| `MSSQL_PID` | `Express` (kostenlos, 10 GB) oder `Developer` |

---

## Hinweise

- **Neue Benutzer müssen freigegeben werden.** Nach der Registrierung ist keine
  Anmeldung möglich, bis ein Admin das Konto unter *Benutzer* freigibt. Eine
  E-Mail-Bestätigung gibt es nicht, es ist kein Mailversand eingerichtet.
- **Passwort zurücksetzen** funktioniert deshalb ebenfalls nicht. Ein Admin
  kann ein Konto ablehnen und neu registrieren lassen.
- Die Suchfelder bei *Match anlegen* und *Tor erfassen* laden Select2 von einem
  CDN. Ohne Internetzugang bleiben die Formulare nutzbar, aber ohne Tippsuche.
