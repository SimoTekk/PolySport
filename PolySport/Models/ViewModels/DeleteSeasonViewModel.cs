namespace PolySport.Models.ViewModels
{
    /// <summary>
    /// Zeigt vor dem Löschen einer Saison, was per Cascade Delete mitgeht.
    /// </summary>
    public class DeleteSeasonViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public List<MatchSummary> Matches { get; set; } = new List<MatchSummary>();

        public int TotalGoals => Matches.Sum(m => m.GoalCount);
    }

    public class MatchSummary
    {
        public string OpponentName { get; set; } = string.Empty;
        public DateTime MatchDate { get; set; }
        public int GoalCount { get; set; }
    }
}
