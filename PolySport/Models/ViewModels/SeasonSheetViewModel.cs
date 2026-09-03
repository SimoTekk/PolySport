namespace PolySport.Models.ViewModels
{
    /// <summary>
    /// Saison-Abschlussblatt: Spieler in den Zeilen, Matches in den Spalten,
    /// pro Zelle Tore und Assists dieses Spielers in diesem Match.
    /// </summary>
    public class SeasonSheetViewModel
    {
        public int SeasonId { get; set; }
        public string SeasonName { get; set; } = string.Empty;

        /// <summary>Zur Auswahl einer anderen Saison.</summary>
        public List<SeasonOption> AvailableSeasons { get; set; } = new List<SeasonOption>();

        /// <summary>Chronologisch, älteste zuerst – von links nach rechts.</summary>
        public List<SheetMatchColumn> Matches { get; set; } = new List<SheetMatchColumn>();

        public List<SheetPlayerRow> Players { get; set; } = new List<SheetPlayerRow>();

        public int TeamGoals => Players.Sum(p => p.TotalGoals);
        public int TeamAssists => Players.Sum(p => p.TotalAssists);
    }

    public class SeasonOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class SheetMatchColumn
    {
        public int MatchId { get; set; }
        public DateTime MatchDate { get; set; }
        public string OpponentName { get; set; } = string.Empty;
        public bool IsFinished { get; set; }
        public int OurScore { get; set; }
        public int OpponentScore { get; set; }

        public string DateLabel => MatchDate.ToString("dd.MM.yy");
        public string ScoreLabel => $"{OurScore}:{OpponentScore}";
    }

    public class SheetPlayerRow
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        /// <summary>Eine Zelle pro Match, gleiche Reihenfolge wie Matches.</summary>
        public List<SheetCell> Cells { get; set; } = new List<SheetCell>();

        public int TotalGoals => Cells.Sum(c => c.Goals);
        public int TotalAssists => Cells.Sum(c => c.Assists);
        public int TotalPoints => TotalGoals + TotalAssists;

        /// <summary>
        /// Einsätze: nur beendete Matches werden gezählt. Ein Kadereintrag für
        /// ein noch nicht gespieltes Match ist eine Planung, kein Einsatz –
        /// sonst hätte am Saisonanfang jeder so viele Einsätze wie Termine.
        /// </summary>
        public int MatchesPlayed => Cells.Count(c => c.CountsAsAppearance);

        /// <summary>Von diesen Einsätzen im Tor – siehe SheetCell.WasGoalkeeper.</summary>
        public int GoalkeeperMatches => Cells.Count(c => c.CountsAsAppearance && c.WasGoalkeeper);
    }

    public class SheetCell
    {
        /// <summary>
        /// Stand der Spieler im Kader dieses Matches? Falls nicht, wird die
        /// Zelle leer dargestellt – "nicht dabei" ist etwas anderes als "0 Punkte".
        /// </summary>
        public bool WasInRoster { get; set; }

        /// <summary>Match beendet – erst dann zählt der Kadereintrag als Einsatz.</summary>
        public bool MatchIsFinished { get; set; }

        /// <summary>
        /// Dieser Spieler stand in diesem Match im Tor. Damit lässt sich
        /// nachvollziehen, wer bei Torhütermangel eingesprungen ist.
        /// </summary>
        public bool WasGoalkeeper { get; set; }

        /// <summary>Im Kader und Match beendet.</summary>
        public bool CountsAsAppearance => WasInRoster && MatchIsFinished;

        public int Goals { get; set; }
        public int Assists { get; set; }
    }
}
