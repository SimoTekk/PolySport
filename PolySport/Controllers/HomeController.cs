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

            viewModel.Scorers = await BuildScorersAsync(season.Id);

            // Kader aller Matches der Saison: daraus entstehen das Aufgebot des
            // nächsten Spiels und die Präsenzliste.
            var matchIds = matches.Select(m => m.Id).ToList();

            var rosterEntries = await _context.MatchPlayers
                .Where(mp => matchIds.Contains(mp.MatchId))
                .Select(mp => new RosterRow
                {
                    MatchId = mp.MatchId,
                    PlayerId = mp.UserId,
                    PlayerName = mp.User!.Username,
                    IsGoalkeeper = mp.IsGoalkeeper
                })
                .ToListAsync();

            if (openMatch != null)
                viewModel.Lineup = BuildLineup(openMatch, rosterEntries);

            viewModel.Presence = await BuildPresenceAsync(matches, rosterEntries);

            return View(viewModel);
        }

        /// <summary>Ein Kadereintrag samt Spielername, für Aufgebot und Präsenzliste.</summary>
        private sealed class RosterRow
        {
            public int MatchId { get; set; }
            public int PlayerId { get; set; }
            public string PlayerName { get; set; } = string.Empty;
            public bool IsGoalkeeper { get; set; }
        }

        /// <summary>
        /// Aufgebot des nächsten offenen Matches: Torhüter getrennt, Feldspieler
        /// alphabetisch. Ohne erfassten Kader bleibt beides leer – die Kachel
        /// sagt das dann selbst.
        /// </summary>
        private static LineupViewModel BuildLineup(Match match, List<RosterRow> rosterEntries)
        {
            var entries = rosterEntries.Where(r => r.MatchId == match.Id).ToList();

            return new LineupViewModel
            {
                MatchId = match.Id,
                OpponentName = match.OpponentName,
                MatchDate = match.MatchDate,
                VenueLabel = match.VenueLabel,
                IsHomeGame = match.IsHomeGame,
                GoalkeeperName = entries.FirstOrDefault(r => r.IsGoalkeeper)?.PlayerName,
                FieldPlayers = entries
                    .Where(r => !r.IsGoalkeeper)
                    .Select(r => r.PlayerName)
                    .OrderBy(n => n)
                    .ToList()
            };
        }

        /// <summary>
        /// Präsenzliste: alle aktiven Spieler und zusätzlich alle, die in dieser
        /// Saison im Kader standen. Als Einsatz zählt nur ein beendetes Match –
        /// sonst hätte am Saisonanfang jeder so viele Einsätze wie Termine.
        /// </summary>
        private async Task<List<PresenceEntry>> BuildPresenceAsync(List<Match> matches, List<RosterRow> rosterEntries)
        {
            var finishedIds = matches.Where(m => m.IsFinished).Select(m => m.Id).ToHashSet();
            var counted = rosterEntries.Where(r => finishedIds.Contains(r.MatchId)).ToList();

            var appearances = counted
                .GroupBy(r => r.PlayerId)
                .ToDictionary(g => g.Key, g => g.Count());

            var inGoal = counted
                .Where(r => r.IsGoalkeeper)
                .GroupBy(r => r.PlayerId)
                .ToDictionary(g => g.Key, g => g.Count());

            var involvedIds = rosterEntries.Select(r => r.PlayerId).Distinct().ToList();

            var players = await _context.Players
                .Where(p => p.IsActive || involvedIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Username, p.IsActive })
                .ToListAsync();

            return players
                .Select(p => new PresenceEntry
                {
                    PlayerName = p.Username,
                    IsActive = p.IsActive,
                    Appearances = appearances.TryGetValue(p.Id, out var count) ? count : 0,
                    GoalkeeperAppearances = inGoal.TryGetValue(p.Id, out var games) ? games : 0
                })
                .OrderByDescending(e => e.Appearances)
                .ThenBy(e => e.PlayerName)
                .ToList();
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

        private async Task<List<PlayerStatViewModel>> BuildScorersAsync(int seasonId)
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
