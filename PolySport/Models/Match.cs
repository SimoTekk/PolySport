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

        [Required]
        [Display(Name = "Datum")]
        public DateTime MatchDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Heimspiel (true) oder Auswärtsspiel (false). Null bleibt zulässig:
        /// Matches aus der Zeit vor diesem Feld haben keine Angabe, und ein
        /// falsches „Auswärts“ wäre schlechter als eine offene Angabe.
        /// </summary>
        [Display(Name = "Heim oder Auswärts")]
        public bool? IsHomeGame { get; set; }

        /// <summary>
        /// Beendetes Spiel: das Resultat zählt für die Bilanz und es können
        /// keine Tore mehr erfasst werden.
        /// </summary>
        [Display(Name = "Beendet")]
        public bool IsFinished { get; set; }

        public DateTime? FinishedAt { get; set; }

        // --- Spieluhr ---------------------------------------------------
        // Die Uhr läuft auf dem Server: gespeichert wird nur, welches Drittel
        // aktuell dran ist und seit wann es läuft. Alles andere wird daraus
        // berechnet, damit ein Seiten-Neuladen nichts verfälscht.

        /// <summary>Letztes gestartetes Drittel. 0 = Spiel noch nicht gestartet.</summary>
        [Display(Name = "Aktuelles Drittel")]
        public int CurrentPeriod { get; set; }

        /// <summary>
        /// Startzeit (UTC) des laufenden Drittels. Null bedeutet: die Uhr steht –
        /// entweder noch nicht gestartet oder Pause zwischen zwei Dritteln.
        /// </summary>
        public DateTime? PeriodStartedAt { get; set; }

        // Navigation Properties
        public ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();
        public ICollection<Goal> Goals { get; set; } = new List<Goal>();

        // Der Spielstand wird aus den Tor-Datensätzen berechnet, damit es
        // keine zweite Wahrheit neben den Toren gibt.
        [NotMapped]
        public int OurScore => Goals.Count(g => !g.IsOpponentGoal);

        [NotMapped]
        public int OpponentScore => Goals.Count(g => g.IsOpponentGoal);

        [NotMapped]
        public bool HasStarted => CurrentPeriod > 0;

        /// <summary>„Heimspiel“, „Auswärtsspiel“ oder „–“, wenn nichts erfasst ist.</summary>
        [NotMapped]
        public string VenueLabel => IsHomeGame switch
        {
            true => "Heimspiel",
            false => "Auswärtsspiel",
            _ => "–"
        };

        /// <summary>Kurzform für Tabellen und den Spielplan: H, A oder –.</summary>
        [NotMapped]
        public string VenueShort => IsHomeGame switch
        {
            true => "H",
            false => "A",
            _ => "–"
        };

        /// <summary>Die Uhr läuft gerade.</summary>
        [NotMapped]
        public bool IsPeriodRunning => !IsFinished && PeriodStartedAt.HasValue;

        /// <summary>Pause zwischen zwei Dritteln – die Uhr wartet auf den nächsten Start.</summary>
        [NotMapped]
        public bool IsInBreak => !IsFinished && HasStarted && !PeriodStartedAt.HasValue;

        [NotMapped]
        public bool CanStartNextPeriod => !IsFinished && !PeriodStartedAt.HasValue && CurrentPeriod < 3;

        [NotMapped]
        public int NextPeriod => CurrentPeriod + 1;

        /// <summary>Sekunden seit dem Start des laufenden Drittels.</summary>
        [NotMapped]
        public int ElapsedSecondsInPeriod => PeriodStartedAt.HasValue
            ? Math.Max(0, (int)(DateTime.UtcNow - PeriodStartedAt.Value).TotalSeconds)
            : 0;

        [NotMapped]
        public string StatusLabel
        {
            get
            {
                if (IsFinished) return "Beendet";
                if (IsPeriodRunning) return $"{CurrentPeriod}. Drittel läuft";
                if (IsInBreak) return CurrentPeriod >= 3
                    ? "3. Drittel beendet"
                    : $"Pause nach dem {CurrentPeriod}. Drittel";
                return "Nicht gestartet";
            }
        }
    }
}
