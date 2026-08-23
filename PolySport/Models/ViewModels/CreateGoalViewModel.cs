using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PolySport.Models.ViewModels
{
    /// <summary>
    /// Gemeinsame Basis der beiden Tor-Formulare. Läuft die Spieluhr, kommt die
    /// Zeit von ihr (LiveClock = true) und die Felder sind versteckt. Steht die
    /// Uhr, werden Drittel und Minute von Hand nachgetragen.
    /// </summary>
    public abstract class GoalTimeInputBase
    {
        public int MatchId { get; set; }
        public string OpponentName { get; set; } = string.Empty;

        /// <summary>Zeit kommt von der laufenden Spieluhr.</summary>
        public bool LiveClock { get; set; }

        [Display(Name = "Drittel")]
        public int? Period { get; set; }

        /// <summary>
        /// Bei laufender Uhr beim Öffnen des Formulars festgehalten – so zählt
        /// der Moment des Klicks, nicht der des Abschickens.
        /// </summary>
        public int? SecondsInPeriod { get; set; }

        /// <summary>Nur für die manuelle Nacherfassung.</summary>
        [Display(Name = "Minute im Drittel")]
        public int? MinuteInPeriod { get; set; }

        /// <summary>Anzeige der festgehaltenen Zeit, z.B. "12:34".</summary>
        public string CapturedTimeLabel => Goal.FormatTime(SecondsInPeriod);
    }

    public class CreateGoalViewModel : GoalTimeInputBase
    {
        [Required(ErrorMessage = "Bitte wähle den Torschützen aus.")]
        [Display(Name = "Torschütze")]
        public int ScorerId { get; set; }

        [Display(Name = "Assist (optional)")]
        public int? AssistId { get; set; } // Nullable, da es nicht immer einen Assist gibt

        // Diese Liste enthält NUR die Spieler, die bei diesem Match im Kader sind
        public IEnumerable<SelectListItem>? MatchRoster { get; set; }
    }

    public class CreateOpponentGoalViewModel : GoalTimeInputBase
    {
    }
}
