namespace PolySport.Models
{

    public class MatchPlayer
    {
         public int MatchId { get; set; }
         public Match? Match { get; set; }

         public int UserId { get; set; }
         public User? User { get; set; }

         /// <summary>
         /// Dieser Spieler stand in diesem Match im Tor. Pro Match ist höchstens
         /// einer so markiert; es bleibt am Kader hängen, weil bei Torhütermangel
         /// auch ein Feldspieler ins Tor geht und man später nachvollziehen will,
         /// wer wann gehütet hat.
         /// </summary>
         public bool IsGoalkeeper { get; set; }
    }

}
