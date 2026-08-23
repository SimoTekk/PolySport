using PolySport.Services;

namespace PolySport.Models.ViewModels
{
    public class UpdatePageViewModel
    {
        public UpdateInfo Info { get; set; } = new UpdateInfo();
        public UpdateStatus Status { get; set; } = new UpdateStatus();

        /// <summary>Änderungen zwischen installierter und neuester Version, neueste zuerst.</summary>
        public List<ChangelogEntry> PendingChanges { get; set; } = new List<ChangelogEntry>();

        /// <summary>Kann sich diese Installation selbst aktualisieren?</summary>
        public bool CanInstall { get; set; }

        public string RepositoryUrl { get; set; } = string.Empty;

        public bool IsUpdateRunning =>
            Status.State == UpdateState.Requested || Status.State == UpdateState.Running;

        /// <summary>Wie viele Versionen liegen dazwischen?</summary>
        public int VersionsBehind => PendingChanges.Count;
    }
}
