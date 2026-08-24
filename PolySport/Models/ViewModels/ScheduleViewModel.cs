namespace PolySport.Models.ViewModels
{
    /// <summary>
    /// Spielplan einer Saison: kommende Termine zuerst, gespielte darunter.
    /// Bewusst eine Liste und kein Monatsgitter – auf dem Handy ist das
    /// lesbar, und Resultat wie Status stehen gleich daneben.
    /// </summary>
    public class ScheduleViewModel
    {
        public int SeasonId { get; set; }
        public string SeasonName { get; set; } = string.Empty;

        public List<SeasonOption> AvailableSeasons { get; set; } = new List<SeasonOption>();

        /// <summary>Noch nicht beendet, aufsteigend – das nächste Spiel oben.</summary>
        public List<ScheduleEntry> Upcoming { get; set; } = new List<ScheduleEntry>();

        /// <summary>Beendet, absteigend – das jüngste Resultat oben.</summary>
        public List<ScheduleEntry> Played { get; set; } = new List<ScheduleEntry>();

        public int HomeGames { get; set; }
        public int AwayGames { get; set; }

        /// <summary>Matches ohne Angabe, ob heim oder auswärts.</summary>
        public int VenueUnknown { get; set; }

        public int MatchesTotal => Upcoming.Count + Played.Count;
    }

    public class ScheduleEntry
    {
        // Monats- und Tagesnamen stehen fest im Code: die Oberfläche ist
        // deutsch, und so hängt die Anzeige nicht davon ab, welche Kultur
        // auf dem Server eingestellt ist.
        private static readonly string[] MonthNames =
        {
            "Januar", "Februar", "März", "April", "Mai", "Juni",
            "Juli", "August", "September", "Oktober", "November", "Dezember"
        };

        private static readonly string[] DayNames = { "So", "Mo", "Di", "Mi", "Do", "Fr", "Sa" };

        public int Id { get; set; }
        public DateTime MatchDate { get; set; }
        public string OpponentName { get; set; } = string.Empty;

        public bool? IsHomeGame { get; set; }
        public string VenueLabel { get; set; } = "–";
        public string VenueShort { get; set; } = "–";

        public bool IsFinished { get; set; }
        public bool HasStarted { get; set; }
        public bool IsPeriodRunning { get; set; }
        public string StatusLabel { get; set; } = string.Empty;

        public int OurScore { get; set; }
        public int OpponentScore { get; set; }

        public string ResultLabel => OurScore > OpponentScore ? "Sieg"
                                   : OurScore < OpponentScore ? "Niederlage"
                                   : "Unentschieden";

        /// <summary>Überschrift der Monatsgruppe, z.B. "September 2026".</summary>
        public string MonthLabel => $"{MonthNames[MatchDate.Month - 1]} {MatchDate.Year}";

        /// <summary>Wochentag in zwei Buchstaben, z.B. "Sa".</summary>
        public string DayLabel => DayNames[(int)MatchDate.DayOfWeek];
    }
}
