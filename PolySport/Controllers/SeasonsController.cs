using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolySport.Data;
using PolySport.Models;
using PolySport.Models.ViewModels;

namespace PolySport.Controllers
{
    [Authorize]
    public class SeasonsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SeasonsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Liste aller Saisons
        public async Task<IActionResult> Index()
        {
            return View(await _context.Seasons.ToListAsync());
        }

        // GET: Formular für neue Saison
        [Authorize(Roles = AppRoles.Admin)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Neue Saison speichern
        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Season season)
        {
            if (ModelState.IsValid)
            {
                // Wenn die neue Saison aktiv ist, alle anderen auf inaktiv setzen
                if (season.IsActive)
                {
                    var activeSeasons = _context.Seasons.Where(s => s.IsActive);
                    foreach (var s in activeSeasons) s.IsActive = false;
                }

                _context.Seasons.Add(season);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Saison \"{season.Name}\" wurde angelegt.";
                return RedirectToAction(nameof(Index));
            }
            return View(season);
        }

        // GET: Saison bearbeiten
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var season = await _context.Seasons.FindAsync(id);
            if (season == null) return NotFound();

            return View(season);
        }

        // POST: Änderungen speichern
        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Season season)
        {
            if (id != season.Id) return NotFound();
            if (!ModelState.IsValid) return View(season);

            var existing = await _context.Seasons.FindAsync(id);
            if (existing == null) return NotFound();

            // Nur die bearbeitbaren Felder übernehmen, nicht das ganze Objekt ersetzen –
            // sonst gehen die verknüpften Matches an der Navigation verloren.
            existing.Name = season.Name;

            // Es darf immer nur eine Saison aktiv sein
            if (season.IsActive)
            {
                var others = await _context.Seasons
                    .Where(s => s.IsActive && s.Id != id)
                    .ToListAsync();
                foreach (var other in others) other.IsActive = false;
            }
            existing.IsActive = season.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Saison \"{existing.Name}\" wurde gespeichert.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Seasons/Activate/5 – Saison direkt aus der Liste aktiv setzen
        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var season = await _context.Seasons.FindAsync(id);
            if (season == null) return NotFound();

            if (season.IsActive)
            {
                TempData["Success"] = $"Saison \"{season.Name}\" ist bereits aktiv.";
                return RedirectToAction(nameof(Index));
            }

            // Es darf immer nur eine Saison aktiv sein
            var others = await _context.Seasons
                .Where(s => s.IsActive && s.Id != id)
                .ToListAsync();
            foreach (var other in others) other.IsActive = false;

            season.IsActive = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = others.Any()
                ? $"Saison \"{season.Name}\" ist jetzt aktiv (\"{others[0].Name}\" wurde deaktiviert)."
                : $"Saison \"{season.Name}\" ist jetzt aktiv.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Löschen bestätigen. Zeigt vorher, was alles mitgelöscht wird.
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var viewModel = await BuildDeleteViewModelAsync(id.Value);
            if (viewModel == null) return NotFound();

            return View(viewModel);
        }

        // POST: Saison endgültig löschen
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = AppRoles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var season = await _context.Seasons.FindAsync(id);
            if (season == null) return NotFound();

            // Matches, Tore und Kader hängen per Cascade Delete daran und
            // verschwinden mit. Die Bestätigungsseite weist darauf hin.
            var name = season.Name;
            _context.Seasons.Remove(season);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Saison \"{name}\" wurde gelöscht.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<DeleteSeasonViewModel?> BuildDeleteViewModelAsync(int id)
        {
            var season = await _context.Seasons.FindAsync(id);
            if (season == null) return null;

            var matches = await _context.Matches
                .Where(m => m.SeasonId == id)
                .Select(m => new MatchSummary
                {
                    OpponentName = m.OpponentName,
                    MatchDate = m.MatchDate,
                    GoalCount = m.Goals.Count()
                })
                .OrderByDescending(m => m.MatchDate)
                .ToListAsync();

            return new DeleteSeasonViewModel
            {
                Id = season.Id,
                Name = season.Name,
                IsActive = season.IsActive,
                Matches = matches
            };
        }
    }
}