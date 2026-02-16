using PolySport.Data;
using PolySport.Models;
using PolySport.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace PolySport.Controllers
{
    public class GoalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GoalsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Goals/Create/5 (Die 5 ist die MatchId)
        public async Task<IActionResult> Create(int matchId)
        {
            var match = await _context.Matches.FindAsync(matchId);
            if (match == null) return NotFound();

            // Der magische Teil: Wir laden NUR die Spieler, die über MatchPlayer mit diesem Match verknüpft sind!
            var roster = await _context.MatchPlayers
                .Include(mp => mp.User) // Die User-Daten mitladen
                .Where(mp => mp.MatchId == matchId)
                .Select(mp => new SelectListItem
                {
                    Value = mp.UserId.ToString(),
                    Text = mp.User!.Username
                })
                .ToListAsync();

            var viewModel = new CreateGoalViewModel
            {
                MatchId = match.Id,
                OpponentName = match.OpponentName,
                MatchRoster = roster
            };

            return View(viewModel);
        }

        // POST: Goals/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateGoalViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var goal = new Goal
                {
                    MatchId = viewModel.MatchId,
                    ScorerId = viewModel.ScorerId,
                    AssistId = viewModel.AssistId // Kann auch null sein
                };

                _context.Goals.Add(goal);
                await _context.SaveChangesAsync();

                // Nach dem Speichern leiten wir den Admin zur Match-Detailseite zurück
                return RedirectToAction("Details", "Matches", new { id = viewModel.MatchId });
            }

            // Bei Fehler Dropdown neu laden
            viewModel.MatchRoster = await _context.MatchPlayers
                .Include(mp => mp.User)
                .Where(mp => mp.MatchId == viewModel.MatchId)
                .Select(mp => new SelectListItem { Value = mp.UserId.ToString(), Text = mp.User!.Username })
                .ToListAsync();

            return View(viewModel);
        }
    }
}