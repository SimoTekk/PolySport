namespace PolySport.Models.ViewModels
{
    /// <summary>
    /// Kennzahlen für die Startseite. Bezieht sich immer auf die aktive Saison.
    /// </summary>
    public class DashboardViewModel
    {
        public bool HasActiveSeason { get; set; }
        public string SeasonName { get; set; } = string.Empty;

        public int MatchesTotal { get; set; }
        public int MatchesFinished { get; set; }
        public int MatchesOpen { get; set; }

        // Bilanz zählt nur beendete Spiele
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }

        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int GoalDifference => GoalsFor - GoalsAgainst;

        public int ActivePlayers { get; set; }

        public List<PlayerStatViewModel> Scorers { get; set; } = new List<PlayerStatViewModel>();

        public MatchTile? LastMatch { get; set; }
        public MatchTile? OpenMatch { get; set; }

        /// <summary>Aufgebot des nächsten offenen Matches. Null, wenn keins ansteht.</summary>
        public LineupViewModel? Lineup { get; set; }

        /// <summary>
        /// Präsenzliste der Saison: wer wie oft im Einsatz war. Gezählt werden
        /// nur beendete Matches – ein Kader für einen offenen Termin ist eine
        /// Planung, kein Einsatz.
        /// </summary>
        public List<PresenceEntry> Presence { get; set; } = new List<PresenceEntry>();

        /// <summary>Nur für Admins gefüllt.</summary>
        public int PendingApprovals { get; set; }
    }

    /// <summary>
    /// Aufgebot vor dem Match: Torhüter und Feldspieler. Steht auch ohne
    /// Anmeldung auf dem Dashboard, damit es vor dem Spiel als Ganzes in den
    /// Chat der Mannschaft kopiert werden kann.
    /// </summary>
    public class LineupViewModel
    {
        public int MatchId { get; set; }
        public string OpponentName { get; set; } = string.Empty;
        public DateTime MatchDate { get; set; }

        /// <summary>„Heimspiel“, „Auswärtsspiel“ oder „–“.</summary>
        public string VenueLabel { get; set; } = "–";
        public bool? IsHomeGame { get; set; }

        /// <summary>Null, wenn für dieses Match kein Torhüter erfasst ist.</summary>
        public string? GoalkeeperName { get; set; }

        /// <summary>Kader ohne den Torhüter, alphabetisch.</summary>
        public List<string> FieldPlayers { get; set; } = new List<string>();

        public int FieldPlayerCount => FieldPlayers.Count;

        public int TotalCount => FieldPlayers.Count + (GoalkeeperName == null ? 0 : 1);

        /// <summary>Fertiger Text zum Kopieren, Zeile für Zeile wie auf der Kachel.</summary>
        public string ShareText
        {
            get
            {
                var venue = IsHomeGame.HasValue ? $" ({VenueLabel})" : string.Empty;

                return string.Join("\n", new[]
                {
                    $"Aufgebot {MatchDate:dd.MM.yyyy} {MatchDate:HH:mm} gegen {OpponentName}{venue}",
                    $"Torhüter: {GoalkeeperName ?? "noch offen"}",
                    $"Feldspieler ({FieldPlayerCount}): "
                        + (FieldPlayers.Count > 0 ? string.Join(", ", FieldPlayers) : "noch keine")
                });
            }
        }
    }

    /// <summary>Eine Zeile der Präsenzliste.</summary>
    public class PresenceEntry
    {
        public string PlayerName { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        /// <summary>Beendete Matches, in denen der Spieler im Kader stand.</summary>
        public int Appearances { get; set; }

        /// <summary>Davon im Tor.</summary>
        public int GoalkeeperAppearances { get; set; }
    }

    public class MatchTile
    {
        public int Id { get; set; }
        public string OpponentName { get; set; } = string.Empty;
        public DateTime MatchDate { get; set; }
        public int OurScore { get; set; }
        public int OpponentScore { get; set; }
        public bool IsFinished { get; set; }

        /// <summary>
        /// Zustand der Spieluhr, damit die Kachel nicht „läuft“ behauptet,
        /// solange das Spiel noch gar nicht angepfiffen ist.
        /// </summary>
        public bool HasStarted { get; set; }
        public bool IsPeriodRunning { get; set; }
        public string StatusLabel { get; set; } = string.Empty;

        public string ResultLabel => OurScore > OpponentScore ? "Sieg"
                                   : OurScore < OpponentScore ? "Niederlage"
                                   : "Unentschieden";
    }
}
