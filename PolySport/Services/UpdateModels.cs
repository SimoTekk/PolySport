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

    /// <summary>Zustand eines laufenden oder abgeschlossenen Updates.</summary>
    public class UpdateStatus
    {
        public UpdateState State { get; init; } = UpdateState.Idle;
        public string? Version { get; init; }
        public string? Message { get; init; }
        public DateTime? UpdatedAt { get; init; }
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
        Failed
    }
}
