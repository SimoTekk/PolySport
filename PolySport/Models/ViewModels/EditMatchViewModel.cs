using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PolySport.Models.ViewModels
{
    /// <summary>
    /// Stammdaten eines bestehenden Matches. Kader und Tore werden hier nicht
    /// angefasst – die hängen an eigenen Ansichten.
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

        /// <summary>Alle Saisons, nicht nur die aktive – ein Match darf verschoben werden.</summary>
        public IEnumerable<SelectListItem>? AvailableSeasons { get; set; }

        /// <summary>Nur zur Anzeige: wie viele Tore hängen daran.</summary>
        public int GoalCount { get; set; }
    }
}
