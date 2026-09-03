using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolySport.Data;
using PolySport.Models.ViewModels;

namespace PolySport.Controllers
{
    [Authorize]
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

            // 3. Strichliste führen (Dictionary) – nur eigene Tore haben Schützen
            var playerStatsDict = new Dictionary<int, PlayerStatViewModel>();

            foreach (var goal in goalsInSeason.Where(g => !g.IsOpponentGoal))
            {
                // Hat der Spieler ein Tor gemacht?
                if (goal.ScorerId.HasValue && goal.Scorer != null)
                {
                    if (!playerStatsDict.ContainsKey(goal.ScorerId.Value))
                        playerStatsDict[goal.ScorerId.Value] = new PlayerStatViewModel { PlayerName = goal.Scorer.Username };

                    playerStatsDict[goal.ScorerId.Value].Goals++;
                }

                // Hat ein Spieler einen Assist gegeben?
                if (goal.AssistId.HasValue && goal.Assist != null)
                {
                    if (!playerStatsDict.ContainsKey(goal.AssistId.Value))
                        playerStatsDict[goal.AssistId.Value] = new PlayerStatViewModel { PlayerName = goal.Assist.Username };

                    playerStatsDict[goal.AssistId.Value].Assists++;
                }
            }

            // 4. Auswertung nach Drittel: wann fallen die Tore?
            var periodStats = Enumerable.Range(1, 3)
                .Select(period => new PeriodStatViewModel
                {
                    Period = period,
                    GoalsFor = goalsInSeason.Count(g => !g.IsOpponentGoal && g.Period == period),
                    GoalsAgainst = goalsInSeason.Count(g => g.IsOpponentGoal && g.Period == period)
                })
                .ToList();

            // 5. Daten verpacken und sortieren
            var viewModel = new SeasonStatsViewModel
            {
                SeasonId = season.Id,
                SeasonName = season.Name,
                // Sortierung: 1. Nach Punkten absteigend, 2. bei Gleichstand nach Toren absteigend
                PlayerStats = playerStatsDict.Values
                    .OrderByDescending(p => p.TotalPoints)
                    .ThenByDescending(p => p.Goals)
                    .ToList(),
                PeriodStats = periodStats,
                GoalsWithoutTime = goalsInSeason.Count(g => !g.IsOpponentGoal && !g.Period.HasValue),
                GoalsAgainstWithoutTime = goalsInSeason.Count(g => g.IsOpponentGoal && !g.Period.HasValue)
            };

            return View(viewModel);
        }

        // GET: Statistics/Sheet/5 – Abschlussblatt der Saison als Matrix
        public async Task<IActionResult> Sheet(int? id)
        {
            var season = id.HasValue
                ? await _context.Seasons.FindAsync(id)
                : await _context.Seasons.FirstOrDefaultAsync(s => s.IsActive);

            if (season == null) return NotFound("Saison nicht gefunden oder noch keine Saison aktiv.");

            var viewModel = new SeasonSheetViewModel
            {
                SeasonId = season.Id,
                SeasonName = season.Name,
                AvailableSeasons = await _context.Seasons
                    .OrderByDescending(s => s.IsActive)
                    .ThenBy(s => s.Name)
                    .Select(s => new SeasonOption { Id = s.Id, Name = s.Name, IsActive = s.IsActive })
                    .ToListAsync()
            };

            // Matches chronologisch – sie werden die Spalten
            var matches = await _context.Matches
                .Include(m => m.Goals)
                .Where(m => m.SeasonId == season.Id)
                .OrderBy(m => m.MatchDate)
                .ThenBy(m => m.Id)
                .ToListAsync();

            if (!matches.Any()) return View(viewModel);

            var matchIds = matches.Select(m => m.Id).ToList();

            var rosterEntries = await _context.MatchPlayers
                .Where(mp => matchIds.Contains(mp.MatchId))
                .Select(mp => new { mp.MatchId, mp.UserId, mp.IsGoalkeeper })
                .ToListAsync();

            var allGoals = matches.SelectMany(m => m.Goals).Where(g => !g.IsOpponentGoal).ToList();

            // Jeder, der im Kader stand oder gepunktet hat, kommt ins Blatt –
            // auch inaktive Spieler, damit die Saison vollständig bleibt.
            var playerIds = rosterEntries.Select(r => r.UserId)
                .Concat(allGoals.Where(g => g.ScorerId.HasValue).Select(g => g.ScorerId!.Value))
                .Concat(allGoals.Where(g => g.AssistId.HasValue).Select(g => g.AssistId!.Value))
                .Distinct()
                .ToList();

            var players = await _context.Players
                .Where(p => playerIds.Contains(p.Id))
                .ToListAsync();

            // Nachschlagewerke, damit die Matrix ohne verschachtelte Suchen entsteht
            var rosterLookup = rosterEntries
                .Select(r => (r.MatchId, r.UserId))
                .ToHashSet();

            var goalieLookup = rosterEntries
                .Where(r => r.IsGoalkeeper)
                .Select(r => (r.MatchId, r.UserId))
                .ToHashSet();

            var goalsPerPlayerMatch = allGoals
                .Where(g => g.ScorerId.HasValue)
                .GroupBy(g => (g.MatchId, PlayerId: g.ScorerId!.Value))
                .ToDictionary(g => g.Key, g => g.Count());

            var assistsPerPlayerMatch = allGoals
                .Where(g => g.AssistId.HasValue)
                .GroupBy(g => (g.MatchId, PlayerId: g.AssistId!.Value))
                .ToDictionary(g => g.Key, g => g.Count());

            viewModel.Matches = matches.Select(m => new SheetMatchColumn
            {
                MatchId = m.Id,
                MatchDate = m.MatchDate,
                OpponentName = m.OpponentName,
                IsFinished = m.IsFinished,
                OurScore = m.OurScore,
                OpponentScore = m.OpponentScore
            }).ToList();

            viewModel.Players = players.Select(player => new SheetPlayerRow
            {
                PlayerId = player.Id,
                PlayerName = player.Username,
                IsActive = player.IsActive,
                Cells = matches.Select(m => new SheetCell
                {
                    WasInRoster = rosterLookup.Contains((m.Id, player.Id)),
                    MatchIsFinished = m.IsFinished,
                    WasGoalkeeper = goalieLookup.Contains((m.Id, player.Id)),
                    Goals = goalsPerPlayerMatch.TryGetValue((m.Id, player.Id), out var g) ? g : 0,
                    Assists = assistsPerPlayerMatch.TryGetValue((m.Id, player.Id), out var a) ? a : 0
                }).ToList()
            })
            .OrderByDescending(p => p.TotalPoints)
            .ThenByDescending(p => p.TotalGoals)
            .ThenBy(p => p.PlayerName)
            .ToList();

            return View(viewModel);
        }
    }
}
