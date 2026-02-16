using System.ComponentModel.DataAnnotations;

namespace PolySport.Models
{
    public class Season
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Der Name der Saison ist erforderlich.")]
        [Display(Name = "Saison")]
        public string Name { get; set; } = string.Empty; // z.B. "Saison 2023/24"

        [Display(Name = "Aktuelle Saison?")]
        public bool IsActive { get; set; }

        // Navigation Property (Eine Saison hat viele Matches)
        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}