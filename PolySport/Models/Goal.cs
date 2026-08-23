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

        /// <summary>
        /// Tor des Gegners. Dann bleiben Torschütze und Assist leer.
        /// </summary>
        [Display(Name = "Gegnertor")]
        public bool IsOpponentGoal { get; set; }

        // Nullable, weil Gegnertore keinen Schützen aus unserem Kader haben.
        [Display(Name = "Torschütze")]
        public int? ScorerId { get; set; }
        [ForeignKey("ScorerId")]
        public User? Scorer { get; set; }

        [Display(Name = "Assist")]
        public int? AssistId { get; set; } // Nullable, da es nicht immer einen Assist gibt
        [ForeignKey("AssistId")]
        public User? Assist { get; set; }

        // Zeitpunkt im Spiel. Nullable, weil Tore aus der Zeit vor dieser
        // Erfassung keine Zeitangabe haben.
        [Display(Name = "Drittel")]
        public int? Period { get; set; }

        /// <summary>
        /// Sekunden seit Beginn des Drittels. Kommt normalerweise von der
        /// Spieluhr, kann bei Nachträgen auch von Hand gesetzt werden.
        /// </summary>
        [Display(Name = "Zeit im Drittel")]
        public int? SecondsInPeriod { get; set; }

        /// <summary>Sortierschlüssel: Tore ohne Zeitangabe landen am Ende.</summary>
        [NotMapped]
        public int SortKey => Period.HasValue
            ? Period.Value * 100000 + (SecondsInPeriod ?? 0)
            : int.MaxValue;

        /// <summary>"12:34" bzw. "–" wenn keine Zeit erfasst ist.</summary>
        [NotMapped]
        public string TimeLabel => FormatTime(SecondsInPeriod);

        public static string FormatTime(int? seconds)
        {
            if (!seconds.HasValue) return "–";
            var value = TimeSpan.FromSeconds(seconds.Value);
            return $"{(int)value.TotalMinutes:00}:{value.Seconds:00}";
        }
    }
}
