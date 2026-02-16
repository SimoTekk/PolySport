using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolySport.Data;
using PolySport.Models.ViewModels;

namespace PolySport.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Statistics/Season/5
        // Wenn man keine ID übergibt, laden wir automatisch die aktuell aktive Saison!
        public async Task<IActionResult> Season(int? id)
        {
            // 1. Saison finden
            var season = id.HasValue
                ? await _context.Seasons.FindAsync(id)
                : await _context.Seasons.FirstOrDefaultAsync(s => s.IsActive);

            if (season == null) return NotFound("Saison nicht gefunden oder noch keine Saison aktiv.");

            // 2. Alle Tore dieser Saison laden (inkl. Match und Spieler-Namen)
            var goalsInSeason = await _context.Goals
                .Include(g => g.Match)
                .Include(g => g.Scorer)
                .Include(g => g.Assist)
                .Where(g => g.Match!.SeasonId == season.Id)
                .ToListAsync();

            // 3. Strichliste führen (Dictionary)
            var playerStatsDict = new Dictionary<int, PlayerStatViewModel>();

            foreach (var goal in goalsInSeason)
            {
                // Hat der Spieler ein Tor gemacht?
                if (goal.ScorerId > 0 && goal.Scorer != null)
                {
                    if (!playerStatsDict.ContainsKey(goal.ScorerId))
                        playerStatsDict[goal.ScorerId] = new PlayerStatViewModel { PlayerName = goal.Scorer.Username };

                    playerStatsDict[goal.ScorerId].Goals++;
                }

                // Hat ein Spieler einen Assist gegeben?
                if (goal.AssistId.HasValue && goal.Assist != null)
                {
                    if (!playerStatsDict.ContainsKey(goal.AssistId.Value))
                        playerStatsDict[goal.AssistId.Value] = new PlayerStatViewModel { PlayerName = goal.Assist.Username };

                    playerStatsDict[goal.AssistId.Value].Assists++;
                }
            }

            // 4. Daten verpacken und sortieren
            var viewModel = new SeasonStatsViewModel
            {
                SeasonId = season.Id,
                SeasonName = season.Name,
                // Sortierung: 1. Nach Punkten absteigend, 2. bei Gleichstand nach Toren absteigend
                PlayerStats = playerStatsDict.Values
                    .OrderByDescending(p => p.TotalPoints)
                    .ThenByDescending(p => p.Goals)
                    .ToList()
            };

            return View(viewModel);
        }
    }
}