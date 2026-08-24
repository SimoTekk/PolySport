using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PolySport.Models.ViewModels
{
    /// <summary>
    /// Stammdaten eines bestehenden Matches. Der Kader lässt sich nur ändern,
    /// solange das Match noch nicht gestartet ist – sobald die Uhr lief, hängen
    /// Tore und Einsätze daran. Tore werden hier nie angefasst, die hängen an
    /// der Detailseite.
    /// </summary>
    public class EditMatchViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Saison")]
        public int SeasonId { get; set; }

        [Required(ErrorMessage = "Gegnername fehlt.")]
        [Display(Name = "Gegner")]
        public string OpponentName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Datum")]
        [DataType(DataType.DateTime)]
        public DateTime MatchDate { get; set; }

        /// <summary>Null = keine Angabe. Siehe Match.IsHomeGame.</summary>
        [Display(Name = "Heim oder Auswärts")]
        public bool? IsHomeGame { get; set; }

        /// <summary>
        /// Bewusst nullable: sonst hängt Identity... bzw. die Validierung eine
        /// implizite Pflichtregel daran, und ein absichtlich leerer Kader liesse
        /// sich nicht speichern.
        /// </summary>
        [Display(Name = "Spieler im Kader")]
        public List<int>? SelectedPlayerIds { get; set; } = new List<int>();

        /// <summary>Alle Saisons, nicht nur die aktive – ein Match darf verschoben werden.</summary>
        public IEnumerable<SelectListItem>? AvailableSeasons { get; set; }

        /// <summary>
        /// Auswahl für den Kader: aktive Spieler und zusätzlich die, die schon
        /// im Kader stehen – sonst würde ein inzwischen deaktivierter Spieler
        /// beim Speichern unbemerkt aus dem Kader fallen.
        /// </summary>
        public IEnumerable<SelectListItem>? AvailablePlayers { get; set; }

        /// <summary>Match noch nicht gestartet – nur dann ist der Kader offen.</summary>
        public bool CanEditRoster { get; set; }

        /// <summary>Nur zur Anzeige, wenn der Kader gesperrt ist.</summary>
        public List<string> RosterNames { get; set; } = new List<string>();

        /// <summary>Nur zur Anzeige: wie viele Tore hängen daran.</summary>
        public int GoalCount { get; set; }
    }
}
