namespace PolySport.Models.ViewModels
{
    // Die Hauptklasse für die Seite
    public class SeasonStatsViewModel
    {
        public int SeasonId { get; set; }
        public string SeasonName { get; set; } = string.Empty;

        // Liste aller Spieler-Statistiken in dieser Saison
        public List<PlayerStatViewModel> PlayerStats { get; set; } = new List<PlayerStatViewModel>();

        /// <summary>Auswertung: wann fallen die Tore? Ein Eintrag pro Drittel.</summary>
        public List<PeriodStatViewModel> PeriodStats { get; set; } = new List<PeriodStatViewModel>();

        /// <summary>Eigene Tore ohne Zeitangabe (Altdaten) – zählen im Total mit.</summary>
        public int GoalsWithoutTime { get; set; }

        /// <summary>Gegentore ohne Zeitangabe – zählen im Total ebenfalls mit.</summary>
        public int GoalsAgainstWithoutTime { get; set; }

        public int TotalGoalsFor => PeriodStats.Sum(p => p.GoalsFor) + GoalsWithoutTime;
        public int TotalGoalsAgainst => PeriodStats.Sum(p => p.GoalsAgainst) + GoalsAgainstWithoutTime;

        /// <summary>Grösster Einzelwert – für die Balkenbreite in der Ansicht.</summary>
        public int PeriodPeak
        {
            get
            {
                if (!PeriodStats.Any()) return 0;
                return Math.Max(PeriodStats.Max(p => p.GoalsFor), PeriodStats.Max(p => p.GoalsAgainst));
            }
        }
    }

    // Hilfsklasse für eine einzelne Tabellen-Zeile (Ein Spieler)
    public class PlayerStatViewModel
    {
        public string PlayerName { get; set; } = string.Empty;
        public int Goals { get; set; }
        public int Assists { get; set; }

        // C# rechnet die Gesamtpunkte automatisch aus!
        public int TotalPoints => Goals + Assists;
    }

    public class PeriodStatViewModel
    {
        public int Period { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }

        public string Label => $"{Period}. Drittel";
    }
}
