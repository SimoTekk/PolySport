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

        // GET: Matches/Edit/5 – Stammdaten ändern (Saison, Gegner, Datum, Kader)
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var match = await _context.Matches
                .Include(m => m.Goals)
                .Include(m => m.MatchPlayers)
                    .ThenInclude(mp => mp.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound();

            var rosterIds = match.MatchPlayers.Select(mp => mp.UserId).ToList();

            return View(new EditMatchViewModel
            {
                Id = match.Id,
                SeasonId = match.SeasonId,
                OpponentName = match.OpponentName,
                MatchDate = match.MatchDate,
                GoalCount = match.Goals.Count,
                SelectedPlayerIds = rosterIds,
                CanEditRoster = !match.HasStarted,
                RosterNames = match.MatchPlayers
                    .Select(mp => mp.User!.Username)
                    .OrderBy(n => n)
                    .ToList(),
                AvailableSeasons = await LoadSeasonsAsync(),
                AvailablePlayers = await LoadPlayersAsync(rosterIds)
            });
        }

        // POST: Matches/Edit/5
        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditMatchViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            var match = await _context.Matches
                .Include(m => m.MatchPlayers)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound();

            // Sobald die Uhr lief, bleibt der Kader unverändert: daran hängen
            // Einsätze und Tore. Das Formular zeigt ihn dann nur noch an, und
            // hier wird eine trotzdem mitgeschickte Auswahl ignoriert.
            var canEditRoster = !match.HasStarted;

            if (canEditRoster)
                await ValidateRosterAsync(match, viewModel.SelectedPlayerIds);

            if (ModelState.IsValid)
            {
                match.SeasonId = viewModel.SeasonId;
                match.OpponentName = viewModel.OpponentName;
                match.MatchDate = viewModel.MatchDate;

                if (canEditRoster)
                    ApplyRoster(match, viewModel.SelectedPlayerIds);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Die Angaben zum Match wurden gespeichert.";
                return RedirectToAction(nameof(Details), new { id = match.Id });
            }

            viewModel.GoalCount = await _context.Goals.CountAsync(g => g.MatchId == id);
            viewModel.CanEditRoster = canEditRoster;
            viewModel.RosterNames = await _context.MatchPlayers
                .Where(mp => mp.MatchId == id)
                .Select(mp => mp.User!.Username)
                .OrderBy(n => n)
                .ToListAsync();
            viewModel.AvailableSeasons = await LoadSeasonsAsync();
            // Auch den bisherigen Kader mitgeben: sonst fehlt ein abgewählter
            // inaktiver Spieler in der Liste und liesse sich nicht zurückholen.
            viewModel.AvailablePlayers = await LoadPlayersAsync(
                (viewModel.SelectedPlayerIds ?? new List<int>())
                    .Concat(match.MatchPlayers.Select(mp => mp.UserId)));
            return View(viewModel);
        }

        /// <summary>
        /// Prüft eine neue Kader-Auswahl. Das Formular bietet nur passende
        /// Spieler an, aber ein manipuliertes Formular könnte jede Id senden.
        /// Und wer in diesem Match ein Tor oder einen Assist hat, darf nicht
        /// herausfallen – sonst stünde in der Torfolge jemand, der laut Kader
        /// nicht dabei war.
        /// </summary>
        private async Task ValidateRosterAsync(Match match, List<int>? selectedPlayerIds)
        {
            var selected = (selectedPlayerIds ?? new List<int>()).Distinct().ToList();
            var current = match.MatchPlayers.Select(mp => mp.UserId).ToList();

            var allowed = await _context.Players
                .Where(p => p.IsActive)
                .Select(p => p.Id)
                .ToListAsync();
            allowed.AddRange(current);

            if (selected.Any(pid => !allowed.Contains(pid)))
            {
                ModelState.AddModelError(nameof(EditMatchViewModel.SelectedPlayerIds),
                    "Mindestens einer der gewählten Spieler existiert nicht oder ist nicht aktiv.");
                return;
            }

            var removed = current.Where(pid => !selected.Contains(pid)).ToList();
            if (removed.Count == 0) return;

            // Wenige Tore pro Match, darum bequem im Speicher vergleichen.
            var involved = await _context.Goals
                .Where(g => g.MatchId == match.Id)
                .Select(g => new { g.ScorerId, g.AssistId })
                .ToListAsync();

            var blocked = removed
                .Where(pid => involved.Any(g => g.ScorerId == pid || g.AssistId == pid))
                .ToList();

            if (blocked.Count == 0) return;

            var names = await _context.Players
                .Where(p => blocked.Contains(p.Id))
                .OrderBy(p => p.Username)
                .Select(p => p.Username)
                .ToListAsync();

            ModelState.AddModelError(nameof(EditMatchViewModel.SelectedPlayerIds),
                "Aus dem Kader entfernen geht nicht, solange ein Tor oder Assist daran hängt: "
                + string.Join(", ", names)
                + ". Lösche das Tor zuerst auf der Detailseite.");
        }

        /// <summary>Kader auf die Auswahl bringen: Weggefallene raus, neue rein.</summary>
        private void ApplyRoster(Match match, List<int>? selectedPlayerIds)
        {
            var selected = (selectedPlayerIds ?? new List<int>()).Distinct().ToList();
            var current = match.MatchPlayers.Select(mp => mp.UserId).ToList();

            foreach (var gone in match.MatchPlayers.Where(mp => !selected.Contains(mp.UserId)).ToList())
                _context.MatchPlayers.Remove(gone);

            foreach (var added in selected.Where(pid => !current.Contains(pid)))
                _context.MatchPlayers.Add(new MatchPlayer { MatchId = match.Id, UserId = added });
        }

        /// <summary>
        /// Spieler für die Kader-Auswahl: die aktiven und zusätzlich die, die
        /// schon im Kader stehen. Ein inzwischen deaktivierter Spieler soll
        /// nicht verschwinden, nur weil das Formular ihn nicht mehr anbietet.
        /// </summary>
        private async Task<List<SelectListItem>> LoadPlayersAsync(IEnumerable<int>? includeIds)
        {
            var ids = includeIds?.ToList() ?? new List<int>();

            return await _context.Players
                .Where(p => p.IsActive || ids.Contains(p.Id))
                .OrderBy(p => p.Username)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.IsActive ? p.Username : p.Username + " (inaktiv)"
                })
                .ToListAsync();
        }

        /// <summary>Alle Saisons zur Auswahl, aktive zuerst.</summary>
        private async Task<List<SelectListItem>> LoadSeasonsAsync()
        {
            return await _context.Seasons
                .OrderByDescending(s => s.IsActive)
                .ThenBy(s => s.Name)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.IsActive ? s.Name + " (aktiv)" : s.Name
                })
                .ToListAsync();
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
