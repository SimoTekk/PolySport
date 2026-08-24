using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolySport.Data;
using PolySport.Models;
using PolySport.Models.ViewModels;

namespace PolySport.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Das Dashboard ist offen: die Kennzahlen der aktiven Saison sieht
            // jeder Besucher. Die Ansicht lässt für Nichtangemeldete nur die
            // Schaltflächen weg, die auf geschützte Seiten führen.
            var season = await _context.Seasons.FirstOrDefaultAsync(s => s.IsActive);

            var viewModel = new DashboardViewModel
            {
                ActivePlayers = await _context.Players.CountAsync(p => p.IsActive)
            };

            if (User.IsInRole(AppRoles.Admin))
                viewModel.PendingApprovals = await _userManager.Users.CountAsync(u => !u.IsApproved);

            if (season == null)
                return View(viewModel);

            viewModel.HasActiveSeason = true;
            viewModel.SeasonName = season.Name;

            var matches = await _context.Matches
                .Include(m => m.Goals)
                .Where(m => m.SeasonId == season.Id)
                .ToListAsync();

            viewModel.MatchesTotal = matches.Count;
            viewModel.MatchesFinished = matches.Count(m => m.IsFinished);
            viewModel.MatchesOpen = matches.Count(m => !m.IsFinished);

            // Bilanz und Torverhältnis nur aus beendeten Spielen – laufende
            // Spiele hätten sonst ein unfertiges Resultat in der Statistik.
            foreach (var match in matches.Where(m => m.IsFinished))
            {
                viewModel.GoalsFor += match.OurScore;
                viewModel.GoalsAgainst += match.OpponentScore;

                if (match.OurScore > match.OpponentScore) viewModel.Wins++;
                else if (match.OurScore < match.OpponentScore) viewModel.Losses++;
                else viewModel.Draws++;
            }

            var lastFinished = matches
                .Where(m => m.IsFinished)
                .OrderByDescending(m => m.MatchDate)
                .FirstOrDefault();

            if (lastFinished != null)
                viewModel.LastMatch = ToTile(lastFinished);

            // Das nächste offene Spiel: bevorzugt das zeitlich nächstliegende
            var openMatch = matches
                .Where(m => !m.IsFinished)
                .OrderBy(m => m.MatchDate)
                .FirstOrDefault();

            if (openMatch != null)
                viewModel.OpenMatch = ToTile(openMatch);

            viewModel.TopScorers = await BuildTopScorersAsync(season.Id);

            return View(viewModel);
        }

        private static MatchTile ToTile(Match match) => new MatchTile
        {
            Id = match.Id,
            OpponentName = match.OpponentName,
            MatchDate = match.MatchDate,
            OurScore = match.OurScore,
            OpponentScore = match.OpponentScore,
            IsFinished = match.IsFinished,
            HasStarted = match.HasStarted,
            IsPeriodRunning = match.IsPeriodRunning,
            StatusLabel = match.StatusLabel
        };

        private async Task<List<PlayerStatViewModel>> BuildTopScorersAsync(int seasonId)
        {
            // Gegentore haben keinen Schützen und fallen hier automatisch weg
            var goals = await _context.Goals
                .Include(g => g.Scorer)
                .Include(g => g.Assist)
                .Where(g => g.Match!.SeasonId == seasonId && !g.IsOpponentGoal)
                .ToListAsync();

            var stats = new Dictionary<int, PlayerStatViewModel>();

            foreach (var goal in goals)
            {
                if (goal.ScorerId.HasValue && goal.Scorer != null)
                {
                    if (!stats.ContainsKey(goal.ScorerId.Value))
                        stats[goal.ScorerId.Value] = new PlayerStatViewModel { PlayerName = goal.Scorer.Username };

                    stats[goal.ScorerId.Value].Goals++;
                }

                if (goal.AssistId.HasValue && goal.Assist != null)
                {
                    if (!stats.ContainsKey(goal.AssistId.Value))
                        stats[goal.AssistId.Value] = new PlayerStatViewModel { PlayerName = goal.Assist.Username };

                    stats[goal.AssistId.Value].Assists++;
                }
            }

            return stats.Values
                .OrderByDescending(p => p.TotalPoints)
                .ThenByDescending(p => p.Goals)
                .Take(3)
                .ToList();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
