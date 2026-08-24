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

        // Hier speichern wir die IDs der Spieler, die der Admin im Formular anhakt
        [Display(Name = "Spieler im Kader")]
        public List<int> SelectedPlayerIds { get; set; } = new List<int>();

        // Diese Listen füllen wir im Controller, um sie im HTML-Dropdown/Listen anzuzeigen
        public IEnumerable<SelectListItem>? AvailableSeasons { get; set; }
        public IEnumerable<SelectListItem>? AvailablePlayers { get; set; }
    }
}