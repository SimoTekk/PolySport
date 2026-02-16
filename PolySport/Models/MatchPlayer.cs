namespace PolySport.Models
{
    
    public class MatchPlayer
    {
         public int MatchId { get; set; }
         public Match? Match { get; set; }

         public int UserId { get; set; }
         public User? User { get; set; }
    }
    
}