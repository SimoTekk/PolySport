using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolySport.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Der Name ist erforderlich.")]
        [Display(Name = "Spielername")]
        public string Username { get; set; } = string.Empty;

        public string? PictureUrl { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }

        [Display(Name = "Aktiver Spieler?")]
        public bool IsActive { get; set; } = true; // Für den Soft-Delete!

        // Navigation Properties
        public ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();

        [InverseProperty("Scorer")]
        public ICollection<Goal> GoalsScored { get; set; } = new List<Goal>();

        [InverseProperty("Assist")]
        public ICollection<Goal> Assists { get; set; } = new List<Goal>();
    }
}