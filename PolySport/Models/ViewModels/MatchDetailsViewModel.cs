namespace PolySport.Models.ViewModels
{
    public class MatchDetailsViewModel
    {
        public int MatchId { get; set; }
        public string SeasonName { get; set; } = string.Empty;
        public string OpponentName { get; set; } = string.Empty;
        public DateTime MatchDate { get; set; }

        /// <summary>„Heimspiel“, „Auswärtsspiel“ oder „–“.</summary>
        public string VenueLabel { get; set; } = "–";
        public bool? IsHomeGame { get; set; }

        // Beide Resultate werden aus den Tor-Datensätzen gezählt
        public int OurScore { get; set; }
        public int OpponentScore { get; set; }

        public bool IsFinished { get; set; }
        public DateTime? FinishedAt { get; set; }

        // --- Zustand der Spieluhr ---
        public int CurrentPeriod { get; set; }
        public bool HasStarted { get; set; }
        public bool IsPeriodRunning { get; set; }
        public bool IsInBreak { get; set; }
        public bool CanStartNextPeriod { get; set; }
        public int NextPeriod { get; set; }
        public int ElapsedSecondsInPeriod { get; set; }
        public string StatusLabel { get; set; } = string.Empty;

        /// <summary>"Sieg" / "Niederlage" / "Unentschieden" – nur bei beendeten Spielen sinnvoll.</summary>
        public string ResultLabel => OurScore > OpponentScore ? "Sieg"
                                   : OurScore < OpponentScore ? "Niederlage"
                                   : "Unentschieden";

        public List<string> RosterNames { get; set; } = new List<string>();

        /// <summary>Alle Tore chronologisch, mit laufendem Zwischenstand.</summary>
        public List<GoalDisplay> Timeline { get; set; } = new List<GoalDisplay>();
    }

    // Eine kleine Hilfsklasse nur für die Anzeige der Tore
    public class GoalDisplay
    {
        public int GoalId { get; set; }
        public bool IsOpponentGoal { get; set; }

        public int? Period { get; set; }
        public int? SecondsInPeriod { get; set; }

        public string? ScorerName { get; set; }
        public string? AssistName { get; set; } // Kann leer sein

        // Zwischenstand nach diesem Tor
        public int ScoreOurs { get; set; }
        public int ScoreOpponent { get; set; }

        public string TimeLabel => Goal.FormatTime(SecondsInPeriod);

        public string PeriodLabel => Period.HasValue ? $"{Period}. Drittel" : "Ohne Zeitangabe";
    }
}
