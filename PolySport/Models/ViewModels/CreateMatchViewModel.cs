using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PolySport.Models.ViewModels
{
    public class CreateMatchViewModel
    {
        [Required(ErrorMessage = "Bitte wähle eine Saison aus.")]
        [Display(Name = "Saison")]
        public int SeasonId { get; set; }

        [Required(ErrorMessage = "Gegnername fehlt.")]
        [Display(Name = "Gegner")]
        public string OpponentName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Spieldatum")]
        public DateTime MatchDate { get; set; } = DateTime.Now;

        /// <summary>Null = keine Angabe. Siehe Match.IsHomeGame.</summary>
        [Display(Name = "Heim oder Auswärts")]
        public bool? IsHomeGame { get; set; }

        /// <summary>
        /// Die IDs der angehakten Spieler. Bewusst nullable: ein Match ohne
        /// Kader muss sich anlegen lassen – am Saisonanfang werden zuerst alle
        /// Termine erfasst und der Kader erst vor dem Spiel nachgetragen.
        /// Ein nicht-nullable List würde eine implizite Pflichtregel bekommen.
        /// </summary>
        [Display(Name = "Spieler im Kader")]
        public List<int>? SelectedPlayerIds { get; set; } = new List<int>();

        /// <summary>Wer im Tor steht. Null = keine Angabe.</summary>
        [Display(Name = "Torhüter")]
        public int? GoalkeeperId { get; set; }

        // Diese Listen füllen wir im Controller, um sie im HTML-Dropdown/Listen anzuzeigen
        public IEnumerable<SelectListItem>? AvailableSeasons { get; set; }

        /// <summary>Auswahl für die Ankreuzliste des Kaders.</summary>
        public List<RosterPlayerOption> RosterOptions { get; set; } = new List<RosterPlayerOption>();

        /// <summary>Zusammengefasst für die geteilte Teilansicht _RosterPicker.</summary>
        public RosterPickerViewModel Picker => new RosterPickerViewModel
        {
            Players = RosterOptions,
            SelectedPlayerIds = SelectedPlayerIds ?? new List<int>(),
            GoalkeeperId = GoalkeeperId
        };
    }
}
