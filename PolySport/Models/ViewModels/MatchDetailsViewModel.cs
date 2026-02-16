namespace PolySport.Models.ViewModels
{
    public class MatchDetailsViewModel
    {
        public int MatchId { get; set; }
        public string SeasonName { get; set; } = string.Empty;
        public string OpponentName { get; set; } = string.Empty;
        public DateTime MatchDate { get; set; }

        // Die Resultate
        public int OpponentScore { get; set; }
        public int OurScore { get; set; } // Wird automatisch berechnet!

        // Listen für die Anzeige
        public List<string> RosterNames { get; set; } = new List<string>();
        public List<GoalDisplay> Goals { get; set; } = new List<GoalDisplay>();
    }

    // Eine kleine Hilfsklasse nur für die Anzeige der Tore
    public class GoalDisplay
    {
        public string ScorerName { get; set; } = string.Empty;
        public string? AssistName { get; set; } // Kann leer sein
    }
}