# Änderungen

Diese Datei wird von der Anwendung gelesen: auf der Update-Seite erscheinen
alle Einträge zwischen der installierten und der neuesten Version.

Format bitte beibehalten – eine Überschrift `## vX.Y.Z – TT.MM.JJJJ`,
darunter Punkte mit `-`.

## v1.5.2 – 24.08.2026

- Die Bedienelemente oben rechts sind auf Deutsch: „Anmelden“, „Registrieren“, „Abmelden“ und „Hallo …“ statt der englischen Vorgaben
- Auf der Match-Detailseite steht „PolySport“ in derselben Schreibweise wie in der Menüleiste

## v1.5.1 – 24.08.2026

- **Dashboard ohne Anmeldung**: die Kennzahlen der aktiven Saison sind jetzt offen einsehbar – Bilanz, Torverhältnis, Spiele, Kaderstärke, letztes Resultat, offenes Match und die drei besten Scorer. Matches, Torfolgen und die vollständige Auswertung bleiben angemeldeten Mitgliedern vorbehalten
- Die Menüleiste hebt jetzt die aufgerufene Seite hervor. Bisher war „Matches“ dauerhaft blau, unabhängig davon, wo man sich befand
- Beim Erfassen eines Tores bleibt „Matches“ hervorgehoben, weil das aus einem Match heraus geschieht
- Auf der Match-Detailseite steht neben dem Spielstand der Mannschaftsname statt „WIR“

## v1.5.0 – 24.08.2026

- **Kader nachträglich anpassen**: solange ein Match nicht gestartet ist, lässt sich der Kader unter „Match bearbeiten" ändern – vergessene Spieler nachtragen oder kurzfristige Absagen austragen, ohne das Match neu anzulegen
- Sobald das 1. Drittel gestartet ist, bleibt der Kader fest und wird nur noch angezeigt: daran hängen Einsätze und Tore
- Spieler mit einem Tor oder Assist in diesem Match lassen sich nicht aus dem Kader entfernen; das Formular sagt, wer betroffen ist
- Ein inzwischen inaktiv geschalteter Spieler bleibt im Kader stehen und wird als „(inaktiv)" angezeigt, statt beim Speichern stillschweigend zu verschwinden
- Neues Symbol in der Browser-Leiste: das Wappen, in 16, 32 und 48 Pixel. Ein bereits geöffneter Tab zeigt unter Umständen noch das alte Zeichen, bis der Browser sein Symbol-Archiv erneuert

## v1.4.2 – 24.08.2026

- Die Änderungsliste erscheint jetzt sofort nach einer Veröffentlichung. Bisher lieferte GitHub sie bis zu fünf Minuten aus einem Zwischenspeicher, sodass nach einem Release erst „keine Änderungsliste abrufbar" stand
- Falls die Abfrage nicht möglich ist, wird der bisherige Weg als Rückfall genutzt

## v1.4.1 – 24.08.2026

- Die Änderungen stehen jetzt direkt im Rahmen „Verfügbar" unter der Prüfzeit, statt in einem eigenen Kasten weiter unten
- Bei mehreren Versionen wird pro Version gruppiert; ab vier Versionen ist die Liste scrollbar, damit die Seite bedienbar bleibt

## v1.4.0 – 24.08.2026

- **Spieler bearbeiten**: Name, E-Mail und Telefon lassen sich ändern. Bereits erfasste Tore und Einsätze bleiben erhalten – aus „Dummy1" wird also der richtige Name, ohne dass die Statistik leidet
- **Match bearbeiten**: Saison, Gegner und Datum lassen sich nachträglich korrigieren. Ein vertippter Gegnername ist damit kein Dauerzustand mehr
- Beim Wechsel der Saison weist das Formular darauf hin, wie viele Tore mitverschoben werden

## v1.3.1 – 24.08.2026

- Admin-Rechte lassen sich in der Benutzerverwaltung vergeben und entziehen
- Die Update-Seite zeigt, was sich seit der installierten Version geändert hat – bei mehreren Versionen alle Einträge dazwischen
- Sicherung gegen Aussperren: die eigenen Admin-Rechte und die des letzten Admins lassen sich nicht entziehen

## v1.3.0 – 24.08.2026

- Neue Rolle **Manager**: darf Spiele leiten, also Spieluhr bedienen, Tore und Gegentore erfassen, Fehleingaben löschen sowie Matches beenden und wieder öffnen
- Manager dürfen bewusst keine Matches anlegen, keine Saisons oder Spieler verwalten und keine Benutzer freigeben
- Benutzerverwaltung zeigt die Rolle jeder Person und erlaubt das Vergeben und Entziehen
- Rollenänderungen greifen sofort, ohne neue Anmeldung

## v1.2.4 – 24.08.2026

- Ein nicht erreichbarer Port lässt ein Update nicht mehr als fehlgeschlagen erscheinen. Entscheidend ist, ob die Anwendung läuft
- Wartezeit nach dem Update von fünf auf zwei Minuten gekürzt
- Hinweise des Update-Skripts erscheinen jetzt auch im Erfolgsfall in der Oberfläche

## v1.2.3 – 24.08.2026

- Das Setup fragt die Erreichbarkeit als Auswahl statt als freie Adresse. Die Frage nach einer „Adresse" verleitete dazu, die IP des Containers einzutippen

## v1.2.2 – 24.08.2026

- Die Prüfung nach Installation und Update beachtet die Adresse, an die der Port gebunden ist. Vorher wurde immer 127.0.0.1 gefragt und ein Fehler gemeldet, obwohl alles lief
- Fehlermeldungen nennen die Ursache statt der letzten Zeilen einer Containerliste

## v1.2.1 – 24.08.2026

- Hängengebliebene Update-Meldungen werden nach 30 Minuten erkannt
- Neuer Knopf, um eine hängende oder fehlgeschlagene Meldung zurückzusetzen

## v1.2.0 – 24.08.2026

- Die installierte Version steht im Footer. Für Admins führt sie direkt zur Update-Seite
- Neues Favicon mit Eishockeyschläger und Puck
- Die Update-Seite prüft beim Öffnen immer frisch statt den zwischengespeicherten Stand zu zeigen
- Das Installationsskript nennt am Ende die tatsächlich erreichbare Adresse

## v1.1.3 – 24.08.2026

- Installation über `git clone` als empfohlener Weg. Kurze Zeilen ohne Anführungszeichen gehen beim Kopieren in ein Terminal nicht kaputt

## v1.1.2 – 24.08.2026

- Das Installationsskript stellt `curl` und `git` sicher, bevor sie gebraucht werden. Minimale Debian-Vorlagen bringen beides nicht mit

## v1.1.1 – 23.08.2026

- Kein Update-Hinweis mehr, wenn die installierte Version unbekannt ist

## v1.1.0 – 23.08.2026

- Die Anwendung prüft im Hintergrund, ob auf GitHub eine neuere Version vorliegt, und zeigt Admins einen Hinweis
- Updates lassen sich mit einem Klick aus der Weboberfläche installieren. Die Datenbank wird vorher gesichert
- Der Anwendungscontainer braucht dafür keinen Zugriff auf Docker: ein Dienst auf dem Host führt das Update aus

## v1.0.0 – 23.08.2026

- Erste vollständige Version
- Zugriffsschutz auf allen Bereichen. Neue Registrierungen müssen von einem Admin freigegeben werden
- Spieluhr auf dem Server: Drittel starten und beenden, Tore bekommen ihre Spielzeit automatisch
- Gegentore werden einzeln mit Zeit erfasst, der Spielstand ergibt sich aus den Toren
- Torfolge mit Zwischenstand, Tore einzeln löschbar
- Saisons anlegen, bearbeiten, aktiv setzen und löschen
- Spieler anlegen und aktiv oder inaktiv schalten
- Dashboard mit Bilanz, Torverhältnis, offenem Match und Top-Scorern
- Saisonblatt mit Toren und Assists pro Spieler und Match, druckbar im Querformat
- Auswertung der Torzeiten nach Drittel
- Geführte Installation und Aktualisierung per Docker Compose
