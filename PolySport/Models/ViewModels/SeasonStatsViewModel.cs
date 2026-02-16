namespace PolySport.Models.ViewModels
{
    // Die Hauptklasse für die Seite
    public class SeasonStatsViewModel
    {
        public int SeasonId { get; set; }
        public string SeasonName { get; set; } = string.Empty;

        // Liste aller Spieler-Statistiken in dieser Saison
        public List<PlayerStatViewModel> PlayerStats { get; set; } = new List<PlayerStatViewModel>();
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
}