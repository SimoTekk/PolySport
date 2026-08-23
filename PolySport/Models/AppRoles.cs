namespace PolySport.Models
{
    /// <summary>
    /// Rollennamen zentral, damit sie in Attributen und Views nicht getippt werden müssen.
    /// </summary>
    public static class AppRoles
    {
        /// <summary>Darf alles: Matches, Saisons, Spieler und Benutzer verwalten.</summary>
        public const string Admin = "Admin";

        /// <summary>
        /// Leitet Spiele: Spieluhr bedienen, Tore erfassen und korrigieren,
        /// Match beenden. Darf keine Matches anlegen und nichts verwalten.
        /// </summary>
        public const string Manager = "Manager";

        /// <summary>Für Aktionen, die beide Rollen ausführen dürfen.</summary>
        public const string AdminOrManager = Admin + "," + Manager;

        /// <summary>Alle Rollen, die beim Start vorhanden sein müssen.</summary>
        public static readonly string[] All = { Admin, Manager };
    }
}
