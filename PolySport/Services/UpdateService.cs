using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PolySport.Services
{
    public interface IUpdateService
    {
        /// <summary>Zuletzt geprüftes Ergebnis, ohne Netzwerkzugriff. Für die Anzeige im Layout.</summary>
        UpdateInfo Cached { get; }

        /// <summary>Fragt GitHub ab und aktualisiert den Zwischenspeicher.</summary>
        Task<UpdateInfo> CheckAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Kann diese Installation sich selbst aktualisieren? Nur wenn der
        /// Ablageordner für die Anforderung vorhanden und beschreibbar ist.
        /// </summary>
        bool CanInstallUpdates { get; }

        /// <summary>Fordert das Update an. Ausgeführt wird es vom Wächter auf dem Host.</summary>
        Task<bool> RequestInstallAsync(string version);

        UpdateStatus GetStatus();

        /// <summary>Löscht eine hängengebliebene Statusmeldung.</summary>
        bool ClearStatus();

        /// <summary>
        /// Änderungen zwischen der installierten und der neuesten Version,
        /// neueste zuerst. Leer, wenn die Datei nicht gelesen werden kann.
        /// </summary>
        Task<List<ChangelogEntry>> GetPendingChangesAsync(CancellationToken cancellationToken = default);

        string RepositoryUrl { get; }
    }

    /// <summary>
    /// Prüft anhand der Git-Tags auf GitHub, ob eine neuere Version vorliegt.
    /// Tags genügen, es braucht kein veröffentlichtes GitHub-Release.
    /// </summary>
    public class UpdateService : IUpdateService
    {
        /// <summary>Ab dieser Dauer ohne Fortschritt gilt ein Update als hängengeblieben.</summary>
        private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(30);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<UpdateService> _logger;
        private readonly string _repository;
        private readonly string _stateDirectory;
        private readonly string _currentVersion;

        private UpdateInfo _cached;

        private static readonly TimeSpan ChangelogCacheDuration = TimeSpan.FromMinutes(10);
        private List<ChangelogEntry>? _changelog;
        private DateTime _changelogLoadedAt;

        public UpdateService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<UpdateService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            _repository = configuration["Update:Repository"] ?? "SimoTekk/PolySport";
            _stateDirectory = configuration["Update:StateDirectory"] ?? "/app/state";

            // Die laufende Version wird beim Start als Umgebungsvariable gesetzt
            // (siehe docker-compose.yml und deploy/install.sh).
            var version = configuration["POLYSPORT_VERSION"];
            _currentVersion = string.IsNullOrWhiteSpace(version) ? "unbekannt" : version.Trim();

            _cached = new UpdateInfo { CurrentVersion = _currentVersion };
        }

        public UpdateInfo Cached => _cached;

        public string RepositoryUrl => $"https://github.com/{_repository}";

        public bool CanInstallUpdates
        {
            get
            {
                try
                {
                    return Directory.Exists(_stateDirectory);
                }
                catch
                {
                    return false;
                }
            }
        }

        public async Task<UpdateInfo> CheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PolySport", "1.0"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                var url = $"https://api.github.com/repos/{_repository}/tags?per_page=100";
                using var response = await client.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return Store(new UpdateInfo
                    {
                        CurrentVersion = _currentVersion,
                        CheckedAt = DateTime.UtcNow,
                        Error = DescribeFailure(response)
                    });
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                var latest = document.RootElement.EnumerateArray()
                    .Select(tag => tag.TryGetProperty("name", out var name) ? name.GetString() : null)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    // Vorabversionen wie v1.1.0-beta1 werden nicht angeboten
                    .Where(name => !IsPreRelease(name!))
                    .Select(name => (Name: name!, Version: ParseVersion(name!)))
                    .Where(entry => entry.Version != null)
                    .OrderByDescending(entry => entry.Version)
                    .FirstOrDefault();

                if (latest.Name == null)
                {
                    return Store(new UpdateInfo
                    {
                        CurrentVersion = _currentVersion,
                        CheckedAt = DateTime.UtcNow,
                        Error = "Keine Versions-Tags gefunden"
                    });
                }

                var current = ParseVersion(_currentVersion);

                // Ist die laufende Version unbekannt (POLYSPORT_VERSION nicht
                // gesetzt, etwa in der Entwicklung), lässt sich nichts
                // vergleichen. Dann wird die neueste Version nur angezeigt und
                // kein Update gemeldet – sonst stünde dauerhaft ein Hinweis da.
                return Store(new UpdateInfo
                {
                    CurrentVersion = _currentVersion,
                    LatestVersion = latest.Name,
                    IsUpdateAvailable = current != null && latest.Version! > current,
                    CheckedAt = DateTime.UtcNow,
                    ReleaseUrl = $"https://github.com/{_repository}/releases/tag/{latest.Name}",
                    Error = current == null
                        ? "Installierte Version unbekannt, Vergleich nicht möglich"
                        : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Versionsprüfung fehlgeschlagen: {Message}", ex.Message);
                return Store(new UpdateInfo
                {
                    CurrentVersion = _currentVersion,
                    CheckedAt = DateTime.UtcNow,
                    Error = "Prüfung nicht möglich"
                });
            }
        }

        public async Task<bool> RequestInstallAsync(string version)
        {
            if (!CanInstallUpdates) return false;

            try
            {
                // Der Wächter auf dem Host reagiert auf diese Datei.
                var request = Path.Combine(_stateDirectory, "update-request");
                await File.WriteAllTextAsync(request, version + Environment.NewLine);

                // Sofort einen Zwischenstand schreiben, damit die Seite etwas anzeigt.
                var status = Path.Combine(_stateDirectory, "update-status");
                await File.WriteAllTextAsync(status, string.Join(Environment.NewLine, new[]
                {
                    "state=requested",
                    $"version={version}",
                    "message=Update angefordert, warte auf den Dienst auf dem Host",
                    $"updated={DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)}"
                }) + Environment.NewLine);

                _logger.LogInformation("Update auf {Version} wurde angefordert.", version);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update konnte nicht angefordert werden.");
                return false;
            }
        }

        public UpdateStatus GetStatus()
        {
            try
            {
                var path = Path.Combine(_stateDirectory, "update-status");
                if (!File.Exists(path)) return new UpdateStatus();

                // Einfaches schlüssel=wert-Format, damit es sich aus einem
                // Shell-Skript ohne Klimmzüge schreiben lässt.
                var values = File.ReadAllLines(path)
                    .Select(line => line.Split('=', 2))
                    .Where(parts => parts.Length == 2)
                    .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);

                values.TryGetValue("state", out var state);
                values.TryGetValue("version", out var version);
                values.TryGetValue("message", out var message);
                values.TryGetValue("updated", out var updated);

                var parsedState = state?.ToLowerInvariant() switch
                {
                    "requested" => UpdateState.Requested,
                    "running" => UpdateState.Running,
                    "success" => UpdateState.Success,
                    "failed" => UpdateState.Failed,
                    _ => UpdateState.Idle
                };

                DateTime? updatedAt = DateTime.TryParse(updated, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var parsed) ? parsed : null;

                // Ein Update dauert Minuten, nicht Stunden. Meldet der Dienst
                // auf dem Host so lange keinen Fortschritt, ist er
                // abgebrochen, ohne einen Abschluss zu schreiben. Sonst würde
                // die Oberfläche für immer "läuft" zeigen.
                var running = parsedState is UpdateState.Requested or UpdateState.Running;
                if (running && updatedAt.HasValue &&
                    DateTime.UtcNow - updatedAt.Value > StaleAfter)
                {
                    parsedState = UpdateState.Stale;
                }

                return new UpdateStatus
                {
                    State = parsedState,
                    Version = version,
                    Message = message,
                    UpdatedAt = updatedAt
                };
            }
            catch
            {
                return new UpdateStatus();
            }
        }

        public async Task<List<ChangelogEntry>> GetPendingChangesAsync(CancellationToken cancellationToken = default)
        {
            var info = _cached.CheckedAt.HasValue ? _cached : await CheckAsync(cancellationToken);
            if (string.IsNullOrEmpty(info.LatestVersion)) return new List<ChangelogEntry>();

            var all = await LoadChangelogAsync(cancellationToken);
            if (all.Count == 0) return all;

            var current = ParseVersion(info.CurrentVersion);
            var latest = ParseVersion(info.LatestVersion);

            // Ist die installierte Version unbekannt, lässt sich keine Spanne
            // bilden – dann nur der Eintrag zur neuesten Version.
            if (current == null)
            {
                return all.Where(e => ParseVersion(e.Version) == latest).ToList();
            }

            return all
                .Where(e =>
                {
                    var version = ParseVersion(e.Version);
                    return version != null
                        && version > current
                        && (latest == null || version <= latest);
                })
                .ToList();
        }

        /// <summary>Holt CHANGELOG.md und zerlegt sie, mit kurzem Zwischenspeicher.</summary>
        private async Task<List<ChangelogEntry>> LoadChangelogAsync(CancellationToken cancellationToken)
        {
            if (_changelog != null && DateTime.UtcNow - _changelogLoadedAt < ChangelogCacheDuration)
                return _changelog;

            var markdown = await FetchChangelogTextAsync(cancellationToken);
            if (markdown == null) return new List<ChangelogEntry>();

            _changelog = ParseChangelog(markdown);
            _changelogLoadedAt = DateTime.UtcNow;
            return _changelog;
        }

        /// <summary>
        /// Holt CHANGELOG.md, bevorzugt über die GitHub-API.
        ///
        /// Grund: raw.githubusercontent.com liefert bis zu fünf Minuten die
        /// alte Fassung aus seinem Zwischenspeicher. Genau dann, wenn man die
        /// Notizen braucht – direkt nach einer Veröffentlichung – wären sie
        /// noch nicht da. Die API antwortet sofort mit dem aktuellen Stand.
        /// Klappt die API nicht (etwa bei erschöpftem Anfragelimit), dient der
        /// Raw-Weg als Rückfall.
        /// </summary>
        private async Task<string?> FetchChangelogTextAsync(CancellationToken cancellationToken)
        {
            var apiUrl = $"https://api.github.com/repos/{_repository}/contents/CHANGELOG.md?ref=master";
            var rawUrl = $"https://raw.githubusercontent.com/{_repository}/master/CHANGELOG.md";

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PolySport", "1.0"));
                // Liefert den Dateiinhalt direkt statt in JSON verpackt
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.raw"));

                using var response = await client.GetAsync(apiUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogInformation(
                    "Änderungsliste über die API nicht verfügbar ({Status}), versuche den Rückfall.",
                    (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Änderungsliste über die API fehlgeschlagen: {Message}", ex.Message);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PolySport", "1.0"));

                return await client.GetStringAsync(rawUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Änderungsliste konnte nicht geladen werden: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Erwartet Abschnitte "## v1.2.3 – Datum" mit Punkten, die mit "-"
        /// beginnen. Alles andere wird übersprungen.
        /// </summary>
        internal static List<ChangelogEntry> ParseChangelog(string markdown)
        {
            var entries = new List<ChangelogEntry>();
            ChangelogEntry? current = null;
            var items = new List<string>();

            void Flush()
            {
                if (current == null) return;
                entries.Add(new ChangelogEntry
                {
                    Version = current.Version,
                    Date = current.Date,
                    Items = new List<string>(items)
                });
                items.Clear();
            }

            foreach (var raw in markdown.Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.TrimEnd();

                var heading = Regex.Match(line, @"^##\s+(v?\d+(?:\.\d+)*)\s*(?:[–\-—:]\s*(.*))?$");
                if (heading.Success)
                {
                    Flush();
                    current = new ChangelogEntry
                    {
                        Version = heading.Groups[1].Value.Trim(),
                        Date = string.IsNullOrWhiteSpace(heading.Groups[2].Value)
                            ? null
                            : heading.Groups[2].Value.Trim()
                    };
                    continue;
                }

                if (current == null) continue;

                var item = Regex.Match(line, @"^\s*[-*]\s+(.+)$");
                if (item.Success)
                {
                    // Fettschrift aus Markdown entfernen, die Anzeige ist reiner Text
                    var text = item.Groups[1].Value.Replace("**", "").Trim();
                    if (text.Length > 0) items.Add(text);
                }
            }

            Flush();
            return entries;
        }

        public bool ClearStatus()
        {
            try
            {
                var path = Path.Combine(_stateDirectory, "update-status");
                if (File.Exists(path)) File.Delete(path);

                // Eine nicht abgeholte Anforderung ebenfalls entfernen, sonst
                // startet der Wächter sie beim nächsten Mal doch noch.
                var request = Path.Combine(_stateDirectory, "update-request");
                if (File.Exists(request)) File.Delete(request);

                _logger.LogInformation("Update-Status wurde zurückgesetzt.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update-Status konnte nicht zurückgesetzt werden.");
                return false;
            }
        }

        private UpdateInfo Store(UpdateInfo info)
        {
            _cached = info;
            return info;
        }

        /// <summary>
        /// Verständliche Meldung zu einer fehlgeschlagenen Anfrage. GitHub
        /// lässt anonym 60 Anfragen pro Stunde zu – wird das erreicht, kommt
        /// 403 zurück, was ohne Erklärung ratlos macht.
        /// </summary>
        private static string DescribeFailure(HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return "Repository nicht erreichbar (privat?)";

            var isRateLimited =
                response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                (int)response.StatusCode == 429;

            if (isRateLimited)
            {
                var wait = "";
                if (response.Headers.TryGetValues("x-ratelimit-reset", out var values)
                    && long.TryParse(values.FirstOrDefault(), out var epoch))
                {
                    var minutes = (int)Math.Ceiling(
                        (DateTimeOffset.FromUnixTimeSeconds(epoch) - DateTimeOffset.UtcNow).TotalMinutes);
                    if (minutes > 0) wait = $", nächster Versuch in {minutes} Minuten möglich";
                }

                return $"Anfragelimit von GitHub erreicht{wait}";
            }

            return $"GitHub antwortete mit {(int)response.StatusCode}";
        }

        /// <summary>
        /// Vorabversion nach dem Muster von Semantic Versioning: alles nach
        /// einem Bindestrich, z.B. v1.1.0-beta1 oder v2.0.0-rc.2.
        /// </summary>
        private static bool IsPreRelease(string tagName) => tagName.Contains('-');

        /// <summary>"v1.2.3" oder "1.2" zu einer Version. Null wenn unbrauchbar.</summary>
        private static Version? ParseVersion(string value)
        {
            var match = Regex.Match(value.Trim(), @"^v?(\d+(?:\.\d+){0,3})");
            if (!match.Success) return null;

            var text = match.Groups[1].Value;
            // System.Version braucht mindestens zwei Bestandteile
            if (!text.Contains('.')) text += ".0";

            return Version.TryParse(text, out var version) ? version : null;
        }
    }
}
