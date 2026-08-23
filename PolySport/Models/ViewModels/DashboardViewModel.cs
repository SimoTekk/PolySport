namespace PolySport.Models.ViewModels
{
    /// <summary>
    /// Kennzahlen für die Startseite. Bezieht sich immer auf die aktive Saison.
    /// </summary>
    public class DashboardViewModel
    {
        public bool HasActiveSeason { get; set; }
        public string SeasonName { get; set; } = string.Empty;

        public int MatchesTotal { get; set; }
        public int MatchesFinished { get; set; }
        public int MatchesOpen { get; set; }

        // Bilanz zählt nur beendete Spiele
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }

        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int GoalDifference => GoalsFor - GoalsAgainst;

        public int ActivePlayers { get; set; }

        public List<PlayerStatViewModel> TopScorers { get; set; } = new List<PlayerStatViewModel>();

        public MatchTile? LastMatch { get; set; }
        public MatchTile? OpenMatch { get; set; }

        /// <summary>Nur für Admins gefüllt.</summary>
        public int PendingApprovals { get; set; }
    }

    public class MatchTile
    {
        public int Id { get; set; }
        public string OpponentName { get; set; } = string.Empty;
        public DateTime MatchDate { get; set; }
        public int OurScore { get; set; }
        public int OpponentScore { get; set; }
        public bool IsFinished { get; set; }

        public string ResultLabel => OurScore > OpponentScore ? "Sieg"
                                   : OurScore < OpponentScore ? "Niederlage"
                                   : "Unentschieden";
    }
}
