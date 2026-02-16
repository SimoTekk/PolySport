using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolySport.Models
{
    public class Goal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MatchId { get; set; }
        [ForeignKey("MatchId")]
        public Match? Match { get; set; }

        [Required]
        [Display(Name = "Torschütze")]
        public int ScorerId { get; set; }
        [ForeignKey("ScorerId")]
        public User? Scorer { get; set; }

        [Display(Name = "Assist")]
        public int? AssistId { get; set; } // Nullable, da es nicht immer einen Assist gibt
        [ForeignKey("AssistId")]
        public User? Assist { get; set; }
    }
}