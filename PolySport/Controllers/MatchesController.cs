using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PolySport.Data;
using PolySport.Models;
using PolySport.Models.ViewModels;

namespace PolySport.Controllers
{
    // Lesen: jedes angemeldete Mitglied. Schreiben: nur Admin (siehe Attribute unten).
    [Authorize]
    public class MatchesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MatchesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Matches/Create (Lädt das leere Formular)
        [Authorize(Roles = AppRoles.Admin)]
        public IActionResult Create()
        {
            var viewModel = new CreateMatchViewModel
            {
                // Lade NUR aktive Saisons für das Dropdown
                AvailableSeasons = _context.Seasons
                    .Where(s => s.IsActive)
                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                    .ToList(),

                // Lade NUR aktive Spieler (Soft-Delete) für die Kader-Auswahl
                AvailablePlayers = _context.Players
                    .Where(u => u.IsActive)
                    .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Username })
                    .ToList()
            };

            return View(viewModel);
        }

        // POST: Matches/Create (Wird beim Klick auf Speichern aufgerufen)
        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMatchViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // 1. Das eigentliche Match-Objekt erstellen
                var match = new Match
                {
                    SeasonId = viewModel.SeasonId,
                    OpponentName = viewModel.OpponentName,
                    MatchDate = viewModel.MatchDate
                };

                _context.Matches.Add(match);

                // Wir speichern das Match zuerst, damit der SQL Server eine Match.Id generiert!
                await _context.SaveChangesAsync();

                // 2. Jetzt die ausgewählten Spieler als Kader (MatchPlayer) verknüpfen
                if (viewModel.SelectedPlayerIds != null && viewModel.SelectedPlayerIds.Any())
                {
                    foreach (var playerId in viewModel.SelectedPlayerIds)
                    {
                        var matchPlayer = new MatchPlayer
                        {
                            MatchId = match.Id,
                            UserId = playerId
                        };
                        _context.MatchPlayers.Add(matchPlayer);
                    }
                    await _context.SaveChangesAsync(); // Kader speichern
                }

                // Direkt zur Detailseite, dort werden die Tore erfasst
                return RedirectToAction(nameof(Details), new { id = match.Id });
            }

            // Falls jemand das Formular falsch ausfüllt (z.B. Gegner vergessen):
            // Dropdowns neu laden, bevor die Seite mit Fehlermeldung neu angezeigt wird
            viewModel.AvailableSeasons = _context.Seasons.Where(s => s.IsActive).Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name });
            viewModel.AvailablePlayers = _context.Players.Where(u => u.IsActive).Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Username });

            return View(viewModel);
        }

        // GET: Matches/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Lade das Match inklusive aller verknüpften Daten aus der Datenbank
            var match = await _context.Matches
                .Include(m => m.Season)
                .Include(m => m.MatchPlayers)
                    .ThenInclude(mp => mp.User) // Lade die User-Daten des Kaders
                .Include(m => m.Goals)
                    .ThenInclude(g => g.Scorer) // Lade den Namen des Torschützen
                .Include(m => m.Goals)
                    .ThenInclude(g => g.Assist) // Lade den Namen des Assist-Gebers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound();

            var viewModel = new MatchDetailsViewModel
            {
                MatchId = match.Id,
                SeasonName = match.Season?.Name ?? "Unbekannte Saison",
                OpponentName = match.OpponentName,
                MatchDate = match.MatchDate,
                OurScore = match.OurScore,
                OpponentScore = match.OpponentScore,
                IsFinished = match.IsFinished,
                FinishedAt = match.FinishedAt,

                CurrentPeriod = match.CurrentPeriod,
                HasStarted = match.HasStarted,
                IsPeriodRunning = match.IsPeriodRunning,
                IsInBreak = match.IsInBreak,
                CanStartNextPeriod = match.CanStartNextPeriod,
                NextPeriod = match.NextPeriod,
                ElapsedSecondsInPeriod = match.ElapsedSecondsInPeriod,
                StatusLabel = match.StatusLabel,

                // Liste der Spielernamen im Kader
                RosterNames = match.MatchPlayers.Select(mp => mp.User!.Username).ToList(),

                Timeline = BuildTimeline(match.Goals)
            };

            return View(viewModel);
        }

        /// <summary>
        /// Tore chronologisch sortieren und den Zwischenstand mitrechnen.
        /// Tore ohne Zeitangabe (Altdaten) landen am Ende, damit der letzte
        /// Zwischenstand trotzdem dem Endresultat entspricht.
        /// </summary>
        private static List<GoalDisplay> BuildTimeline(IEnumerable<Goal> goals)
        {
            var ordered = goals
                .OrderBy(g => g.SortKey)
                .ThenBy(g => g.Id)
                .ToList();

            var timeline = new List<GoalDisplay>();
            var ours = 0;
            var theirs = 0;

            foreach (var goal in ordered)
            {
                if (goal.IsOpponentGoal) theirs++; else ours++;

                timeline.Add(new GoalDisplay
                {
                    GoalId = goal.Id,
                    IsOpponentGoal = goal.IsOpponentGoal,
                    Period = goal.Period,
                    SecondsInPeriod = goal.SecondsInPeriod,
                    ScorerName = goal.Scorer?.Username,
                    AssistName = goal.Assist?.Username,
                    ScoreOurs = ours,
                    ScoreOpponent = theirs
                });
            }

            return timeline;
        }

        // POST: Matches/StartPeriod/5 – nächstes Drittel starten, Uhr läuft
        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartPeriod(int id)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null) return NotFound();

            if (match.IsFinished)
            {
                TempData["Error"] = "Das Match ist beendet. Öffne es wieder, um weiterzuspielen.";
            }
            else if (match.PeriodStartedAt.HasValue)
            {
                TempData["Error"] = "Die Uhr läuft bereits.";
            }
            else if (match.CurrentPeriod >= 3)
            {
                TempData["Error"] = "Alle drei Drittel sind gespielt. Beende das Match.";
            }
            else
            {
                match.CurrentPeriod += 1;
                match.PeriodStartedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"{match.CurrentPeriod}. Drittel läuft.";
            }

            return RedirectToAction(nameof(Details), new { id = match.Id });
        }

        // POST: Matches/EndPeriod/5 – Uhr anhalten, sie wartet auf das nächste Drittel
        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndPeriod(int id)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null) return NotFound();

            if (!match.PeriodStartedAt.HasValue)
            {
                TempData["Error"] = "Die Uhr läuft gerade nicht.";
            }
            else
            {
                var period = match.CurrentPeriod;
                match.PeriodStartedAt = null;
                await _context.SaveChangesAsync();

                TempData["Success"] = period >= 3
                    ? "3. Drittel beendet. Du kannst das Match jetzt abschliessen."
                    : $"{period}. Drittel beendet. Die Uhr wartet auf das {period + 1}. Drittel.";
            }

            return RedirectToAction(nameof(Details), new { id = match.Id });
        }

        // POST: Matches/Finish/5 – Spiel beenden, danach sind keine Tore mehr erfassbar
        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finish(int id)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null) return NotFound();

            if (!match.IsFinished)
            {
                match.IsFinished = true;
                match.FinishedAt = DateTime.UtcNow;
                match.PeriodStartedAt = null; // Uhr anhalten
                await _context.SaveChangesAsync();
                TempData["Success"] = "Match beendet. Das Resultat zählt jetzt für die Bilanz.";
            }

            return RedirectToAction(nameof(Details), new { id = match.Id });
        }

        // POST: Matches/Reopen/5 – falls noch etwas nachgetragen werden muss
        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrManager)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(int id)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null) return NotFound();

            if (match.IsFinished)
            {
                match.IsFinished = false;
                match.FinishedAt = null;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Match wieder geöffnet – Tore können nachgetragen werden.";
            }

            return RedirectToAction(nameof(Details), new { id = match.Id });
        }

        // GET: Matches/Index (Die Übersicht aller Spiele)
        public async Task<IActionResult> Index()
        {
            // Tore mitladen, damit der Spielstand pro Zeile berechnet werden kann
            var matches = await _context.Matches
                .Include(m => m.Season)
                .Include(m => m.Goals)
                .OrderByDescending(m => m.MatchDate) // Neueste Spiele zuerst
                .ToListAsync();

            return View(matches);
        }
    }
}
