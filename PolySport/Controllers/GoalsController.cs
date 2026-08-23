using PolySport.Data;
using PolySport.Models;
using PolySport.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace PolySport.Controllers
{
    // Tore erfassen dürfen Admins und Manager – wer das Spiel leitet.
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public class GoalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GoalsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Goals/Create?matchId=5
        public async Task<IActionResult> Create(int matchId)
        {
            var match = await _context.Matches.FindAsync(matchId);
            if (match == null) return NotFound();

            if (match.IsFinished) return FinishedRedirect(match.Id);

            var viewModel = new CreateGoalViewModel
            {
                MatchId = match.Id,
                OpponentName = match.OpponentName,
                MatchRoster = await LoadRosterAsync(match.Id)
            };

            ApplyClock(viewModel, match);
            return View(viewModel);
        }

        // POST: Goals/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateGoalViewModel viewModel)
        {
            var match = await _context.Matches.FindAsync(viewModel.MatchId);
            if (match == null) return NotFound();

            if (match.IsFinished) return FinishedRedirect(match.Id);

            // Der Kader ist die Wahrheit: das Dropdown zeigt nur ihn, aber ein
            // manipuliertes Formular könnte jede beliebige Id senden.
            var rosterIds = await _context.MatchPlayers
                .Where(mp => mp.MatchId == viewModel.MatchId)
                .Select(mp => mp.UserId)
                .ToListAsync();

            if (viewModel.ScorerId != 0 && !rosterIds.Contains(viewModel.ScorerId))
                ModelState.AddModelError(nameof(viewModel.ScorerId), "Dieser Spieler steht nicht im Kader dieses Matches.");

            if (viewModel.AssistId.HasValue && !rosterIds.Contains(viewModel.AssistId.Value))
                ModelState.AddModelError(nameof(viewModel.AssistId), "Dieser Spieler steht nicht im Kader dieses Matches.");

            if (viewModel.AssistId.HasValue && viewModel.AssistId.Value == viewModel.ScorerId)
                ModelState.AddModelError(nameof(viewModel.AssistId), "Torschütze und Assist können nicht dieselbe Person sein.");

            var time = ResolveTime(viewModel, match);

            if (ModelState.IsValid)
            {
                _context.Goals.Add(new Goal
                {
                    MatchId = viewModel.MatchId,
                    ScorerId = viewModel.ScorerId,
                    AssistId = viewModel.AssistId, // Kann auch null sein
                    Period = time.Period,
                    SecondsInPeriod = time.Seconds,
                    IsOpponentGoal = false
                });

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Tor erfasst ({time.Period}. Drittel, {Goal.FormatTime(time.Seconds)}).";
                return RedirectToAction("Details", "Matches", new { id = viewModel.MatchId });
            }

            // Bei Fehler Dropdown und Uhrzustand neu laden
            viewModel.OpponentName = match.OpponentName;
            viewModel.MatchRoster = await LoadRosterAsync(viewModel.MatchId);
            ApplyClock(viewModel, match);

            return View(viewModel);
        }

        // GET: Goals/CreateOpponent?matchId=5
        public async Task<IActionResult> CreateOpponent(int matchId)
        {
            var match = await _context.Matches.FindAsync(matchId);
            if (match == null) return NotFound();

            if (match.IsFinished) return FinishedRedirect(match.Id);

            var viewModel = new CreateOpponentGoalViewModel
            {
                MatchId = match.Id,
                OpponentName = match.OpponentName
            };

            ApplyClock(viewModel, match);
            return View(viewModel);
        }

        // POST: Goals/CreateOpponent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOpponent(CreateOpponentGoalViewModel viewModel)
        {
            var match = await _context.Matches.FindAsync(viewModel.MatchId);
            if (match == null) return NotFound();

            if (match.IsFinished) return FinishedRedirect(match.Id);

            var time = ResolveTime(viewModel, match);

            if (ModelState.IsValid)
            {
                _context.Goals.Add(new Goal
                {
                    MatchId = viewModel.MatchId,
                    IsOpponentGoal = true,
                    Period = time.Period,
                    SecondsInPeriod = time.Seconds
                    // Torschütze und Assist bleiben leer
                });

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Gegentor erfasst ({time.Period}. Drittel, {Goal.FormatTime(time.Seconds)}).";
                return RedirectToAction("Details", "Matches", new { id = viewModel.MatchId });
            }

            viewModel.OpponentName = match.OpponentName;
            ApplyClock(viewModel, match);

            return View(viewModel);
        }

        // POST: Goals/Delete/5 – Fehleintrag korrigieren
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var goal = await _context.Goals.FindAsync(id);
            if (goal == null) return NotFound();

            var match = await _context.Matches.FindAsync(goal.MatchId);
            if (match == null) return NotFound();

            if (match.IsFinished) return FinishedRedirect(match.Id);

            _context.Goals.Remove(goal);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tor wurde gelöscht.";
            return RedirectToAction("Details", "Matches", new { id = goal.MatchId });
        }

        /// <summary>
        /// Läuft die Uhr, wird der aktuelle Stand festgehalten – und zwar beim
        /// Öffnen des Formulars, damit die Zeit des Tores zählt und nicht die
        /// Zeit, die zum Ausfüllen gebraucht wurde. Steht die Uhr, wird von Hand
        /// nachgetragen.
        /// </summary>
        private static void ApplyClock(GoalTimeInputBase viewModel, Match match)
        {
            if (match.IsPeriodRunning)
            {
                viewModel.LiveClock = true;
                viewModel.Period = match.CurrentPeriod;
                viewModel.SecondsInPeriod = match.ElapsedSecondsInPeriod;
            }
            else
            {
                viewModel.LiveClock = false;
                // Vorschlag: das zuletzt gespielte Drittel
                viewModel.Period ??= match.CurrentPeriod > 0 ? match.CurrentPeriod : null;
            }
        }

        /// <summary>
        /// Ermittelt Drittel und Sekunde des Tores und meldet fehlende Angaben
        /// über den ModelState.
        /// </summary>
        private (int? Period, int? Seconds) ResolveTime(GoalTimeInputBase viewModel, Match match)
        {
            if (viewModel.LiveClock && match.IsPeriodRunning)
            {
                // Festgehaltener Wert, aber nie mehr als seit Drittelbeginn
                // vergangen ist – gegen manipulierte Formulare.
                var elapsed = match.ElapsedSecondsInPeriod;
                var seconds = viewModel.SecondsInPeriod.HasValue
                    ? Math.Clamp(viewModel.SecondsInPeriod.Value, 0, elapsed)
                    : elapsed;

                return (match.CurrentPeriod, seconds);
            }

            // Manuelle Nacherfassung
            if (!viewModel.Period.HasValue)
                ModelState.AddModelError(nameof(viewModel.Period), "Bitte gib das Drittel an.");
            else if (viewModel.Period < 1 || viewModel.Period > 3)
                ModelState.AddModelError(nameof(viewModel.Period), "Drittel muss 1, 2 oder 3 sein.");

            if (!viewModel.MinuteInPeriod.HasValue)
                ModelState.AddModelError(nameof(viewModel.MinuteInPeriod), "Bitte gib die Spielminute an.");
            else if (viewModel.MinuteInPeriod < 0 || viewModel.MinuteInPeriod > 30)
                ModelState.AddModelError(nameof(viewModel.MinuteInPeriod), "Minute muss zwischen 0 und 30 liegen.");

            return (viewModel.Period, viewModel.MinuteInPeriod * 60);
        }

        private async Task<List<SelectListItem>> LoadRosterAsync(int matchId)
        {
            // Der magische Teil: Wir laden NUR die Spieler, die über MatchPlayer mit diesem Match verknüpft sind!
            return await _context.MatchPlayers
                .Include(mp => mp.User)
                .Where(mp => mp.MatchId == matchId)
                .OrderBy(mp => mp.User!.Username)
                .Select(mp => new SelectListItem
                {
                    Value = mp.UserId.ToString(),
                    Text = mp.User!.Username
                })
                .ToListAsync();
        }

        private IActionResult FinishedRedirect(int matchId)
        {
            TempData["Error"] = "Das Match ist beendet. Öffne es wieder, um Tore nachzutragen.";
            return RedirectToAction("Details", "Matches", new { id = matchId });
        }
    }
}
