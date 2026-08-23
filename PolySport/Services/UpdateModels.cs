namespace PolySport.Services
{
    /// <summary>Ergebnis der Versionsprüfung gegen GitHub.</summary>
    public class UpdateInfo
    {
        public string CurrentVersion { get; init; } = "unbekannt";
        public string? LatestVersion { get; init; }
        public bool IsUpdateAvailable { get; init; }
        public DateTime? CheckedAt { get; init; }
        public string? ReleaseUrl { get; init; }

        /// <summary>Gesetzt, wenn die Prüfung nicht möglich war (z.B. kein Netz).</summary>
        public string? Error { get; init; }
    }

    /// <summary>Ein Abschnitt aus CHANGELOG.md.</summary>
    public class ChangelogEntry
    {
        /// <summary>Wie in der Datei geschrieben, z.B. "v1.3.0".</summary>
        public string Version { get; init; } = string.Empty;

        /// <summary>Freitext hinter der Version, z.B. "24.08.2026".</summary>
        public string? Date { get; init; }

        public List<string> Items { get; init; } = new List<string>();
    }

    /// <summary>Zustand eines laufenden oder abgeschlossenen Updates.</summary>
    public class UpdateStatus
    {
        public UpdateState State { get; init; } = UpdateState.Idle;
        public string? Version { get; init; }
        public string? Message { get; init; }
        public DateTime? UpdatedAt { get; init; }

        /// <summary>Wie lange die Meldung schon unverändert ist.</summary>
        public TimeSpan? Age => UpdatedAt.HasValue ? DateTime.UtcNow - UpdatedAt.Value : null;
    }

    public enum UpdateState
    {
        /// <summary>Kein Update angefordert.</summary>
        Idle,

        /// <summary>Angefordert, der Wächter auf dem Host hat noch nicht übernommen.</summary>
        Requested,

        /// <summary>Läuft gerade.</summary>
        Running,

        Success,
        Failed,

        /// <summary>
        /// Meldet seit langem keinen Fortschritt mehr. Der Dienst auf dem Host
        /// ist wahrscheinlich abgebrochen, ohne einen Abschluss zu schreiben.
        /// </summary>
        Stale
    }
}
