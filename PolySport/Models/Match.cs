using PolySport.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolySport.Models
{
    public class Match
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Saison")]
        public int SeasonId { get; set; }
        [ForeignKey("SeasonId")]
        public Season? Season { get; set; }

        [Required(ErrorMessage = "Gegnername fehlt.")]
        [Display(Name = "Gegner")]
        public string OpponentName { get; set; } = string.Empty;

        [Display(Name = "Tore Gegner")]
        public int OpponentScore { get; set; }

        [Required]
        [Display(Name = "Datum")]
        public DateTime MatchDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();
        public ICollection<Goal> Goals { get; set; } = new List<Goal>();
    }
}