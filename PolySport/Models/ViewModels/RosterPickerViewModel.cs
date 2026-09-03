namespace PolySport.Models.ViewModels
{
    /// <summary>
    /// Kaderauswahl als Ankreuzliste. Wird von „Match anlegen“ und
    /// „Match bearbeiten“ geteilt; die Feldnamen im Formular entsprechen
    /// darum den Eigenschaften beider Formular-Modelle.
    /// </summary>
    public class RosterPickerViewModel
    {
        /// <summary>Alle anwählbaren Spieler, alphabetisch.</summary>
        public List<RosterPlayerOption> Players { get; set; } = new List<RosterPlayerOption>();

        /// <summary>Wer bereits angehakt ist.</summary>
        public List<int> SelectedPlayerIds { get; set; } = new List<int>();

        /// <summary>Wer im Tor steht. Null = keine Angabe.</summary>
        public int? GoalkeeperId { get; set; }
    }

    public class RosterPlayerOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Inaktive Spieler erscheinen nur, wenn sie schon im Kader stehen –
        /// sie werden gekennzeichnet, damit die Auswahl nachvollziehbar bleibt.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
