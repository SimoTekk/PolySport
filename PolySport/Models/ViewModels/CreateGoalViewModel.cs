using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PolySport.Models.ViewModels
{
    public class CreateGoalViewModel
    {
        public int MatchId { get; set; }
        public string OpponentName { get; set; } = string.Empty; // Nur für die Anzeige (z.B. "Tor gegen SC Bern")

        [Required(ErrorMessage = "Bitte wähle den Torschützen aus.")]
        [Display(Name = "Torschütze (Scorer)")]
        public int ScorerId { get; set; }

        [Display(Name = "Assist (Optional)")]
        public int? AssistId { get; set; } // Nullable, da es nicht immer einen Assist gibt

        // Diese Liste enthält NUR die Spieler, die bei diesem Match im Kader sind
        public IEnumerable<SelectListItem>? MatchRoster { get; set; }
    }
}